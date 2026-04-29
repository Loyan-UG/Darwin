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
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of the handler.
        /// </summary>
        public UpdateCurrentUserHandler(IAppDbContext db, ICurrentUserService currentUser, IValidator<UserProfileEditDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
            _localizer = localizer;
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

            var userId = _currentUser.GetCurrentUserId();
            if (userId == Guid.Empty || userId != dto.Id)
                return Result.Fail(_localizer["Unauthorized"]);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted && u.IsActive, ct);
            if (user is null)
                return Result.Fail(_localizer["UserNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = user.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["ConcurrencyConflict"]);

            // Update profile fields
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Locale = dto.Locale;
            user.Timezone = dto.Timezone;
            user.Currency = dto.Currency;
            var phoneChanged = !string.Equals(user.PhoneE164, dto.PhoneE164, System.StringComparison.Ordinal);
            user.PhoneE164 = dto.PhoneE164;
            if (phoneChanged)
            {
                user.PhoneNumberConfirmed = false;
            }

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
