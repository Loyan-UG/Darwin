using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Performs a soft delete of a user. Records flagged as system cannot be deleted.
    /// </summary>
    public sealed class SoftDeleteUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>Creates a new handler instance.</summary>
        public SoftDeleteUserHandler(
            IAppDbContext db,
            IJwtTokenService jwt,
            ISecurityStampService stamps,
            IValidator<UserDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
            _validator = validator;
            _localizer = localizer;
        }

        /// <summary>
        /// Marks the specified user as deleted. If the user is marked as system, the operation fails.
        /// </summary>
        /// <param name="dto">The delete request containing id and concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result<UserDeleteOutcomeDto>> HandleAsync(UserDeleteDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["UserNotFound"]);

            // Concurrency check
            if (user.RowVersion is not null && dto.RowVersion is not null && user.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(user.RowVersion, dto.RowVersion))
                    return Result<UserDeleteOutcomeDto>.Fail(_localizer["ConcurrencyConflict"]);
            }

            if (user.IsSystem)
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["SystemUsersCannotBeDeleted"]);

            var hasOrderHistory = await _db.Set<Order>()
                .AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id, ct)
                .ConfigureAwait(false);

            user.IsActive = false;
            user.SecurityStamp = _stamps.NewStamp();

            var outcome = new UserDeleteOutcomeDto();
            if (hasOrderHistory)
            {
                outcome.WasDeactivatedDueToReferences = true;
            }
            else
            {
                user.IsDeleted = true;
                outcome.WasSoftDeleted = true;
            }

            await _db.SaveChangesAsync(ct);
            await _jwt.RevokeAllForUserAsync(user.Id, ct);

            return Result<UserDeleteOutcomeDto>.Ok(outcome);
        }
    }
}
