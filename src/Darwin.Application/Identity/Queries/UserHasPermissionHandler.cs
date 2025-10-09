using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Checks whether a user effectively has a given permission via any of the assigned roles.
    /// </summary>
    public sealed class UserHasPermissionHandler
    {
        private readonly IAppDbContext _db;
        public UserHasPermissionHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Returns true if any of user's roles grants the specified permission.
        /// </summary>
        public async Task<Result<bool>> HandleAsync(Guid userId, string permissionKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
                return Result<bool>.Fail("Permission key is required.");

            var normalized = permissionKey.Trim().ToUpperInvariant();

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

            return Result<bool>.Ok(has);
        }
    }
}
