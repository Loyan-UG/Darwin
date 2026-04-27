using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Commands;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="RequestCurrentUserAccountDeletionHandler"/>.
/// </summary>
public sealed class RequestCurrentUserAccountDeletionHandlerTests
{
    private static readonly DateTime FixedNow = new(2030, 5, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_Should_Fail_WhenConfirmationIsFalse()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var userId = Guid.NewGuid();
        var handler = CreateHandler(db, userId);

        var result = await handler.HandleAsync(false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ExplicitDeletionConfirmationRequired");
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenCurrentUserIdIsEmpty()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var handler = CreateHandler(db, Guid.Empty);

        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenUserNotFound()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ActiveUserAccountNotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenUserIsInactive()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "inactive@darwin.de");
        user.IsActive = false;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, userId);

        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ActiveUserAccountNotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenUserIsDeleted()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "deleted@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, userId);

        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ActiveUserAccountNotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenUserIsSystem()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "system@darwin.de");
        user.IsSystem = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, userId);

        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("SystemUsersCannotRequestAccountDeletion");
    }

    [Fact]
    public async Task Handle_Should_AnonymizeUser_WhenActiveRegularUser()
    {
        await using var db = AccountDeletionTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "active@darwin.de");
        user.FirstName = "Anna";
        user.LastName = "Hoffman";
        user.Company = "Acme GmbH";
        user.MarketingConsent = true;
        db.Set<User>().Add(user);

        db.Set<UserDevice>().Add(new UserDevice
        {
            UserId = userId,
            DeviceId = "device-001",
            PushToken = "push-abc",
            NotificationsEnabled = true,
            IsActive = true
        });

        db.Set<Address>().Add(new Address
        {
            UserId = userId,
            FullName = "Anna Hoffman",
            Street1 = "Hauptstrasse 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        });

        db.Set<UserToken>().Add(new UserToken(userId, "EmailConfirm", "some-value", FixedNow.AddHours(1)));
        db.Set<UserLogin>().Add(new UserLogin(userId, "Google", "google-key-123", "Anna Hoffman"));
        db.Set<UserTwoFactorSecret>().Add(new UserTwoFactorSecret(userId, "SECRETBASE32", "active@darwin.de"));
        db.Set<UserWebAuthnCredential>().Add(new UserWebAuthnCredential { UserId = userId });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, userId);
        var result = await handler.HandleAsync(true, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking()
            .SingleAsync(x => x.Id == userId, TestContext.Current.CancellationToken);
        persisted.IsActive.Should().BeFalse();
        persisted.Email.Should().StartWith("deleted-user-");
        persisted.FirstName.Should().Be("Deleted");
        persisted.LastName.Should().Be("User");
        persisted.Company.Should().BeNull();
        persisted.MarketingConsent.Should().BeFalse();
        persisted.ChannelsOptInJson.Should().Be("{}");
        persisted.TwoFactorEnabled.Should().BeFalse();
        persisted.EmailConfirmed.Should().BeFalse();

        var device = await db.Set<UserDevice>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        device.IsActive.Should().BeFalse();
        device.NotificationsEnabled.Should().BeFalse();
        device.PushToken.Should().BeNull();

        var address = await db.Set<Address>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        address.FullName.Should().Be("Deleted User");
        address.Street1.Should().Be("Deleted");
        address.City.Should().Be("Deleted");

        var token = await db.Set<UserToken>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        token.UsedAtUtc.Should().NotBeNull();

        var login = await db.Set<UserLogin>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        login.IsDeleted.Should().BeTrue();

        var totp = await db.Set<UserTwoFactorSecret>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        totp.IsDeleted.Should().BeTrue();

        var webAuthn = await db.Set<UserWebAuthnCredential>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        webAuthn.IsDeleted.Should().BeTrue();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RequestCurrentUserAccountDeletionHandler CreateHandler(IAppDbContext db, Guid userId)
    {
        return new RequestCurrentUserAccountDeletionHandler(
            db,
            new FakeCurrentUser(userId),
            new FakeStampService(),
            new FakeClock(FixedNow),
            new TestLocalizer());
    }

    private static User CreateUser(Guid id, string email) =>
        new(email, "hashed:pass", "stamp")
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            IsDeleted = false,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        public FakeCurrentUser(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class AccountDeletionTestDbContext : DbContext, IAppDbContext
    {
        private AccountDeletionTestDbContext(DbContextOptions<AccountDeletionTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AccountDeletionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AccountDeletionTestDbContext>()
                .UseInMemoryDatabase($"darwin_account_deletion_tests_{Guid.NewGuid()}")
                .Options;
            return new AccountDeletionTestDbContext(options);
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

            modelBuilder.Entity<UserToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.Purpose).IsRequired();
                builder.Property(x => x.Value).IsRequired();
            });

            modelBuilder.Entity<PasswordResetToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.Token).IsRequired();
            });

            modelBuilder.Entity<UserDevice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.DeviceId).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<Address>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FullName).IsRequired();
                builder.Property(x => x.Street1).IsRequired();
                builder.Property(x => x.PostalCode).IsRequired();
                builder.Property(x => x.City).IsRequired();
                builder.Property(x => x.CountryCode).IsRequired();
            });

            modelBuilder.Entity<UserLogin>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.ProviderKey).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<UserTwoFactorSecret>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.SecretBase32).IsRequired();
                builder.Property(x => x.Label).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<UserWebAuthnCredential>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.CredentialId).IsRequired();
                builder.Property(x => x.PublicKey).IsRequired();
            });
        }
    }
}
