// File: src/Darwin.Application/Identity/Commands/UpdateCurrentUserHandler.cs
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
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
    /// Updates the profile information of the currently authenticated user.
    /// Only profile fields such as name, locale, timezone, currency, and phone are modifiable.
    /// Email and activation status are immutable in this context.
    /// </summary>
    public sealed class UpdateCurrentUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<UserProfileEditDto> _validator;

        /// <summary>
        /// Initializes a new instance of the handler.
        /// </summary>
        public UpdateCurrentUserHandler(IAppDbContext db, ICurrentUserService currentUser, IValidator<UserProfileEditDto> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        /// <summary>
        /// Handles the profile update for the current user.
        /// </summary>
        /// <param name="dto">The profile edit request containing updated fields.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(UserProfileEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var userId = _currentUser.GetCurrentUserId;
            if (userId == Guid.Empty || userId != dto.Id)
                return Result.Fail("Unauthorized.");

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail("User not found.");

            // Concurrency check
            if (user.RowVersion is not null && dto.RowVersion is not null && user.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(user.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            // Update profile fields
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Locale = dto.Locale;
            user.Timezone = dto.Timezone;
            user.Currency = dto.Currency;
            user.PhoneE164 = dto.PhoneE164;

            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
