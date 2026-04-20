using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers current-user preference query and command handlers.
/// </summary>
public sealed class CurrentUserPreferencesHandlersTests
{
    [Fact]
    public async Task GetCurrentUserPreferences_Should_MapStoredChannelFlags()
    {
        await using var db = PreferenceTestDbContext.Create();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(
            userId,
            channelsOptInJson: "{\"Email\":true,\"SMS\":false,\"WhatsApp\":true,\"Push\":true,\"Analytics\":false}",
            marketingConsent: true));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentUserPreferencesHandler(
            db,
            new StubCurrentUserService(userId),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MarketingConsent.Should().BeTrue();
        result.Value.AllowEmailMarketing.Should().BeTrue();
        result.Value.AllowSmsMarketing.Should().BeFalse();
        result.Value.AllowWhatsAppMarketing.Should().BeTrue();
        result.Value.AllowPromotionalPushNotifications.Should().BeTrue();
        result.Value.AllowOptionalAnalyticsTracking.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCurrentUserPreferences_Should_ClearMarketingChannels_WhenConsentIsRevoked()
    {
        await using var db = PreferenceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(
            userId,
            channelsOptInJson: "{\"Email\":true,\"SMS\":true,\"WhatsApp\":true,\"Push\":true,\"Analytics\":true,\"Legacy\":true}",
            marketingConsent: true);

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCurrentUserPreferencesHandler(
            db,
            new StubCurrentUserService(userId),
            new UpdateMemberPreferencesValidator(),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new UpdateMemberPreferencesDto
        {
            RowVersion = user.RowVersion,
            MarketingConsent = false,
            AllowEmailMarketing = true,
            AllowSmsMarketing = true,
            AllowWhatsAppMarketing = true,
            AllowPromotionalPushNotifications = true,
            AllowOptionalAnalyticsTracking = true
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == userId, TestContext.Current.CancellationToken);

        persisted.MarketingConsent.Should().BeFalse();

        var channels = JsonSerializer.Deserialize<Dictionary<string, bool>>(persisted.ChannelsOptInJson);
        channels.Should().NotBeNull();
        channels!["Email"].Should().BeFalse();
        channels["SMS"].Should().BeFalse();
        channels["WhatsApp"].Should().BeFalse();
        channels["Push"].Should().BeFalse();
        channels["Analytics"].Should().BeTrue();
        channels["Legacy"].Should().BeTrue();
    }

    private static User CreateUser(Guid userId, string channelsOptInJson, bool marketingConsent)
    {
        var user = new User("member@example.de", "hashed-password", "security-stamp")
        {
            Id = userId,
            FirstName = "Max",
            LastName = "Mustermann",
            ChannelsOptInJson = channelsOptInJson,
            MarketingConsent = marketingConsent,
            AcceptsTermsAtUtc = new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            RowVersion = [1, 2, 3, 4]
        };

        return user;
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId)
        {
            _userId = userId;
        }

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class PreferenceTestDbContext : DbContext, IAppDbContext
    {
        private PreferenceTestDbContext(DbContextOptions<PreferenceTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PreferenceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PreferenceTestDbContext>()
                .UseInMemoryDatabase($"darwin_preference_tests_{Guid.NewGuid()}")
                .Options;

            return new PreferenceTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
