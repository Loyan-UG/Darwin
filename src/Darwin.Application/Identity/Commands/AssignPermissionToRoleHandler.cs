using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>Grants a permission to a role (idempotent).</summary>
    public sealed class AssignPermissionToRoleHandler
    {
        private readonly IAppDbContext _db;
        public AssignPermissionToRoleHandler(IAppDbContext db) => _db = db;

        public async Task<Result> HandleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
        {
            var role = await _db.Set<Role>().FirstOrDefaultAsync(x => x.Id == roleId && !x.IsDeleted, ct);
            if (role == null) return Result.Fail("Role not found.");

            var perm = await _db.Set<Permission>().FirstOrDefaultAsync(x => x.Id == permissionId && !x.IsDeleted, ct);
            if (perm == null) return Result.Fail("Permission not found.");

            var exists = await _db.Set<RolePermission>().AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId && !x.IsDeleted, ct);
            if (!exists)
            {
                _db.Set<RolePermission>().Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
                await _db.SaveChangesAsync(ct);
            }
            return Result.Ok();
        }
    }
}
