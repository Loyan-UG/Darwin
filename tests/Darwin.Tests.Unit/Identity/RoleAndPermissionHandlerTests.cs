using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
/// Unit tests for role and permission command handlers:
/// <see cref="CreateRoleHandler"/>, <see cref="UpdateRoleHandler"/>, <see cref="DeleteRoleHandler"/>,
/// <see cref="CreatePermissionHandler"/>, <see cref="UpdatePermissionHandler"/>,
/// <see cref="SoftDeletePermissionHandler"/>, <see cref="AssignPermissionToRoleHandler"/>,
/// and <see cref="AssignRoleToUserHandler"/>.
/// </summary>
public sealed class RoleAndPermissionHandlerTests
{
    // ─── CreateRoleHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRole_Should_PersistRole_WithNormalizedKey()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = new CreateRoleHandler(db, new RoleCreateValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new RoleCreateDto
        {
            Key = "manager",
            DisplayName = "Manager",
            Description = "Store manager role"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var persisted = await db.Set<Role>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.Key.Should().Be("manager");
        persisted.NormalizedName.Should().Be("MANAGER");
        persisted.DisplayName.Should().Be("Manager");
    }

    [Fact]
    public async Task CreateRole_Should_Fail_WhenKeyAlreadyExists()
    {
        await using var db = RolePermissionTestDbContext.Create();
        db.Set<Role>().Add(new Role("admin", "Administrators", isSystem: true, description: null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateRoleHandler(db, new RoleCreateValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new RoleCreateDto
        {
            Key = "admin",
            DisplayName = "Admin Duplicate"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleKeyAlreadyExists");
    }

    // ─── UpdateRoleHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_Should_UpdateDisplayNameAndDescription()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("editor", "Old Name", isSystem: false, description: "Old desc") { RowVersion = [1] };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateRoleHandler(db, new RoleEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new RoleEditDto
        {
            Id = role.Id,
            RowVersion = role.RowVersion,
            DisplayName = "Content Editor",
            Description = "Manages content"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Role>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        persisted.DisplayName.Should().Be("Content Editor");
        persisted.Description.Should().Be("Manages content");
    }

    [Fact]
    public async Task UpdateRole_Should_Fail_WhenRoleIsSystem()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("superadmin", "Super Admin", isSystem: true, description: null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateRoleHandler(db, new RoleEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new RoleEditDto
        {
            Id = role.Id,
            RowVersion = role.RowVersion,
            DisplayName = "Changed Name"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("SystemRolesCannotBeEdited");
    }

    [Fact]
    public async Task UpdateRole_Should_Fail_WhenRoleNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = new UpdateRoleHandler(db, new RoleEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new RoleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [],
            DisplayName = "Any"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    // ─── DeleteRoleHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRole_Should_SoftDeleteRole()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("viewer", "Viewer", isSystem: false, description: null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRoleHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(role.Id, role.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Role>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRole_Should_Fail_WhenRoleIsSystem()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("protected", "Protected", isSystem: true, description: null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRoleHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(role.Id, role.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("SystemProtectedRoleCannotBeDeleted");
    }

    [Fact]
    public async Task DeleteRole_Should_Fail_WhenRoleNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = new DeleteRoleHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), [1], TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    // ─── CreatePermissionHandler ──────────────────────────────────────────────

    [Fact]
    public async Task CreatePermission_Should_PersistPermission()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = new CreatePermissionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync("catalog.read", "Catalog Read", "Read catalog", ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var persisted = await db.Set<Permission>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.Key.Should().Be("catalog.read");
        persisted.DisplayName.Should().Be("Catalog Read");
    }

    [Fact]
    public async Task CreatePermission_Should_Fail_WhenKeyAlreadyExists()
    {
        await using var db = RolePermissionTestDbContext.Create();
        db.Set<Permission>().Add(new Permission("orders.manage", "Orders Manage", false, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreatePermissionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync("orders.manage", "Orders Manage", null, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionKeyAlreadyExists");
    }

    // ─── UpdatePermissionHandler ──────────────────────────────────────────────

    [Fact]
    public async Task UpdatePermission_Should_UpdateDisplayNameAndDescription()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var permission = new Permission("catalog.write", "Old Name", false, "Old description") { RowVersion = [1] };
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePermissionHandler(db, new PermissionEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new PermissionEditDto
        {
            Id = permission.Id,
            RowVersion = permission.RowVersion,
            DisplayName = "Catalog Write",
            Description = "Write access to catalog"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Permission>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        persisted.DisplayName.Should().Be("Catalog Write");
        persisted.Description.Should().Be("Write access to catalog");
    }

    [Fact]
    public async Task UpdatePermission_Should_Fail_WhenNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = new UpdatePermissionHandler(db, new PermissionEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new PermissionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            DisplayName = "Something"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionNotFound");
    }

    // ─── SoftDeletePermissionHandler ─────────────────────────────────────────

    [Fact]
    public async Task SoftDeletePermission_Should_MarkAsDeleted_WhenNotAssigned()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var permission = new Permission("reports.view", "Reports View", false, null) { RowVersion = [1] };
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeletePermissionHandler(db, new PermissionDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new PermissionDeleteDto
        {
            Id = permission.Id,
            RowVersion = permission.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Permission>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeletePermission_Should_Fail_WhenPermissionIsSystem()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var permission = new Permission("system.full-access", "System Full", true, null) { RowVersion = [1] };
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeletePermissionHandler(db, new PermissionDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new PermissionDeleteDto
        {
            Id = permission.Id,
            RowVersion = permission.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("SystemPermissionsCannotBeDeleted");
    }

    [Fact]
    public async Task SoftDeletePermission_Should_Fail_WhenAssignedToRole()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("staff", "Staff", false, null);
        var permission = new Permission("billing.view", "Billing View", false, null) { RowVersion = [1] };
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<RolePermission>().Add(new RolePermission(role.Id, permission.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeletePermissionHandler(db, new PermissionDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new PermissionDeleteDto
        {
            Id = permission.Id,
            RowVersion = permission.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PermissionAssignedToRolesCannotDelete");
    }

    // ─── AssignPermissionToRoleHandler ────────────────────────────────────────

    [Fact]
    public async Task AssignPermissionToRole_Should_CreateAssignment()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("clerk", "Clerk", false, null);
        var permission = new Permission("inventory.view", "Inventory View", false, null);
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignPermissionToRoleHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(role.Id, permission.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var count = await db.Set<RolePermission>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AssignPermissionToRole_Should_BeIdempotent_WhenAssignedTwice()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("supervisor", "Supervisor", false, null);
        var permission = new Permission("reports.export", "Reports Export", false, null);
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignPermissionToRoleHandler(db, new TestLocalizer());

        await handler.HandleAsync(role.Id, permission.Id, TestContext.Current.CancellationToken);
        var result = await handler.HandleAsync(role.Id, permission.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var count = await db.Set<RolePermission>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AssignPermissionToRole_Should_Fail_WhenRoleNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var permission = new Permission("catalog.delete", "Catalog Delete", false, null);
        db.Set<Permission>().Add(permission);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignPermissionToRoleHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), permission.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RoleNotFound");
    }

    // ─── AssignRoleToUserHandler ──────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleToUser_Should_CreateUserRoleAssignment()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var user = CreateUser("role-assign@darwin.de");
        var role = new Role("cashier", "Cashier", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(user.Id, role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var count = await db.Set<UserRole>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AssignRoleToUser_Should_BeIdempotent_WhenAssignedTwice()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var user = CreateUser("idempotent-role@darwin.de");
        var role = new Role("analyst", "Analyst", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        await handler.HandleAsync(user.Id, role.Id, TestContext.Current.CancellationToken);
        var result = await handler.HandleAsync(user.Id, role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var count = await db.Set<UserRole>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AssignRoleToUser_Should_Fail_WhenUserNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("operator", "Operator", false, null);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignRoleToUserHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), role.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── UpdateRolePermissionsHandler ─────────────────────────────────────────

    private static UpdateRolePermissionsHandler CreateUpdateRolePermissionsHandler(RolePermissionTestDbContext db) =>
        new(db, new FixedClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)), new TestLocalizer());

    [Fact]
    public async Task UpdateRolePermissions_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = CreateUpdateRolePermissionsHandler(db);

        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            PermissionIds = new List<Guid>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected before any DB access");
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_Fail_WhenRoleNotFound()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var handler = CreateUpdateRolePermissionsHandler(db);

        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            PermissionIds = new List<Guid>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent role must fail");
        result.Error.Should().Be("RoleNotFound");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var role = new Role("editor", "Editor", false, null) { RowVersion = new byte[] { 1, 2, 3 } };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = new byte[] { 9, 9, 9 }, // stale
            PermissionIds = new List<Guid>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger a concurrency conflict");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_Fail_WhenPermissionIdsContainInvalid()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var role = new Role("editor", "Editor", false, null) { RowVersion = rowVersion };
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = rowVersion,
            PermissionIds = new List<Guid> { Guid.NewGuid() } // non-existent
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("invalid permission IDs must be rejected");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_AddNewPermissions_WhenValid()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var role = new Role("editor", "Editor", false, null) { RowVersion = rowVersion };
        var perm1 = new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = new byte[] { 1 } };
        var perm2 = new Permission("orders.read", "Orders Read", false, null) { RowVersion = new byte[] { 1 } };
        db.Set<Role>().Add(role);
        db.Set<Permission>().AddRange(perm1, perm2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = rowVersion,
            PermissionIds = new List<Guid> { perm1.Id, perm2.Id }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var links = await db.Set<RolePermission>()
            .Where(x => x.RoleId == role.Id && !x.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);
        links.Select(x => x.PermissionId).Should().BeEquivalentTo(new[] { perm1.Id, perm2.Id });
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_RemovePermissions_NotInNewList()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var role = new Role("editor", "Editor", false, null) { RowVersion = rowVersion };
        var perm1 = new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = new byte[] { 1 } };
        var perm2 = new Permission("orders.read", "Orders Read", false, null) { RowVersion = new byte[] { 1 } };
        db.Set<Role>().Add(role);
        db.Set<Permission>().AddRange(perm1, perm2);
        // Pre-assign both permissions
        db.Set<RolePermission>().Add(new RolePermission(role.Id, perm1.Id));
        db.Set<RolePermission>().Add(new RolePermission(role.Id, perm2.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = rowVersion,
            PermissionIds = new List<Guid> { perm1.Id } // perm2 removed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var active = await db.Set<RolePermission>()
            .Where(x => x.RoleId == role.Id && !x.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);
        active.Should().ContainSingle(x => x.PermissionId == perm1.Id);
        var softDeleted = await db.Set<RolePermission>()
            .Where(x => x.RoleId == role.Id && x.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);
        softDeleted.Should().ContainSingle(x => x.PermissionId == perm2.Id);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_ClearAll_WhenEmptyListProvided()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var role = new Role("editor", "Editor", false, null) { RowVersion = rowVersion };
        var perm = new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = new byte[] { 1 } };
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(perm);
        db.Set<RolePermission>().Add(new RolePermission(role.Id, perm.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = rowVersion,
            PermissionIds = new List<Guid>() // clear all
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var active = await db.Set<RolePermission>()
            .Where(x => x.RoleId == role.Id && !x.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);
        active.Should().BeEmpty("all permissions should be soft-removed");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_RestoreSoftDeletedLink_WhenPermissionReAdded()
    {
        await using var db = RolePermissionTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var role = new Role("editor", "Editor", false, null) { RowVersion = rowVersion };
        var perm = new Permission("catalog.read", "Catalog Read", false, null) { RowVersion = new byte[] { 1 } };
        db.Set<Role>().Add(role);
        db.Set<Permission>().Add(perm);
        // A previously deleted link
        db.Set<RolePermission>().Add(new RolePermission(role.Id, perm.Id) { IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateRolePermissionsHandler(db);
        var result = await handler.HandleAsync(new RolePermissionsUpdateDto
        {
            RoleId = role.Id,
            RowVersion = rowVersion,
            PermissionIds = new List<Guid> { perm.Id }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var linkCount = await db.Set<RolePermission>().CountAsync(TestContext.Current.CancellationToken);
        linkCount.Should().Be(1, "restore should reuse the existing row, not create a duplicate");
        var link = await db.Set<RolePermission>().SingleAsync(TestContext.Current.CancellationToken);
        link.IsDeleted.Should().BeFalse("restored link should be active");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email)
    {
        return new User(email, "hashed", "stamp")
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

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) { _utcNow = utcNow; }
        public DateTime UtcNow => _utcNow;
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

    private sealed class RolePermissionTestDbContext : DbContext, IAppDbContext
    {
        private RolePermissionTestDbContext(DbContextOptions<RolePermissionTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RolePermissionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RolePermissionTestDbContext>()
                .UseInMemoryDatabase($"darwin_role_permission_handler_tests_{Guid.NewGuid()}")
                .Options;
            return new RolePermissionTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.NormalizedName).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.RolePermissions);
            });

            modelBuilder.Entity<Permission>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<RolePermission>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RoleId).IsRequired();
                builder.Property(x => x.PermissionId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Role);
                builder.Ignore(x => x.Permission);
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RoleId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.User);
                builder.Ignore(x => x.Role);
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
        }
    }
}
