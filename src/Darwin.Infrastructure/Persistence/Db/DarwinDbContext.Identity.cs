using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Partial DbContext exposing Identity DbSets.
    /// Keep Identity in a separate partial to avoid cluttering the main file.
    /// </summary>
    public sealed partial class DarwinDbContext : DbContext, IAppDbContext
    {
        // Identity
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserLogin> UserLogins => Set<UserLogin>();
        public DbSet<UserToken> UserTokens => Set<UserToken>();
        public DbSet<UserTwoFactorSecret> UserTwoFactorSecrets => Set<UserTwoFactorSecret>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<Address> Addresses => Set<Address>();

        /// <summary>
        /// Registered mobile device installations for users.
        /// </summary>
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();

        /// <summary>
        /// Denormalized per-user engagement snapshot used for segmentation and personalization.
        /// Intended uniqueness is enforced at the database level via a unique index on UserId.
        /// </summary>
        public DbSet<UserEngagementSnapshot> UserEngagementSnapshots => Set<UserEngagementSnapshot>();
    }
}
    