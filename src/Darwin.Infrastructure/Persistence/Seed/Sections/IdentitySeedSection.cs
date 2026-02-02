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
                ("AccessMemberArea", "Access Member Area", "Signed-in members area access"),
                ("AccessLoyaltyBusiness", "Access Loyalty Business Features",
                    "Can process loyalty scan sessions and related business operations via Web and WebApi.")
            };

            foreach (var (key, display, desc) in permissionDefs)
            {
                var exists = await db.Set<Permission>().AnyAsync(p => p.Key == key && !p.IsDeleted, ct);
                if (!exists)
                {
                    db.Add(new Permission(key, display, isSystem: true, description: desc));
                }
            }
            await db.SaveChangesAsync(ct);

            // ----------------------------
            // 2) Roles (system) + role-permissions
            // ----------------------------
            var adminRole = await EnsureRoleAsync(db, "administrators", "Administrators", true,
                "System administrators with full access", WellKnownIds.AdministratorsRoleId, ct);

            var membersRole = await EnsureRoleAsync(db, "members", "Site Members", true,
                "Registered site members with standard access", WellKnownIds.MembersRoleId, ct);

            var businessRole = await EnsureRoleAsync(db, "business", "Business Users", true,
                "Business app users with loyalty access", null, ct);

            var webUsersRole = await EnsureRoleAsync(db, "web-users", "Web Users", false,
                "Website-only accounts with limited access", null, ct);

            // Bind permissions
            var allPerms = await db.Set<Permission>().Where(p => !p.IsDeleted).ToListAsync(ct);

            var pFullAdmin = allPerms.First(p => p.Key == "FullAdminAccess");
            var pMemberArea = allPerms.First(p => p.Key == "AccessMemberArea");
            var pLoyaltyBiz = allPerms.First(p => p.Key == "AccessLoyaltyBusiness");
            var pAccessAdminPanel = allPerms.First(p => p.Key == "AccessAdminPanel");

            await EnsureRolePermissionAsync(db, adminRole.Id, pFullAdmin.Id, ct);
            await EnsureRolePermissionAsync(db, membersRole.Id, pMemberArea.Id, ct);
            await EnsureRolePermissionAsync(db, businessRole.Id, pLoyaltyBiz.Id, ct);
            await EnsureRolePermissionAsync(db, webUsersRole.Id, pAccessAdminPanel.Id, ct);

            // ----------------------------
            // 3) Users (Admin)
            // ----------------------------
            var adminEmail = "admin@darwin.de";
            var adminUser = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == adminEmail && !u.IsDeleted, ct);
            if (adminUser is null)
            {
                var stamp = _stamps.NewStamp();
                adminUser = new User(
                    email: adminEmail,
                    passwordHash: _hasher.Hash("Admin123!"),
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

                db.Add(new UserRole(adminUser.Id, adminRole.Id));
                await db.SaveChangesAsync(ct);
            }

            // ----------------------------
            // 4) Seed 30 users (10 business, 10 consumer, 10 web)
            // ----------------------------
            var businessUsers = new (string Email, string First, string Last, string City, string Postal, string Street)[]
            {
                ("biz1@darwin.de","Lena","Becker","Berlin","10115","Invalidenstraße 117"),
                ("biz2@darwin.de","Jonas","Klein","München","80331","Marienplatz 8"),
                ("biz3@darwin.de","Mara","Wagner","Köln","50672","Hohenzollernring 22"),
                ("biz4@darwin.de","Felix","Scholz","Hamburg","20095","Spitalerstraße 3"),
                ("biz5@darwin.de","Nora","Hoffmann","Frankfurt","60313","Zeil 105"),
                ("biz6@darwin.de","Paul","Schneider","Stuttgart","70173","Königstraße 12"),
                ("biz7@darwin.de","Julia","Fischer","Düsseldorf","40212","Schadowstraße 55"),
                ("biz8@darwin.de","Tim","Kaiser","Leipzig","04109","Grimmaische Straße 14"),
                ("biz9@darwin.de","Sara","Müller","Dresden","01067","Altmarkt 10"),
                ("biz10@darwin.de","Max","Weber","Nürnberg","90402","Königstraße 41")
            };

            var consumerUsers = new (string Email, string First, string Last, string City, string Postal, string Street)[]
            {
                ("cons1@darwin.de","Emma","Krüger","Berlin","10115","Hauptstraße 1"),
                ("cons2@darwin.de","Ben","Seidel","München","80331","Sendlinger Straße 5"),
                ("cons3@darwin.de","Lia","Brandt","Köln","50672","Aachener Straße 12"),
                ("cons4@darwin.de","Noah","Berg","Hamburg","20095","Mönckebergstraße 9"),
                ("cons5@darwin.de","Mia","Vogel","Frankfurt","60313","Neue Kräme 7"),
                ("cons6@darwin.de","Tom","Neumann","Stuttgart","70173","Calwer Straße 3"),
                ("cons7@darwin.de","Lea","Hartmann","Düsseldorf","40212","Flinger Straße 10"),
                ("cons8@darwin.de","Leon","Peters","Leipzig","04109","Katharinenstraße 2"),
                ("cons9@darwin.de","Anna","Schmidt","Dresden","01067","Wilsdruffer Straße 6"),
                ("cons10@darwin.de","Erik","Lang","Nürnberg","90402","Breite Gasse 15")
            };

            var webUsers = new (string Email, string First, string Last, string City, string Postal, string Street)[]
            {
                ("web1@darwin.de","Clara","Meier","Berlin","10115","Torstraße 19"),
                ("web2@darwin.de","Jan","Lorenz","München","80331","Kaufingerstraße 3"),
                ("web3@darwin.de","Sarah","Huber","Köln","50672","Roonstraße 4"),
                ("web4@darwin.de","Tobias","Brand","Hamburg","20095","Bergstraße 11"),
                ("web5@darwin.de","Laura","Zimmer","Frankfurt","60313","Große Bockenheimer 8"),
                ("web6@darwin.de","Moritz","Kuhn","Stuttgart","70173","Rotebühlstraße 29"),
                ("web7@darwin.de","Sophie","Graf","Düsseldorf","40212","Kasernenstraße 9"),
                ("web8@darwin.de","Daniel","Franz","Leipzig","04109","Reichsstraße 1"),
                ("web9@darwin.de","Isabel","Wolf","Dresden","01067","Prager Straße 5"),
                ("web10@darwin.de","Lukas","Arnold","Nürnberg","90402","Kornmarkt 4")
            };

            foreach (var u in businessUsers)
                await EnsureUserAsync(db, u, "Business123!", businessRole.Id, ct);

            foreach (var u in consumerUsers)
                await EnsureUserAsync(db, u, "Consumer123!", membersRole.Id, ct);

            foreach (var u in webUsers)
                await EnsureUserAsync(db, u, "Web123!", webUsersRole.Id, ct);

            _logger.LogInformation("Identity seeding done.");
        }

        private async Task<User> EnsureUserAsync(
            DarwinDbContext db,
            (string Email, string First, string Last, string City, string Postal, string Street) data,
            string password,
            Guid roleId,
            CancellationToken ct)
        {
            var user = await db.Set<User>()
                .FirstOrDefaultAsync(x => x.Email == data.Email && !x.IsDeleted, ct);

            if (user is null)
            {
                var stamp = _stamps.NewStamp();
                user = new User(
                    email: data.Email,
                    passwordHash: _hasher.Hash(password),
                    securityStamp: stamp)
                {
                    FirstName = data.First,
                    LastName = data.Last,
                    IsActive = true,
                    Locale = "de-DE",
                    Timezone = "Europe/Berlin",
                    Currency = "EUR",
                };
                db.Add(user);
                await db.SaveChangesAsync(ct);

                db.Add(new UserRole(user.Id, roleId));

                var addr = new Address
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Street1 = data.Street,
                    Street2 = null,
                    PostalCode = data.Postal,
                    City = data.City,
                    CountryCode = "DE",
                    PhoneE164 = null,
                    IsDefaultBilling = true,
                    IsDefaultShipping = true
                };
                db.Add(addr);
                await db.SaveChangesAsync(ct);

                user.DefaultBillingAddressId = addr.Id;
                user.DefaultShippingAddressId = addr.Id;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var hasRole = await db.Set<UserRole>()
                    .AnyAsync(x => x.UserId == user.Id && x.RoleId == roleId, ct);
                if (!hasRole)
                {
                    db.Add(new UserRole(user.Id, roleId));
                    await db.SaveChangesAsync(ct);
                }
            }

            return user;
        }

        private static async Task<Role> EnsureRoleAsync(
            DarwinDbContext db,
            string key,
            string displayName,
            bool isSystem,
            string? description,
            Guid? forcedId,
            CancellationToken ct)
        {
            var role = await db.Set<Role>()
                .FirstOrDefaultAsync(r => r.Key == key && !r.IsDeleted, ct);

            if (role is null)
            {
                role = new Role(key, displayName, isSystem, description);
                if (forcedId.HasValue) role.Id = forcedId.Value;
                db.Add(role);
                await db.SaveChangesAsync(ct);
            }

            return role;
        }

        private static async Task EnsureRolePermissionAsync(
            DarwinDbContext db,
            Guid roleId,
            Guid permissionId,
            CancellationToken ct)
        {
            var exists = await db.Set<RolePermission>()
                .AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId && !x.IsDeleted, ct);

            if (!exists)
            {
                db.Add(new RolePermission(roleId, permissionId));
                await db.SaveChangesAsync(ct);
            }
        }
    }
}