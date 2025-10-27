using System;
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
    /// Sets a new password for a user without requiring the current password.
    /// This command is intended for Admin area flows (e.g., helpdesk reset).
    /// The handler hashes the new password and rotates the user's security stamp
    /// to invalidate existing sessions and refresh authentication cookies.
    /// </summary>
    public sealed class SetUserPasswordByAdminHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserAdminSetPasswordDto> _validator;

        /// <summary>
        /// Initializes the handler with persistence, hashing and security stamp services.
        /// </summary>
        public SetUserPasswordByAdminHandler(
            IAppDbContext db,
            IUserPasswordHasher hasher,
            ISecurityStampService stamps,
            IValidator<UserAdminSetPasswordDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Performs the admin password reset:
        /// - Validates input
        /// - Loads the user (must not be soft-deleted)
        /// - Hashes the new password and updates the user
        /// - Rotates the security stamp to invalidate active sessions
        /// </summary>
        /// <param name="dto">Admin reset request containing user id and the new password.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result> HandleAsync(UserAdminSetPasswordDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail("User not found.");

            // Hash and set the new password.
            user.PasswordHash = _hasher.Hash(dto.NewPassword);

            // Rotate security stamp to force sign-in refresh and invalidate existing cookies.
            user.SecurityStamp = _stamps.NewStamp();

            // NOTE: This command intentionally does not require the current password.
            // It should only be invoked from Admin flows guarded by permissions/authorization in the Web layer.

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
