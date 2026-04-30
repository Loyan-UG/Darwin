using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateUserRolesHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _clock = clock;
            _localizer = localizer;
        }

        /// <summary>
        /// Updates user-role link set after validating inputs, user existence,
        /// RowVersion, and role id validity.
        /// </summary>
        public async Task<Result> HandleAsync(UserRolesUpdateDto dto, CancellationToken ct = default)
        {
            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var v = _validator.Validate(dto);
            if (!v.IsValid) return Result.Fail(_localizer["InvalidUserRolesPayload"]);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted && u.IsActive, ct);
            if (user is null) return Result.Fail(_localizer["UserNotFoundOrInactive"]);

            // App-level optimistic concurrency check
            var currentRowVersion = user.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["ConcurrencyConflictReloadAndRetry"]);

            // Validate roles exist (non-deleted)
            var distinctRoleIds = dto.RoleIds.Distinct().ToList();
            var validRoleIds = await _db.Set<Role>()
                .AsNoTracking()
                .Where(r => distinctRoleIds.Contains(r.Id) && !r.IsDeleted)
                .Select(r => r.Id)
                .ToListAsync(ct);

            if (validRoleIds.Count != distinctRoleIds.Count)
                return Result.Fail(_localizer["InvalidRolesSelection"]);

            // Current links (tracked to allow soft-delete)
            var links = await _db.Set<UserRole>()
                .Where(ur => ur.UserId == dto.UserId)
                .ToListAsync(ct);

            var currentActive = links.Where(l => !l.IsDeleted).Select(l => l.RoleId).ToHashSet();
            var target = validRoleIds.ToHashSet();

            var toAdd = target.Except(currentActive).ToList();
            var toRemove = currentActive.Except(target).ToList();

            // Restore previously deleted links instead of creating duplicate join rows.
            foreach (var rid in toAdd)
            {
                var existingDeletedLink = links.FirstOrDefault(l => l.RoleId == rid && l.IsDeleted);
                if (existingDeletedLink is not null)
                {
                    existingDeletedLink.IsDeleted = false;
                }
                else
                {
                    _db.Set<UserRole>().Add(new UserRole(dto.UserId, rid));
                }
            }

            // Soft-remove links
            foreach (var link in links.Where(l => toRemove.Contains(l.RoleId) && !l.IsDeleted))
                link.IsDeleted = true;

            // Touch user's ModifiedAtUtc for audit trail
            user.ModifiedAtUtc = _clock.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ConcurrencyConflictReloadAndRetry"]);
            }

            return Result.Ok();
        }
    }
}
