using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Updates editable fields of a permission (DisplayName and Description).
    /// Does not allow changing Key or IsSystem.
    /// </summary>
    public sealed class UpdatePermissionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PermissionEditDto> _validator;

        public UpdatePermissionHandler(IAppDbContext db, IValidator<PermissionEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Handles the update operation.
        /// </summary>
        /// <param name="dto">Edit DTO containing new values and concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(PermissionEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var permission = await _db.Set<Permission>().FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted, ct);
            if (permission is null)
                return Result.Fail("Permission not found.");

            // Concurrency check
            if (permission.RowVersion is not null && dto.RowVersion is not null && permission.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(permission.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            // Since DisplayName and Description have private setters, update via EF's entry
            var type = typeof(Permission);
            var displayNameProp = type.GetProperty(nameof(Permission.DisplayName), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            displayNameProp!.SetValue(permission, dto.DisplayName?.Trim() ?? string.Empty);
            var descriptionProp = type.GetProperty(nameof(Permission.Description), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            descriptionProp!.SetValue(permission, dto.Description);

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
