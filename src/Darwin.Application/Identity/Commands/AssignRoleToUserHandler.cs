using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>Assigns an existing role to a user (idempotent).</summary>
    public sealed class AssignRoleToUserHandler
    {
        private readonly IAppDbContext _db;
        public AssignRoleToUserHandler(IAppDbContext db) => _db = db;

        public async Task<Result> HandleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
            if (user == null) return Result.Fail("User not found.");

            var role = await _db.Set<Role>().FirstOrDefaultAsync(x => x.Id == roleId && !x.IsDeleted, ct);
            if (role == null) return Result.Fail("Role not found.");

            var exists = await _db.Set<UserRole>().AnyAsync(x => x.UserId == userId && x.RoleId == roleId && !x.IsDeleted, ct);
            if (!exists)
            {
                _db.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId });
                await _db.SaveChangesAsync(ct);
            }

            return Result.Ok();
        }
    }
}
