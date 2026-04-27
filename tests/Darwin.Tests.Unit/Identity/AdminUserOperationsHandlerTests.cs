using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
/// Unit tests for admin-facing user operation handlers:
/// <see cref="UpdateUserHandler"/>, <see cref="SetUserPasswordByAdminHandler"/>,
/// <see cref="AssignRoleToUserHandler"/>, <see cref="UpdateUserRolesHandler"/>,
/// and <see cref="GetCurrentUserProfileHandler"/>.
/// </summary>
public sealed class AdminUserOperationsHandlerTests
{
    // ─── UpdateUserHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_Should_UpdateEditableFields()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("admin-update@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserHandler(db, new UserEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserEditDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion,
            FirstName = "Updated",
            LastName = "Name",
            Locale = "en-US",
            Timezone = "America/New_York",
            Currency = "USD",
            PhoneE164 = "+1234567890",
            IsActive = false
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.FirstName.Should().Be("Updated");
        persisted.LastName.Should().Be("Name");
        persisted.Locale.Should().Be("en-US");
        persisted.Timezone.Should().Be("America/New_York");
        persisted.Currency.Should().Be("USD");
        persisted.PhoneE164.Should().Be("+1234567890");
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUser_Should_Fail_WhenUserNotFound()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var handler = new UpdateUserHandler(db, new UserEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task UpdateUser_Should_Fail_WhenSoftDeleted()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("deleted-update@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserHandler(db, new UserEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserEditDto
        {
            Id = user.Id,
            RowVersion = user.RowVersion,
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task UpdateUser_Should_Fail_OnConcurrencyConflict()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("concurrency-update@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserHandler(db, new UserEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UserEditDto
        {
            Id = user.Id,
            RowVersion = [99, 88, 77], // stale
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflict");
    }

    // ─── SetUserPasswordByAdminHandler ────────────────────────────────────────

    [Fact]
    public async Task SetUserPasswordByAdmin_Should_HashNewPassword_AndRotateSecurityStamp()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("admin-pass@darwin.de");
        user.PasswordHash = "hashed:OldPassword1";
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var hasher = new FakeHasher();
        var stamps = new FakeStampService();
        var handler = new SetUserPasswordByAdminHandler(db, hasher, stamps, new UserAdminSetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserAdminSetPasswordDto
        {
            Id = user.Id,
            NewPassword = "NewAdmin1Pass"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.PasswordHash.Should().Be("hashed:NewAdmin1Pass");
        persisted.SecurityStamp.Should().Be("new-stamp");
    }

    [Fact]
    public async Task SetUserPasswordByAdmin_Should_Fail_WhenUserNotFound()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var handler = new SetUserPasswordByAdminHandler(db, new FakeHasher(), new FakeStampService(), new UserAdminSetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserAdminSetPasswordDto
        {
            Id = Guid.NewGuid(),
            NewPassword = "NewAdmin1Pass"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task SetUserPasswordByAdmin_Should_Fail_WhenUserIsSoftDeleted()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("deleted-pass@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetUserPasswordByAdminHandler(db, new FakeHasher(), new FakeStampService(), new UserAdminSetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserAdminSetPasswordDto
        {
            Id = user.Id,
            NewPassword = "NewAdmin1Pass"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── AssignRoleToUserHandler ──────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleToUser_Should_CreateUserRoleLink()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("assign-role@darwin.de");
        var role = new Role("editor", "Editor", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(user.Id, role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var link = await db.Set<UserRole>().SingleAsync(TestContext.Current.CancellationToken);
        link.UserId.Should().Be(user.Id);
        link.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task AssignRoleToUser_Should_BeIdempotent_WhenLinkAlreadyExists()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("idempotent-role@darwin.de");
        var role = new Role("viewer", "Viewer", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserRole>().Add(new UserRole(user.Id, role.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(user.Id, role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var count = await db.Set<UserRole>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1, "a second assignment should not create a duplicate link");
    }

    [Fact]
    public async Task AssignRoleToUser_Should_Fail_WhenUserNotFound()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var role = new Role("admin", "Admin", false, null);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task AssignRoleToUser_Should_Fail_WhenRoleNotFound()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("role-not-found@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(user.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    // ─── UpdateUserRolesHandler ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserRoles_Should_ReplaceRoleSet()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("update-roles@darwin.de");
        var roleA = new Role("role-a", "Role A", false, null);
        var roleB = new Role("role-b", "Role B", false, null);
        var roleC = new Role("role-c", "Role C", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().AddRange(roleA, roleB, roleC);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Start with roles A and B assigned
        db.Set<UserRole>().AddRange(new UserRole(user.Id, roleA.Id), new UserRole(user.Id, roleB.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserRolesHandler(db, new TestLocalizer());

        // Replace with B and C
        var result = await handler.HandleAsync(new UserRolesUpdateDto
        {
            UserId = user.Id,
            RowVersion = user.RowVersion,
            RoleIds = [roleB.Id, roleC.Id]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var activeRoles = await db.Set<UserRole>()
            .Where(ur => ur.UserId == user.Id && !ur.IsDeleted)
            .Select(ur => ur.RoleId)
            .ToListAsync(TestContext.Current.CancellationToken);

        activeRoles.Should().BeEquivalentTo(new[] { roleB.Id, roleC.Id });
    }

    [Fact]
    public async Task UpdateUserRoles_Should_Fail_WhenUserNotFoundOrInactive()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var handler = new UpdateUserRolesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UserRolesUpdateDto
        {
            UserId = Guid.NewGuid(),
            RowVersion = [1],
            RoleIds = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task UpdateUserRoles_Should_Fail_OnConcurrencyConflict()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("roles-concurrency@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserRolesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UserRolesUpdateDto
        {
            UserId = user.Id,
            RowVersion = [99, 88, 77], // stale
            RoleIds = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictReloadAndRetry");
    }

    [Fact]
    public async Task UpdateUserRoles_Should_Fail_WhenSomeRoleIdsAreInvalid()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("invalid-roles@darwin.de");
        var role = new Role("valid-role", "Valid", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserRolesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UserRolesUpdateDto
        {
            UserId = user.Id,
            RowVersion = user.RowVersion,
            RoleIds = [role.Id, Guid.NewGuid()] // one valid, one invalid
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidRolesSelection");
    }

    [Fact]
    public async Task UpdateUserRoles_Should_ClearAllRoles_WhenEmptyListProvided()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("clear-roles@darwin.de");
        var role = new Role("to-remove", "To Remove", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserRole>().Add(new UserRole(user.Id, role.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserRolesHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UserRolesUpdateDto
        {
            UserId = user.Id,
            RowVersion = user.RowVersion,
            RoleIds = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var activeRoles = await db.Set<UserRole>()
            .Where(ur => ur.UserId == user.Id && !ur.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);
        activeRoles.Should().BeEmpty();
    }

    // ─── GetCurrentUserProfileHandler ─────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserProfile_Should_ReturnProfile_WhenUserExists()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("profile-query@darwin.de");
        user.FirstName = "Jane";
        user.LastName = "Doe";
        user.PhoneE164 = "+4915100000001";
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(user.Id);
        var handler = new GetCurrentUserProfileHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("profile-query@darwin.de");
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Doe");
        result.Value.PhoneE164.Should().Be("+4915100000001");
        result.Value.Locale.Should().Be("de-DE");
        result.Value.Timezone.Should().Be("Europe/Berlin");
        result.Value.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetCurrentUserProfile_Should_Fail_WhenUserNotAuthenticated()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var currentUser = new FakeCurrentUser(Guid.Empty); // unauthenticated
        var handler = new GetCurrentUserProfileHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    [Fact]
    public async Task GetCurrentUserProfile_Should_Fail_WhenUserNotFound()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var currentUser = new FakeCurrentUser(Guid.NewGuid()); // unknown user
        var handler = new GetCurrentUserProfileHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task GetCurrentUserProfile_Should_Fail_WhenUserIsInactive()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("inactive-profile@darwin.de");
        user.IsActive = false;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(user.Id);
        var handler = new GetCurrentUserProfileHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task GetCurrentUserProfile_Should_ReturnNonNullRowVersion()
    {
        await using var db = AdminUserOpsTestDbContext.Create();
        var user = CreateUser("rowversion@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(user.Id);
        var handler = new GetCurrentUserProfileHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.RowVersion.Should().NotBeNull();
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
        public bool Verify(string hashedPassword, string password) => hashedPassword == $"hashed:{password}";
    }

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
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
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class AdminUserOpsTestDbContext : DbContext, IAppDbContext
    {
        private AdminUserOpsTestDbContext(DbContextOptions<AdminUserOpsTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AdminUserOpsTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AdminUserOpsTestDbContext>()
                .UseInMemoryDatabase($"darwin_admin_user_ops_tests_{Guid.NewGuid()}")
                .Options;
            return new AdminUserOpsTestDbContext(options);
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

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.NormalizedName).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.RolePermissions);
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RoleId).IsRequired();
                builder.Ignore(x => x.User);
                builder.Ignore(x => x.Role);
            });
        }
    }
}
