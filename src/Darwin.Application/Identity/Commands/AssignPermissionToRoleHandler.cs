using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>Assigns a permission to a role (idempotent).</summary>
    public sealed class AssignPermissionToRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        public AssignPermissionToRoleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
        {
            var roleExists = await _db.Set<Role>().AnyAsync(x => x.Id == roleId && !x.IsDeleted, ct);
            if (!roleExists) return Result.Fail(_localizer["RoleNotFound"]);

            var permExists = await _db.Set<Permission>().AnyAsync(x => x.Id == permissionId && !x.IsDeleted, ct);
            if (!permExists) return Result.Fail(_localizer["PermissionNotFound"]);

            var already = await _db.Set<RolePermission>().AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId && !x.IsDeleted, ct);
            if (!already)
            {
                _db.Set<RolePermission>().Add(new RolePermission(roleId, permissionId));
                await _db.SaveChangesAsync(ct);
            }
            return Result.Ok();
        }
    }
}
