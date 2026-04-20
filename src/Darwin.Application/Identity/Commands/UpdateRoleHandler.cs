using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Updates DisplayName and Description of an existing role.
    /// Key/Name remain immutable for referential stability.
    /// </summary>
    public sealed class UpdateRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RoleEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateRoleHandler(
            IAppDbContext db,
            IValidator<RoleEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(RoleEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var role = await _db.Set<Darwin.Domain.Entities.Identity.Role>()
                .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted, ct);

            if (role == null) return Result.Fail(_localizer["RoleNotFound"]);
            if (role.IsSystem) return Result.Fail(_localizer["SystemRolesCannotBeEdited"]);

            // optimistic concurrency (RowVersion)
            if (dto.RowVersion != null && role.RowVersion != null &&
                dto.RowVersion.Length > 0 && role.RowVersion.Length > 0 &&
                !StructuralComparisons.StructuralEqualityComparer.Equals(dto.RowVersion, role.RowVersion))
            {
                return Result.Fail(_localizer["ConcurrencyConflictReloadAndRetry"]);
            }

            // Requires Role.DisplayName in Domain.
            role.DisplayName = dto.DisplayName?.Trim() ?? string.Empty;
            role.Description = dto.Description;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
