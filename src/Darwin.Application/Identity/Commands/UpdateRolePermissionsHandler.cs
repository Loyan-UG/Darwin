using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Replaces a role's permission assignments with the supplied set.
    /// Performs soft-add (insert links) and soft-remove (mark links deleted) semantics.
    /// </summary>
    public sealed class UpdateRolePermissionsHandler
    {
        private readonly IAppDbContext _db;
        private readonly RolePermissionsUpdateValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateRolePermissionsHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        /// <summary>
        /// Updates the role-permission link set after validating inputs, role existence,
        /// RowVersion, and permission id validity.
        /// </summary>
        public async Task<Result> HandleAsync(RolePermissionsUpdateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) return Result.Fail(_localizer["InvalidRolePermissionsPayload"]);

            // Load role (tracked & non-deleted)
            var role = await _db.Set<Role>()
                .FirstOrDefaultAsync(r => r.Id == dto.RoleId && !r.IsDeleted, ct);
            if (role is null) return Result.Fail(_localizer["RoleNotFound"]);

            // App-level optimistic concurrency check (IAppDbContext has no Entry API)
            if (!role.RowVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail(_localizer["ConcurrencyConflictReloadAndRetry"]);

            // Validate permissions exist (non-deleted)
            var distinctIds = dto.PermissionIds.Distinct().ToList();
            var validIds = await _db.Set<Permission>()
                .AsNoTracking()
                .Where(p => distinctIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (validIds.Count != distinctIds.Count)
                return Result.Fail(_localizer["InvalidPermissionsSelection"]);

            // Current links (tracked to allow soft-delete)
            var currentLinks = await _db.Set<RolePermission>()
                .Where(rp => rp.RoleId == dto.RoleId)
                .ToListAsync(ct);

            var currentActive = currentLinks.Where(x => !x.IsDeleted).Select(x => x.PermissionId).ToHashSet();
            var target = validIds.ToHashSet();

            var toAdd = target.Except(currentActive).ToList();
            var toRemove = currentActive.Except(target).ToList();

            // Restore previously deleted links instead of creating duplicate join rows.
            foreach (var pid in toAdd)
            {
                var existingDeletedLink = currentLinks.FirstOrDefault(l => l.PermissionId == pid && l.IsDeleted);
                if (existingDeletedLink is not null)
                {
                    existingDeletedLink.IsDeleted = false;
                }
                else
                {
                    _db.Set<RolePermission>().Add(new RolePermission(dto.RoleId, pid));
                }
            }

            // Soft-remove links
            foreach (var link in currentLinks.Where(l => toRemove.Contains(l.PermissionId) && !l.IsDeleted))
                link.IsDeleted = true;

            // Touch role's ModifiedAtUtc for audit trail
            role.ModifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
