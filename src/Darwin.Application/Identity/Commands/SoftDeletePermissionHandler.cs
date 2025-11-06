using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Performs a soft delete of a permission. System permissions or those assigned to roles cannot be deleted.
    /// </summary>
    public sealed class SoftDeletePermissionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PermissionDeleteDto> _validator;

        public SoftDeletePermissionHandler(IAppDbContext db, IValidator<PermissionDeleteDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Marks a permission as deleted when possible.
        /// </summary>
        /// <param name="dto">Delete DTO with id and row version.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(PermissionDeleteDto dto, CancellationToken ct = default)
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

            if (permission.IsSystem)
                return Result.Fail("System permissions cannot be deleted.");

            // Check role assignments
            var assigned = await _db.Set<RolePermission>()
                .AnyAsync(rp => rp.PermissionId == permission.Id && !rp.IsDeleted, ct);

            if (assigned)
                return Result.Fail("Permission is assigned to one or more roles and cannot be deleted.");

            permission.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
