using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers miscellaneous Identity query handlers that were not yet unit-tested:
/// <see cref="ListExternalLoginsHandler"/>,
/// <see cref="UserHasPermissionHandler"/>,
/// <see cref="GetCurrentUserAddressesHandler"/>.
/// </summary>
public sealed class MiscIdentityQueryHandlersTests
{
    // ─── ListExternalLoginsHandler ────────────────────────────────────────────

    [Fact]
    public async Task ListExternalLogins_Should_ReturnLogins_ForUser()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserLogin>().AddRange(
            new UserLogin(userId, "Google", "google-key-1", "alice@gmail.com") { RowVersion = [1] },
            new UserLogin(userId, "Microsoft", "ms-key-1", "alice@outlook.com") { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListExternalLoginsHandler(db);
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result.Select(x => x.Provider).Should().BeEquivalentTo(["Google", "Microsoft"]);
        result.Select(x => x.DisplayName).Should().BeEquivalentTo(["alice@gmail.com", "alice@outlook.com"]);
    }

    [Fact]
    public async Task ListExternalLogins_Should_ReturnEmpty_WhenUserHasNoLogins()
    {
        await using var db = MiscIdentityTestDbContext.Create();

        var handler = new ListExternalLoginsHandler(db);
        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListExternalLogins_Should_ExcludeDeletedLogins()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserLogin>().AddRange(
            new UserLogin(userId, "Google", "google-active", "active@gmail.com") { RowVersion = [1] },
            new UserLogin(userId, "Apple", "apple-deleted", null) { IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListExternalLoginsHandler(db);
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0].Provider.Should().Be("Google");
    }

    [Fact]
    public async Task ListExternalLogins_Should_NotReturnLogins_ForOtherUsers()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Set<UserLogin>().Add(
            new UserLogin(otherUserId, "Google", "google-other", null) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListExternalLoginsHandler(db);
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListExternalLogins_Should_ReturnOrderedByProvider()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserLogin>().AddRange(
            new UserLogin(userId, "Microsoft", "ms-key", null) { RowVersion = [1] },
            new UserLogin(userId, "Apple", "apple-key", null) { RowVersion = [1] },
            new UserLogin(userId, "Google", "google-key", null) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListExternalLoginsHandler(db);
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Select(x => x.Provider).Should().BeInAscendingOrder();
    }

    // ─── UserHasPermissionHandler ─────────────────────────────────────────────

    [Fact]
    public async Task UserHasPermission_Should_ReturnTrue_WhenUserHasPermissionViaRole()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Set<Permission>().Add(new Permission("catalog.read", "Catalog Read", false, null)
        {
            Id = permissionId,
            RowVersion = [1]
        });
        db.Set<Role>().Add(new Role("admin", "Admin", false, null)
        {
            Id = roleId,
            RowVersion = [1]
        });
        db.Set<UserRole>().Add(new UserRole(userId, roleId) { RowVersion = [1] });
        db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UserHasPermissionHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, "catalog.read", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFalse_WhenUserHasNoMatchingPermission()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Set<Permission>().Add(new Permission("catalog.read", "Catalog Read", false, null)
        {
            Id = permissionId,
            RowVersion = [1]
        });
        db.Set<Role>().Add(new Role("admin", "Admin", false, null)
        {
            Id = roleId,
            RowVersion = [1]
        });
        db.Set<UserRole>().Add(new UserRole(userId, roleId) { RowVersion = [1] });
        db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UserHasPermissionHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, "orders.manage", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFalse_WhenUserHasNoRoles()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var handler = new UserHasPermissionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), "catalog.read", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFail_WhenPermissionKeyIsEmpty()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var handler = new UserHasPermissionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), "   ", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionKeyRequired");
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFalse_WhenUserRoleIsDeleted()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Set<Permission>().Add(new Permission("catalog.read", "Catalog Read", false, null)
        {
            Id = permissionId,
            RowVersion = [1]
        });
        db.Set<Role>().Add(new Role("admin", "Admin", false, null)
        {
            Id = roleId,
            RowVersion = [1]
        });
        // UserRole is soft-deleted
        db.Set<UserRole>().Add(new UserRole(userId, roleId) { IsDeleted = true, RowVersion = [1] });
        db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UserHasPermissionHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, "catalog.read", TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFalse_WhenRolePermissionIsDeleted()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Set<Permission>().Add(new Permission("catalog.read", "Catalog Read", false, null)
        {
            Id = permissionId,
            RowVersion = [1]
        });
        db.Set<Role>().Add(new Role("admin", "Admin", false, null)
        {
            Id = roleId,
            RowVersion = [1]
        });
        db.Set<UserRole>().Add(new UserRole(userId, roleId) { RowVersion = [1] });
        // RolePermission is soft-deleted
        db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId) { IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UserHasPermissionHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, "catalog.read", TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasPermission_Should_ReturnFalse_WhenPermissionIsDeleted()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Permission is soft-deleted
        db.Set<Permission>().Add(new Permission("catalog.read", "Catalog Read", false, null)
        {
            Id = permissionId,
            IsDeleted = true,
            RowVersion = [1]
        });
        db.Set<Role>().Add(new Role("admin", "Admin", false, null)
        {
            Id = roleId,
            RowVersion = [1]
        });
        db.Set<UserRole>().Add(new UserRole(userId, roleId) { RowVersion = [1] });
        db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UserHasPermissionHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, "catalog.read", TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    // ─── GetCurrentUserAddressesHandler ──────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAddresses_Should_ReturnAddresses_ForCurrentUser()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "alice@test.de"));
        db.Set<Address>().AddRange(
            new Address
            {
                UserId = userId,
                FullName = "Alice Smith",
                Street1 = "Hauptstraße 1",
                PostalCode = "12345",
                City = "Berlin",
                CountryCode = "DE",
                IsDefaultShipping = true,
                RowVersion = [1]
            },
            new Address
            {
                UserId = userId,
                FullName = "Alice Smith",
                Street1 = "Nebenstraße 2",
                PostalCode = "54321",
                City = "Hamburg",
                CountryCode = "DE",
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var getUserWithAddresses = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var handler = new GetCurrentUserAddressesHandler(
            new StubCurrentUserService(userId),
            getUserWithAddresses,
            new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCurrentUserAddresses_Should_ReturnFail_WhenUserNotFound()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var getUserWithAddresses = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var handler = new GetCurrentUserAddressesHandler(
            new StubCurrentUserService(Guid.NewGuid()),
            getUserWithAddresses,
            new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task GetCurrentUserAddresses_Should_ReturnEmptyList_WhenUserHasNoAddresses()
    {
        await using var db = MiscIdentityTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "bob@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var getUserWithAddresses = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var handler = new GetCurrentUserAddressesHandler(
            new StubCurrentUserService(userId),
            getUserWithAddresses,
            new TestLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id, string email)
    {
        return new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            RowVersion = [1]
        };
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;
        public StubCurrentUserService(Guid userId) => _userId = userId;
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

    private sealed class MiscIdentityTestDbContext : DbContext, IAppDbContext
    {
        private MiscIdentityTestDbContext(DbContextOptions<MiscIdentityTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MiscIdentityTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MiscIdentityTestDbContext>()
                .UseInMemoryDatabase($"darwin_misc_identity_tests_{Guid.NewGuid()}")
                .Options;
            return new MiscIdentityTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<UserLogin>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.ProviderKey).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<Permission>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.RolePermissions);
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.User);
                builder.Ignore(x => x.Role);
            });

            modelBuilder.Entity<RolePermission>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

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

            modelBuilder.Entity<Address>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FullName).IsRequired();
                builder.Property(x => x.Street1).IsRequired();
                builder.Property(x => x.PostalCode).IsRequired();
                builder.Property(x => x.City).IsRequired();
                builder.Property(x => x.CountryCode).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
