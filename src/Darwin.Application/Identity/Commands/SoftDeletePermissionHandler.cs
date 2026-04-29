using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Performs a soft delete of a permission. System permissions or those assigned to roles cannot be deleted.
    /// </summary>
    public sealed class SoftDeletePermissionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PermissionDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeletePermissionHandler(
            IAppDbContext db,
            IValidator<PermissionDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(PermissionDeleteDto dto, CancellationToken ct = default)
        {
            var validation = await _validator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return Result.Fail(dto.RowVersion is null || dto.RowVersion.Length == 0
                    ? _localizer["RowVersionRequired"]
                    : _localizer["InvalidDeleteRequest"]);

            var permission = await _db.Set<Permission>().FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted, ct);
            if (permission is null)
                return Result.Fail(_localizer["PermissionNotFound"]);

            var currentVersion = permission.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail(_localizer["ConcurrencyConflict"]);

            if (permission.IsSystem)
                return Result.Fail(_localizer["SystemPermissionsCannotBeDeleted"]);

            var assigned = await _db.Set<RolePermission>()
                .AnyAsync(rp => rp.PermissionId == permission.Id && !rp.IsDeleted, ct);

            if (assigned)
                return Result.Fail(_localizer["PermissionAssignedToRolesCannotDelete"]);

            permission.IsDeleted = true;
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
