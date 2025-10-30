using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Replaces a user's assigned roles with the supplied set.
    /// Performs soft-add (insert links) and soft-remove (mark links deleted) semantics.
    /// </summary>
    public sealed class UpdateUserRolesHandler
    {
        private readonly IAppDbContext _db;
        private readonly UserRolesUpdateValidator _validator = new();

        public UpdateUserRolesHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Updates user-role link set after validating inputs, user existence,
        /// RowVersion, and role id validity.
        /// </summary>
        public async Task<Result> HandleAsync(UserRolesUpdateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) return Result.Fail("Invalid user-roles payload.");

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted && u.IsActive, ct);
            if (user is null) return Result.Fail("User not found or inactive.");

            // App-level optimistic concurrency check
            if (!user.RowVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail("Concurrency conflict. Please reload and try again.");

            // Validate roles exist (non-deleted)
            var distinctRoleIds = dto.RoleIds.Distinct().ToList();
            var validRoleIds = await _db.Set<Role>()
                .AsNoTracking()
                .Where(r => distinctRoleIds.Contains(r.Id) && !r.IsDeleted)
                .Select(r => r.Id)
                .ToListAsync(ct);

            if (validRoleIds.Count != distinctRoleIds.Count)
                return Result.Fail("One or more roles are invalid.");

            // Current links (tracked to allow soft-delete)
            var links = await _db.Set<UserRole>()
                .Where(ur => ur.UserId == dto.UserId)
                .ToListAsync(ct);

            var currentActive = links.Where(l => !l.IsDeleted).Select(l => l.RoleId).ToHashSet();
            var target = validRoleIds.ToHashSet();

            var toAdd = target.Except(currentActive).ToList();
            var toRemove = currentActive.Except(target).ToList();

            // Add new links using domain constructor (properties have private setters)
            foreach (var rid in toAdd)
            {
                _db.Set<UserRole>().Add(new UserRole(dto.UserId, rid));
            }

            // Soft-remove links
            foreach (var link in links.Where(l => toRemove.Contains(l.RoleId) && !l.IsDeleted))
                link.IsDeleted = true;

            // Touch user's ModifiedAtUtc for audit trail
            user.ModifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
