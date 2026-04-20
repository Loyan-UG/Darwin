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
    /// <summary>Assigns an existing role to a user (idempotent).</summary>
    public sealed class AssignRoleToUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        public AssignRoleToUserHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync
            (Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var userExists = await _db.Set<User>().
                AnyAsync(x => x.Id == userId && !x.IsDeleted, ct);

            if (!userExists) return Result.Fail(_localizer["UserNotFound"]);

            var roleExists = await _db.Set<Role>().
                AnyAsync(x => x.Id == roleId && !x.IsDeleted, ct);

            if (!roleExists) return Result.Fail(_localizer["RoleNotFound"]);

            var exists = await _db.Set<UserRole>().
                AnyAsync(x => x.UserId == userId && x.RoleId == roleId && !x.IsDeleted, ct);

            if (!exists)
            {
                // Use Domain ctor to avoid inaccessible setters
                _db.Set<UserRole>().Add(new UserRole(userId, roleId));
                await _db.SaveChangesAsync(ct);
            }
            return Result.Ok();
        }
    }
}
