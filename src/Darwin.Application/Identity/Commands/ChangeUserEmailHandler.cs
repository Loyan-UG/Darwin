using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Changes the email address of a user and rotates the security stamp. This operation is intended for administrators.
    /// </summary>
    public sealed class ChangeUserEmailHandler
    {
        private readonly IAppDbContext _db;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserChangeEmailDto> _validator;

        /// <summary>Creates a new instance of the handler.</summary>
        public ChangeUserEmailHandler(IAppDbContext db, ISecurityStampService stamps, IValidator<UserChangeEmailDto> validator)
        {
            _db = db;
            _stamps = stamps;
            _validator = validator;
        }

        /// <summary>
        /// Updates the user's email address. Ensures uniqueness and resets normalized fields.
        /// Resets the security stamp and clears the email confirmation flag.
        /// </summary>
        /// <param name="dto">Change email request with new email.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> HandleAsync(UserChangeEmailDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail("User not found.");

            var normalized = dto.NewEmail.Trim().ToUpperInvariant();
            var exists = await _db.Set<User>().AnyAsync(u => u.NormalizedEmail == normalized && u.Id != dto.Id && !u.IsDeleted, ct);
            if (exists)
                return Result.Fail("Email already in use.");

            // Update email and normalized values
            var trimmed = dto.NewEmail.Trim();
            user.Email = trimmed;
            user.NormalizedEmail = normalized;
            // Set username equal to email for simplicity
            user.UserName = trimmed;
            user.NormalizedUserName = normalized;
            // Reset email confirmation
            user.EmailConfirmed = false;
            // Rotate security stamp so all sessions are invalidated
            user.SecurityStamp = _stamps.NewStamp();

            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
