using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads a role together with its current permission assignments for editing.
    /// Projects to RolePermissionsEditDto and includes the full permission catalog for UI.
    /// </summary>
    public sealed class GetRoleWithPermissionsForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetRoleWithPermissionsForEditHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Returns the role edit payload or failure if the role is missing or deleted.
        /// </summary>
        public async Task<Result<RolePermissionsEditDto>> HandleAsync(Guid roleId, CancellationToken ct = default)
        {
            var role = await _db.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, ct);

            if (role is null)
                return Result<RolePermissionsEditDto>.Fail(_localizer["RoleNotFound"]);

            var currentPermIds = await (
                    from rolePermission in _db.Set<RolePermission>().AsNoTracking()
                    join permission in _db.Set<Permission>().AsNoTracking() on rolePermission.PermissionId equals permission.Id
                    where rolePermission.RoleId == roleId && !rolePermission.IsDeleted && !permission.IsDeleted
                    select rolePermission.PermissionId)
                .ToListAsync(ct);

            var allPerms = await _db.Set<Permission>()
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.DisplayName)
                .Select(p => new PermissionListItemDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    IsSystem = p.IsSystem,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(ct);

            var dto = new RolePermissionsEditDto
            {
                RoleId = role.Id,
                RowVersion = role.RowVersion,
                RoleDisplayName = role.DisplayName,
                PermissionIds = currentPermIds,
                AllPermissions = allPerms
            };
            return Result<RolePermissionsEditDto>.Ok(dto);
        }
    }
}
