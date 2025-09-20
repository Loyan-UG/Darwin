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
            var userExists = await _db.Set<User>().AnyAsync(x => x.Id == userId && !x.IsDeleted, ct);
            if (!userExists) return Result.Fail("User not found.");

            var roleExists = await _db.Set<Role>().AnyAsync(x => x.Id == roleId && !x.IsDeleted, ct);
            if (!roleExists) return Result.Fail("Role not found.");

            var already = await _db.Set<UserRole>().AnyAsync(x => x.UserId == userId && x.RoleId == roleId && !x.IsDeleted, ct);
            if (!already)
            {
                _db.Set<UserRole>().Add(new UserRole(userId, roleId));
                await _db.SaveChangesAsync(ct);
            }
            return Result.Ok();
        }
    }
}
