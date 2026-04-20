using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections;


namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Updates editable fields of an existing user for the admin panel.
    /// Does not permit changing email, username, or system flags.
    /// Uses the RowVersion for optimistic concurrency.
    /// </summary>
    public sealed class UpdateUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UserEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        public UpdateUserHandler(
            IAppDbContext db,
            IValidator<UserEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        /// <summary>
        /// Handles the update operation for an existing user.
        /// </summary>
        /// <param name="dto">The edit request with updated values and concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(UserEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail(_localizer["UserNotFound"]);

            // Concurrency check
            if (user.RowVersion is not null && dto.RowVersion is not null && user.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(user.RowVersion, dto.RowVersion))
                    return Result.Fail(_localizer["ConcurrencyConflict"]);
            }

            // Update allowed fields
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Locale = dto.Locale;
            user.Timezone = dto.Timezone;
            user.Currency = dto.Currency;
            user.PhoneE164 = dto.PhoneE164;
            user.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
