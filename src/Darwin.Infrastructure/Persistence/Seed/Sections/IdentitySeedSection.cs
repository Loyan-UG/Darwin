using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Domain.Entities.Identity;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds identity data: permissions, roles, and demo users with addresses.
    /// Idempotent by natural keys (Permission.Key, Role.Key, User.Email).
    /// </summary>
    public sealed class IdentitySeedSection
    {
        private readonly ILogger<IdentitySeedSection> _logger;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;

        /// <summary>
        /// Creates a new identity seed section with password hashing and stamps support.
        /// </summary>
        public IdentitySeedSection(
            ILogger<IdentitySeedSection> logger,
            IUserPasswordHasher hasher,
            ISecurityStampService stamps)
        {
            _logger = logger;
            _hasher = hasher;
            _stamps = stamps;
        }

        /// <summary>
        /// Executes seeding against the provided DbContext.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Identity (permissions/roles/users) ...");

            // ----------------------------
            // 1) Permissions (system)
            // ----------------------------
            var permissionDefs = new (string Key, string Display, string? Description)[]
            {
                ("FullAdminAccess", "Full Admin Access", "Unrestricted administrative access"),
                ("ManageUsers", "Manage Users", "Create, edit, and deactivate users"),
                ("ManageRoles", "Manage Roles", "Create, edit, and assign roles"),
                ("ManagePermissions", "Manage Permissions", "Add/remove permissions and bindings"),
                ("AccessAdminPanel", "Access Admin Panel", "Can sign in to the admin UI"),
                ("RecycleBinAccess", "Recycle Bin Access", "View and restore soft-deleted items"),
                ("AccessMemberArea", "Access Member Area", "Signed-in members area access")
            };

            foreach (var (key, display, desc) in permissionDefs)
            {
                var exists = await db.Set<Permission>().AnyAsync(p => p.Key == key && !p.IsDeleted, ct);
                if (!exists)
                {
                    // Permission ctor: (key, displayName, isSystem, description) — IsSystem is set via ctor only.
                    db.Add(new Permission(key, display, isSystem: true, description: desc));
                }
            }
            await db.SaveChangesAsync(ct);

            // ----------------------------
            // 2) Roles (system) + role-permissions
            // ----------------------------
            // Administrators
            var adminRole = await db.Set<Role>()
                .FirstOrDefaultAsync(r => r.Key == "administrators" && !r.IsDeleted, ct);

            if (adminRole is null)
            {
                adminRole = new Role(
                    key: "administrators",
                    displayName: "Administrators",
                    isSystem: true,
                    description: "System administrators with full access");
                adminRole.Id = WellKnownIds.AdministratorsRoleId;
                db.Add(adminRole);
                await db.SaveChangesAsync(ct);
            }

            // Members
            var membersRole = await db.Set<Role>()
                .FirstOrDefaultAsync(r => r.Key == "members" && !r.IsDeleted, ct);

            if (membersRole is null)
            {
                membersRole = new Role(
                    key: "members",
                    displayName: "Site Members",
                    isSystem: true,
                    description: "Registered site members with standard access");
                membersRole.Id = WellKnownIds.MembersRoleId;
                db.Add(membersRole);
                await db.SaveChangesAsync(ct);
            }

            // Bind permissions
            var allPerms = await db.Set<Permission>()
                .Where(p => !p.IsDeleted)
                .ToListAsync(ct);

            var pFullAdmin = allPerms.First(p => p.Key == "FullAdminAccess");
            var pMemberArea = allPerms.First(p => p.Key == "AccessMemberArea");

            // Admins → FullAdminAccess
            bool adminRpExists = await db.Set<RolePermission>()
                .AnyAsync(x => x.RoleId == adminRole.Id && x.PermissionId == pFullAdmin.Id, ct);
            if (!adminRpExists)
                db.Add(new RolePermission(adminRole.Id, pFullAdmin.Id));

            // Members → AccessMemberArea
            bool membersRpExists = await db.Set<RolePermission>()
                .AnyAsync(x => x.RoleId == membersRole.Id && x.PermissionId == pMemberArea.Id, ct);
            if (!membersRpExists)
                db.Add(new RolePermission(membersRole.Id, pMemberArea.Id));

            await db.SaveChangesAsync(ct);

            // ----------------------------
            // 3) Users (admin + demos) + addresses + user-roles
            // ----------------------------
            // Admin (dev-only password)
            var adminEmail = "admin@darwin.de";
            var adminUser = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == adminEmail && !u.IsDeleted, ct);
            if (adminUser is null)
            {
                var stamp = _stamps.NewStamp();
                adminUser = new User(
                    email: adminEmail,
                    passwordHash: _hasher.Hash("Admin123!"), // DEV-ONLY. Force change on first run (see backlog).
                    securityStamp: stamp)
                {
                    Id = WellKnownIds.AdministratorUserId,
                    FirstName = "System",
                    LastName = "Administrator",
                    IsSystem = true,
                    IsActive = true,
                    Locale = "de-DE",
                    Timezone = "Europe/Berlin",
                    Currency = "EUR"
                };
                db.Add(adminUser);
                await db.SaveChangesAsync(ct);

                // Link admin to Administrators role
                db.Add(new UserRole(adminUser.Id, adminRole.Id));
                await db.SaveChangesAsync(ct);
            }

            // Demo users
            var demos = new (string Email, string First, string Last, string Phone, string City, string Postal, string Street1)[]
            {
                ("alice@darwin.com", "Alice", "Müller", "+49 30 1234567", "Berlin", "10115", "Hauptstr. 1"),
                ("bob@darwin.com",   "Bob",   "Schmidt", "+49 89 7654321", "München", "80331", "Marienplatz 8"),
                ("carol@darwin.com", "Carol", "Fischer", "+49 40 1122334", "Hamburg", "20095", "Spitalerstraße 3")
            };

            foreach (var d in demos)
            {
                var u = await db.Set<User>().FirstOrDefaultAsync(x => x.Email == d.Email && !x.IsDeleted, ct);
                if (u is null)
                {
                    var stamp = _stamps.NewStamp();
                    u = new User(
                        email: d.Email,
                        passwordHash: _hasher.Hash("User123!"), // DEV-ONLY convenience
                        securityStamp: stamp)
                    {
                        FirstName = d.First,
                        LastName = d.Last,
                        IsActive = true,
                        Locale = "de-DE",
                        Timezone = "Europe/Berlin",
                        Currency = "EUR",
                    };
                    db.Add(u);
                    await db.SaveChangesAsync(ct);

                    // Members role
                    db.Add(new UserRole(u.Id, membersRole.Id));

                    // Address (German formatting)
                    var addr = new Address
                    {
                        UserId = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}",
                        Street1 = d.Street1,
                        Street2 = null,
                        PostalCode = d.Postal,
                        City = d.City,
                        CountryCode = "DE",
                        PhoneE164 = d.Phone,
                        IsDefaultBilling = true,
                        IsDefaultShipping = true
                    };
                    db.Add(addr);
                    await db.SaveChangesAsync(ct);

                    u.DefaultBillingAddressId = addr.Id;
                    u.DefaultShippingAddressId = addr.Id;
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    // Ensure Members role exists for existing demo users (idempotent)
                    bool hasMembers = await db.Set<UserRole>()
                        .AnyAsync(x => x.UserId == u.Id && x.RoleId == membersRole.Id, ct);
                    if (!hasMembers)
                    {
                        db.Add(new UserRole(u.Id, membersRole.Id));
                        await db.SaveChangesAsync(ct);
                    }
                }
            }

            _logger.LogInformation("Identity seeding done.");
        }
    }
}
