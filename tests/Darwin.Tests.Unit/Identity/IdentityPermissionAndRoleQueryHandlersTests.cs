using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers admin-facing Identity permission/role/user-address query handlers:
/// <see cref="GetPermissionsPageHandler"/>, <see cref="GetPermissionForEditHandler"/>,
/// <see cref="GetRoleWithPermissionsForEditHandler"/>, and
/// <see cref="GetUserWithAddressesForEditHandler"/>.
/// </summary>
public sealed class IdentityPermissionAndRoleQueryHandlersTests
{
    // ─── GetPermissionsPageHandler ────────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsPage_Should_ReturnAllNonDeleted()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        db.Set<Permission>().AddRange(
            new Permission("perm.read", "Read", false, null) { RowVersion = [1] },
            new Permission("perm.write", "Write", false, null) { RowVersion = [1] },
            new Permission("perm.deleted", "Deleted", false, null) { IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(1, 10, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().NotContain(x => x.Key == "perm.deleted");
    }

    [Fact]
    public async Task GetPermissionsPage_Should_FilterBySearchTerm_OnKey()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        db.Set<Permission>().AddRange(
            new Permission("admin.full-access", "Admin Full Access", true, null) { RowVersion = [1] },
            new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(1, 10, "admin", ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Single().Key.Should().Be("admin.full-access");
    }

    [Fact]
    public async Task GetPermissionsPage_Should_FilterBySearchTerm_OnDisplayName()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        db.Set<Permission>().AddRange(
            new Permission("orders.read", "Orders Read", false, null) { RowVersion = [1] },
            new Permission("orders.write", "Orders Write", false, null) { RowVersion = [1] },
            new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(1, 10, "Orders", ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPermissionsPage_Should_RespectPagination()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        for (var i = 1; i <= 5; i++)
            db.Set<Permission>().Add(new Permission($"perm.{i:D2}", $"Permission {i:D2}", false, null) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(2, 2, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(5);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPermissionsPage_Should_ReturnFail_WhenPageNumberIsLessThanOne()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(0, 10, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidPagingParameters");
    }

    [Fact]
    public async Task GetPermissionsPage_Should_ReturnFail_WhenPageSizeIsLessThanOne()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var handler = new GetPermissionsPageHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(1, 0, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidPagingParameters");
    }

    // ─── GetPermissionForEditHandler ──────────────────────────────────────────

    [Fact]
    public async Task GetPermissionForEdit_Should_ReturnPermission_WhenFound()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var permission = new Permission("reports.view", "Reports View", false, "View reports") { RowVersion = [1] };
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(permission.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().Be(permission.Id);
        result.Value.Key.Should().Be("reports.view");
        result.Value.DisplayName.Should().Be("Reports View");
        result.Value.Description.Should().Be("View reports");
        result.Value.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task GetPermissionForEdit_Should_ReturnFail_WhenNotFound()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var handler = new GetPermissionForEditHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionNotFound");
    }

    [Fact]
    public async Task GetPermissionForEdit_Should_ReturnFail_WhenPermissionIsDeleted()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var permission = new Permission("ghost.perm", "Ghost", false, null) { IsDeleted = true, RowVersion = [1] };
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPermissionForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(permission.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionNotFound");
    }

    // ─── GetRoleWithPermissionsForEditHandler ─────────────────────────────────

    [Fact]
    public async Task GetRoleWithPermissionsForEdit_Should_ReturnRoleWithAssignedPermissions()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var role = new Role("manager", "Manager", false, null) { RowVersion = [1] };
        var perm1 = new Permission("orders.read", "Orders Read", false, null) { RowVersion = [1] };
        var perm2 = new Permission("orders.write", "Orders Write", false, null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        db.Set<Permission>().AddRange(perm1, perm2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<RolePermission>().Add(new RolePermission(role.Id, perm1.Id) { RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRoleWithPermissionsForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.RoleId.Should().Be(role.Id);
        result.Value.RoleDisplayName.Should().Be("Manager");
        result.Value.PermissionIds.Should().Contain(perm1.Id);
        result.Value.PermissionIds.Should().NotContain(perm2.Id);
        result.Value.AllPermissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRoleWithPermissionsForEdit_Should_ReturnFail_WhenRoleNotFound()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var handler = new GetRoleWithPermissionsForEditHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    [Fact]
    public async Task GetRoleWithPermissionsForEdit_Should_ReturnFail_WhenRoleIsDeleted()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var role = new Role("ghost", "Ghost Role", false, null) { IsDeleted = true, RowVersion = [1] };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRoleWithPermissionsForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    [Fact]
    public async Task GetRoleWithPermissionsForEdit_Should_ExcludeDeletedRolePermissions()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var role = new Role("editor", "Editor", false, null) { RowVersion = [1] };
        var perm = new Permission("content.edit", "Content Edit", false, null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(perm);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var softDeleted = new RolePermission(role.Id, perm.Id) { IsDeleted = true, RowVersion = [1] };
        db.Set<RolePermission>().Add(softDeleted);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRoleWithPermissionsForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PermissionIds.Should().BeEmpty();
    }

    // ─── GetUserWithAddressesForEditHandler ───────────────────────────────────

    [Fact]
    public async Task GetUserWithAddressesForEdit_Should_ReturnUserWithAddresses()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "alice@test.de");
        db.Set<User>().Add(user);
        db.Set<Address>().AddRange(
            new Address { UserId = userId, FullName = "Alice Smith", Street1 = "Hauptstraße 1", PostalCode = "12345", City = "Berlin", CountryCode = "DE", IsDefaultShipping = true, RowVersion = [1] },
            new Address { UserId = userId, FullName = "Alice Smith", Street1 = "Nebenstraße 2", PostalCode = "54321", City = "Hamburg", CountryCode = "DE", IsDefaultBilling = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().Be(userId);
        result.Value.Email.Should().Be("alice@test.de");
        result.Value.Addresses.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserWithAddressesForEdit_Should_ReturnFail_WhenUserNotFound()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var handler = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task GetUserWithAddressesForEdit_Should_ExcludeDeletedAddresses()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "bob@test.de");
        db.Set<User>().Add(user);
        db.Set<Address>().AddRange(
            new Address { UserId = userId, FullName = "Bob", Street1 = "Visible St", PostalCode = "11111", City = "Munich", CountryCode = "DE", RowVersion = [1] },
            new Address { UserId = userId, FullName = "Bob", Street1 = "Deleted St", PostalCode = "99999", City = "Cologne", CountryCode = "DE", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Addresses.Should().HaveCount(1);
        result.Value.Addresses[0].City.Should().Be("Munich");
    }

    [Fact]
    public async Task GetUserWithAddressesForEdit_Should_ReturnFail_WhenUserIsDeleted()
    {
        await using var db = PermissionRoleTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "deleted@test.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithAddressesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(userId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
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

    private sealed class PermissionRoleTestDbContext : DbContext, IAppDbContext
    {
        private PermissionRoleTestDbContext(DbContextOptions<PermissionRoleTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PermissionRoleTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PermissionRoleTestDbContext>()
                .UseInMemoryDatabase($"darwin_identity_perm_role_tests_{Guid.NewGuid()}")
                .Options;
            return new PermissionRoleTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

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
