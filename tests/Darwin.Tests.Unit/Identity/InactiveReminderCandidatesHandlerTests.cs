using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="GetInactiveReminderCandidatesHandler"/>.
/// Verifies validation, candidate selection, cooldown suppression, and device filtering.
/// </summary>
public sealed class InactiveReminderCandidatesHandlerTests
{
    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ReturnFail_WhenRequestIsNull()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RequestPayloadRequired");
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ReturnFail_WhenThresholdDaysIsZero()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 0, CooldownHours = 72 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InactiveThresholdDaysGreaterThanZero");
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ReturnFail_WhenCooldownHoursIsNegative()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 14, CooldownHours = -1 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("CooldownHoursMustNotBeNegative");
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ReturnEmpty_WhenNoSnapshots()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 14, CooldownHours = 72 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ReturnCandidates_WithActivePushDevice()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "inactive@test.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-20),
            SnapshotJson = "{}",
            CalculatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        });
        db.Set<UserDevice>().Add(new UserDevice
        {
            UserId = userId,
            DeviceId = "device-abc",
            PushToken = "push-token-xyz",
            NotificationsEnabled = true,
            IsActive = true,
            Platform = MobilePlatform.Android,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 14, CooldownHours = 72 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().UserId.Should().Be(userId);
        result.Value.Single().PushToken.Should().Be("push-token-xyz");
        result.Value.Single().IsSuppressed.Should().BeFalse();
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ExcludeUsers_WithoutActivePushDevice()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "nopush@test.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-20),
            SnapshotJson = "{}",
            CalculatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        });
        // No device registered
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 14, CooldownHours = 72 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_SuppressByCooldown_WhenRecentReminderSent()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "cooldown@test.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var lastReminderSentAt = DateTime.UtcNow.AddHours(-12); // within 72-hour cooldown
        var snapshotJson = $"{{\"LastInactiveReminderSentAtUtc\":\"{lastReminderSentAt:O}\"}}";

        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-20),
            SnapshotJson = snapshotJson,
            CalculatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        });
        db.Set<UserDevice>().Add(new UserDevice
        {
            UserId = userId,
            DeviceId = "device-cooldown",
            PushToken = "push-token-cooldown",
            NotificationsEnabled = true,
            IsActive = true,
            Platform = MobilePlatform.iOS,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto
            {
                InactiveThresholdDays = 14,
                CooldownHours = 72,
                IncludeSuppressedByCooldown = false
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty("cooldown suppression should exclude this candidate");
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_IncludeSuppressed_WhenFlagIsSet()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "include-suppressed@test.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var lastReminderSentAt = DateTime.UtcNow.AddHours(-12);
        var snapshotJson = $"{{\"LastInactiveReminderSentAtUtc\":\"{lastReminderSentAt:O}\"}}";

        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-20),
            SnapshotJson = snapshotJson,
            CalculatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        });
        db.Set<UserDevice>().Add(new UserDevice
        {
            UserId = userId,
            DeviceId = "device-incl",
            PushToken = "push-token-incl",
            NotificationsEnabled = true,
            IsActive = true,
            Platform = MobilePlatform.Android,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto
            {
                InactiveThresholdDays = 14,
                CooldownHours = 72,
                IncludeSuppressedByCooldown = true
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().IsSuppressed.Should().BeTrue();
        result.Value.Single().SuppressionCode.Should().Be("CooldownActive");
    }

    [Fact]
    public async Task GetInactiveReminderCandidates_Should_ExcludeRecentlyActiveUsers()
    {
        await using var db = InactiveReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "active@test.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-2), // active 2 days ago – below 14-day threshold
            SnapshotJson = "{}",
            CalculatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        });
        db.Set<UserDevice>().Add(new UserDevice
        {
            UserId = userId,
            DeviceId = "device-active",
            PushToken = "push-token-active",
            NotificationsEnabled = true,
            IsActive = true,
            Platform = MobilePlatform.Android,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInactiveReminderCandidatesHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(
            new GetInactiveReminderCandidatesDto { InactiveThresholdDays = 14, CooldownHours = 72 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty("user is still recently active");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id, string email)
    {
        return new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            IsActive = true,
            EmailConfirmed = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class InactiveReminderTestDbContext : DbContext, IAppDbContext
    {
        private InactiveReminderTestDbContext(DbContextOptions<InactiveReminderTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InactiveReminderTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InactiveReminderTestDbContext>()
                .UseInMemoryDatabase($"darwin_inactive_reminder_tests_{Guid.NewGuid()}")
                .Options;
            return new InactiveReminderTestDbContext(options);
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
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.Logins);
                builder.Ignore(x => x.Tokens);
                builder.Ignore(x => x.TwoFactorSecrets);
                builder.Ignore(x => x.Devices);
                builder.Ignore(x => x.BusinessFavorites);
                builder.Ignore(x => x.BusinessLikes);
                builder.Ignore(x => x.BusinessReviews);
                builder.Ignore(x => x.EngagementSnapshot);
            });

            modelBuilder.Entity<UserEngagementSnapshot>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.SnapshotJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<UserDevice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.DeviceId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
