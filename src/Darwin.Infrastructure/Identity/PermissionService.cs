using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Identity
{
    /// <summary>
    /// Provides permission evaluation against the database using the application's EF Core context.
    /// Resolves effective permissions through Role -> RolePermission -> Permission relationships.
    /// This service is intentionally read-only and side-effect-free.
    /// </summary>
    public sealed class PermissionService : IPermissionService
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Binds to the application's DbContext abstraction for read-only queries.
        /// </summary>
        public PermissionService(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns true when the user has the given permission key via any of their roles.
        /// Comparison is case-insensitive on the key and ignores soft-deleted links.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="permissionKey">Stable permission key (e.g. "FullAdminAccess").</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<bool> HasAsync(Guid userId, string permissionKey, CancellationToken ct = default)
        {
            if (userId == Guid.Empty) return false;
            if (string.IsNullOrWhiteSpace(permissionKey)) return false;

            var normalized = permissionKey.Trim().ToUpperInvariant();

            // SELECT 1
            // FROM   UserRoles ur
            // JOIN   RolePermissions rp ON ur.RoleId = rp.RoleId
            // JOIN   Permissions p ON p.Id = rp.PermissionId
            // WHERE  ur.UserId = @userId
            //   AND  ur.IsDeleted = 0 AND rp.IsDeleted = 0 AND p.IsDeleted = 0
            //   AND  UPPER(p.Key) = @normalized
            var has = await
                (from ur in _db.Set<Darwin.Domain.Entities.Identity.UserRole>().AsNoTracking()
                 join rp in _db.Set<Darwin.Domain.Entities.Identity.RolePermission>().AsNoTracking()
                     on ur.RoleId equals rp.RoleId
                 join p in _db.Set<Darwin.Domain.Entities.Identity.Permission>().AsNoTracking()
                     on rp.PermissionId equals p.Id
                 where ur.UserId == userId
                       && !ur.IsDeleted && !rp.IsDeleted && !p.IsDeleted
                       && p.Key.ToUpper() == normalized
                 select p.Id)
                .AnyAsync(ct);

            return has;
        }

        /// <summary>
        /// Returns a distinct set of all permission keys granted to the user through their roles.
        /// Keys are normalized to upper-case for consistent, case-insensitive comparisons in callers.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<HashSet<string>> GetAllAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var keys = await
                (from ur in _db.Set<Darwin.Domain.Entities.Identity.UserRole>().AsNoTracking()
                 join rp in _db.Set<Darwin.Domain.Entities.Identity.RolePermission>().AsNoTracking()
                     on ur.RoleId equals rp.RoleId
                 join p in _db.Set<Darwin.Domain.Entities.Identity.Permission>().AsNoTracking()
                     on rp.PermissionId equals p.Id
                 where ur.UserId == userId
                       && !ur.IsDeleted && !rp.IsDeleted && !p.IsDeleted
                 select p.Key)
                .ToListAsync(ct);

            // Normalize to upper for stable, case-insensitive set semantics.
            return new HashSet<string>(keys.Select(k => k.ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
        }
    }
}
