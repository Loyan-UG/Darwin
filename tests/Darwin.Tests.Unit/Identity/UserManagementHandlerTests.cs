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
using Darwin.Domain.Entities.Orders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for user management command handlers:
/// <see cref="CreateUserHandler"/>, <see cref="SoftDeleteUserHandler"/>,
/// <see cref="ChangePasswordHandler"/>, and <see cref="UpdateCurrentUserHandler"/>.
/// </summary>
public sealed class UserManagementHandlerTests
{
    // ─── CreateUserHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_Should_PersistUser_WithHashedPassword()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var hasher = new FakeHasher();
        var handler = new CreateUserHandler(db, hasher, new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "newuser@darwin.de",
            Password = "PlainPass123",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            FirstName = "Max",
            LastName = "Mustermann",
            IsActive = true
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        persisted.Email.Should().Be("newuser@darwin.de");
        persisted.PasswordHash.Should().Be("hashed:PlainPass123");
        persisted.FirstName.Should().Be("Max");
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_Should_Fail_WhenEmailAlreadyExists()
    {
        await using var db = UserMgmtTestDbContext.Create();
        db.Set<User>().Add(CreateUser("duplicate@darwin.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var hasher = new FakeHasher();
        var handler = new CreateUserHandler(db, hasher, new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "duplicate@darwin.de",
            Password = "AnotherPass1A",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("EmailAlreadyInUse");
    }

    // ─── SoftDeleteUserHandler ────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteUser_Should_SoftDeleteUser_WhenNoOrderHistory()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("delete-me@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService();
        var handler = new SoftDeleteUserHandler(db, jwt, new FakeStampService(), new UserDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserDeleteDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.WasSoftDeleted.Should().BeTrue();
        result.Value.WasDeactivatedDueToReferences.Should().BeFalse();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUser_Should_OnlyDeactivate_WhenUserHasOrderHistory()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("order-history@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<Order>().Add(new Order
        {
            UserId = user.Id,
            OrderNumber = "ORD-0001",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService();
        var handler = new SoftDeleteUserHandler(db, jwt, new FakeStampService(), new UserDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserDeleteDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.WasDeactivatedDueToReferences.Should().BeTrue();
        result.Value.WasSoftDeleted.Should().BeFalse();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeFalse();
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUser_Should_Fail_WhenUserIsSystem()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("system-user@darwin.de");
        user.IsSystem = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteUserHandler(db, new FakeJwtService(), new FakeStampService(), new UserDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserDeleteDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("SystemUsersCannotBeDeleted");
    }

    [Fact]
    public async Task SoftDeleteUser_Should_Fail_WhenUserNotFound()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var handler = new SoftDeleteUserHandler(db, new FakeJwtService(), new FakeStampService(), new UserDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── ChangePasswordHandler ────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Should_UpdatePasswordHash_WhenCurrentPasswordIsCorrect()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("change-pass@darwin.de");
        user.PasswordHash = "hashed:OldPass1A";
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var hasher = new FakeHasher();
        var handler = new ChangePasswordHandler(db, hasher, new FakeStampService(), new UserChangePasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserChangePasswordDto
        {
            Id = user.Id,
            CurrentPassword = "OldPass1A",
            NewPassword = "NewSecure1B"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.PasswordHash.Should().Be("hashed:NewSecure1B");
        persisted.SecurityStamp.Should().Be("new-stamp");
    }

    [Fact]
    public async Task ChangePassword_Should_Fail_WhenCurrentPasswordIsIncorrect()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("wrong-pass@darwin.de");
        user.PasswordHash = "hashed:RealPass1A";
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ChangePasswordHandler(db, new FakeHasher(), new FakeStampService(), new UserChangePasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserChangePasswordDto
        {
            Id = user.Id,
            CurrentPassword = "WrongPass1A",
            NewPassword = "NewPass1B"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("CurrentPasswordIncorrect");
    }

    [Fact]
    public async Task ChangePassword_Should_Fail_WhenUserNotFound()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var handler = new ChangePasswordHandler(db, new FakeHasher(), new FakeStampService(), new UserChangePasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserChangePasswordDto
        {
            Id = Guid.NewGuid(),
            CurrentPassword = "AnyPass1A",
            NewPassword = "NewPass1B"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── UpdateCurrentUserHandler ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateCurrentUser_Should_UpdateProfileFields()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("profile-update@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(user.Id);
        var handler = new UpdateCurrentUserHandler(db, currentUser, new UserProfileEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserProfileEditDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion,
            FirstName = "Updated",
            LastName = "Name",
            Locale = "en-US",
            Timezone = "America/New_York",
            Currency = "USD"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.FirstName.Should().Be("Updated");
        persisted.LastName.Should().Be("Name");
        persisted.Locale.Should().Be("en-US");
        persisted.Timezone.Should().Be("America/New_York");
        persisted.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task UpdateCurrentUser_Should_ClearPhoneConfirmation_WhenPhoneChanges()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("phone-change@darwin.de");
        user.PhoneE164 = "+4915123456789";
        user.PhoneNumberConfirmed = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(user.Id);
        var handler = new UpdateCurrentUserHandler(db, currentUser, new UserProfileEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserProfileEditDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion,
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            PhoneE164 = "+4916198765432"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.PhoneE164.Should().Be("+4916198765432");
        persisted.PhoneNumberConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCurrentUser_Should_Fail_WhenUserIdMismatch()
    {
        await using var db = UserMgmtTestDbContext.Create();
        var user = CreateUser("mismatch@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(Guid.NewGuid()); // different user
        var handler = new UpdateCurrentUserHandler(db, currentUser, new UserProfileEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserProfileEditDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion,
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Unauthorized");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email)
    {
        return new User(email, "hashed:initial", "initial-stamp")
        {
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
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

    private sealed class FakeHasher : IUserPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string hashedPassword, string password) =>
            hashedPassword == $"hashed:{password}";
    }

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";

        public bool AreEqual(string? a, string? b) =>
            string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class FakeJwtService : IJwtTokenService
    {
        public Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)> IssueTokensAsync(
            Guid userId, string email, string? deviceId,
            System.Collections.Generic.IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null, CancellationToken ct = default)
            => Task.FromResult(("at", DateTime.UtcNow.AddMinutes(30), "rt", DateTime.UtcNow.AddDays(7)));

        public Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;

        public FakeCurrentUser(Guid userId) => _userId = userId;

        public Guid GetCurrentUserId() => _userId;
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

    private sealed class UserMgmtTestDbContext : DbContext, IAppDbContext
    {
        private UserMgmtTestDbContext(DbContextOptions<UserMgmtTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static UserMgmtTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<UserMgmtTestDbContext>()
                .UseInMemoryDatabase($"darwin_user_mgmt_tests_{Guid.NewGuid()}")
                .Options;
            return new UserMgmtTestDbContext(options);
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

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
