using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Performs a soft delete of a user. Records flagged as system cannot be deleted.
    /// </summary>
    public sealed class SoftDeleteUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UserDeleteDto> _validator;

        /// <summary>Creates a new handler instance.</summary>
        public SoftDeleteUserHandler(IAppDbContext db, IValidator<UserDeleteDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Marks the specified user as deleted. If the user is marked as system, the operation fails.
        /// </summary>
        /// <param name="dto">The delete request containing id and concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(UserDeleteDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail("User not found.");

            // Concurrency check
            if (user.RowVersion is not null && dto.RowVersion is not null && user.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(user.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            if (user.IsSystem)
                return Result.Fail("System users cannot be deleted.");

            // TODO: If orders or other aggregates reference this user, prevent deletion and only deactivate.
            user.IsDeleted = true;
            user.IsActive = false;

            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
