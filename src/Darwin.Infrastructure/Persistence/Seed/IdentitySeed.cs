using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Darwin.Shared.Constants;

namespace Darwin.Infrastructure.Persistence.Seed
{
    /// <summary>
    /// Adds baseline permissions, an Admin role (system), and an Admin user (system), idempotently.
    /// </summary>
    public sealed class IdentitySeed
    {
        private readonly DarwinDbContext _db;
        private readonly ILogger<IdentitySeed> _logger;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;

        public IdentitySeed(DarwinDbContext db, ILogger<IdentitySeed> logger, IUserPasswordHasher hasher, ISecurityStampService stamps)
        {
            _db = db; _logger = logger; _hasher = hasher; _stamps = stamps;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Identity (permissions/roles/users) ...");

            // TODO: Deterministic IDs (replace with WellKnownIds later)
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            // 1) Permissions
            var permissions = new[]
            {
                new { Key = "FullAdminAccess", Display="Full Admin Access", Description="Full access to admin panel and all operations.", IsSystem=true },
                new { Key = "ManageUsers", Display="Manage Users", Description="Create, edit, disable users.", IsSystem=true },
                new { Key = "ManageRoles", Display="Manage Roles", Description="Create, edit roles and assign permissions.", IsSystem=true },
                new { Key = "ManagePermissions", Display="Manage Permissions", Description="Create new permissions and descriptions.", IsSystem=true },
                new { Key = "AccessAdminPanel", Display="Access Admin Panel", Description="Can access admin area.", IsSystem=true },
                new { Key = "RecycleBinAccess", Display="Recycle Bin Access", Description="Can access recycle bin.", IsSystem=true },
                new { Key = "AccessMemberArea", Display="Access Member Area", Description="Can access customer/member area.", IsSystem=true }
            };

            foreach (var p in permissions)
            {
                if (!await _db.Permissions.AnyAsync(x => x.Key == p.Key, ct))
                {
                    _db.Permissions.Add(new Permission(p.Key, p.Display, p.IsSystem, p.Description));
                }
            }
            await _db.SaveChangesAsync(ct);

            // 2) Admin Role
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == adminRoleId, ct);
            if (adminRole is null)
            {
                adminRole = new Role("Admin", "Admin", isSystem: true, "The Role of System Administrators");
                // enforce deterministic ID if desired:
                adminRole.GetType().GetProperty(nameof(Role.Id))!.SetValue(adminRole, adminRoleId);
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync(ct);
            }

            // 2.1) Assign all permissions to Admin
            var permIds = await _db.Permissions.Select(p => p.Id).ToListAsync(ct);
            foreach (var pid in permIds)
            {
                var exists = await _db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == pid, ct);
                if (!exists)
                {
                    _db.RolePermissions.Add(new RolePermission(adminRole.Id, pid));
                }
            }
            await _db.SaveChangesAsync(ct);

            // 3) Admin User
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminUserId, ct);
            if (admin is null)
            {
                var hash = _hasher.Hash("admin"); // TODO: rotate after first run
                admin = new User(email: "admin@darwin.de", passwordHash: hash, securityStamp: _stamps.NewStamp())
                {
                    IsSystem = true,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };
                // force deterministic ID
                admin.GetType().GetProperty(nameof(User.Id))!.SetValue(admin, adminUserId);

                _db.Users.Add(admin);
                await _db.SaveChangesAsync(ct);
            }

            // 3.1) Assign Admin role to Admin user
            var urExists = await _db.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id, ct);
            if (!urExists)
            {
                _db.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Identity seeding done.");
        }
    }
}
