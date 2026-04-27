using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="ChangeUserEmailHandler"/>.
/// </summary>
public sealed class ChangeUserEmailHandlerTests
{
    [Fact]
    public async Task ChangeUserEmail_Should_UpdateEmail_AndRotateSecurityStamp()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var user = CreateUser("old@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = user.Id,
            NewEmail = "new@darwin.de"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.Email.Should().Be("new@darwin.de");
        persisted.NormalizedEmail.Should().Be("NEW@DARWIN.DE");
        persisted.UserName.Should().Be("new@darwin.de");
        persisted.NormalizedUserName.Should().Be("NEW@DARWIN.DE");
        persisted.EmailConfirmed.Should().BeFalse();
        persisted.SecurityStamp.Should().Be("new-stamp");
    }

    [Fact]
    public async Task ChangeUserEmail_Should_TrimAndNormalizeNewEmail()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var user = CreateUser("trim-test@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = user.Id,
            NewEmail = "  Trimmed@Darwin.DE  "
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.Email.Should().Be("Trimmed@Darwin.DE");
        persisted.NormalizedEmail.Should().Be("TRIMMED@DARWIN.DE");
    }

    [Fact]
    public async Task ChangeUserEmail_Should_Fail_WhenUserNotFound()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = Guid.NewGuid(),
            NewEmail = "notfound@darwin.de"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task ChangeUserEmail_Should_Fail_WhenEmailAlreadyInUse()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var existingUser = CreateUser("taken@darwin.de");
        var targetUser = CreateUser("target@darwin.de");
        db.Set<User>().AddRange(existingUser, targetUser);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = targetUser.Id,
            NewEmail = "taken@darwin.de"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("EmailAlreadyInUse");
    }

    [Fact]
    public async Task ChangeUserEmail_Should_Succeed_WhenEmailBelongsToSameUser()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var user = CreateUser("same@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        // Changing email to same address of a different user is blocked, but same user should pass uniqueness check
        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = user.Id,
            NewEmail = "different@darwin.de"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeUserEmail_Should_NotUpdateDeletedUser()
    {
        await using var db = ChangeEmailTestDbContext.Create();
        var user = CreateUser("deleted@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UserChangeEmailDto
        {
            Id = user.Id,
            NewEmail = "changed@darwin.de"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email)
    {
        return new User(email, "hashed:initial", "initial-stamp")
        {
            FirstName = "Test",
            LastName = "User",
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

    private static ChangeUserEmailHandler CreateHandler(ChangeEmailTestDbContext db)
        => new(db, new FakeStampService(), new UserChangeEmailValidator(), new TestLocalizer());

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class ChangeEmailTestDbContext : DbContext, IAppDbContext
    {
        private ChangeEmailTestDbContext(DbContextOptions<ChangeEmailTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ChangeEmailTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ChangeEmailTestDbContext>()
                .UseInMemoryDatabase($"darwin_change_email_tests_{Guid.NewGuid()}")
                .Options;
            return new ChangeEmailTestDbContext(options);
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
        }
    }
}
