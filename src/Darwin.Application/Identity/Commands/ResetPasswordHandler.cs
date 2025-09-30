using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Resets the user's password if the provided token is valid, unused, and not expired.
    /// Also rotates the user security stamp to invalidate old sessions and refresh sign-ins.
    /// </summary>
    public sealed class ResetPasswordHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IClock _clock;
        private readonly IValidator<ResetPasswordDto> _validator;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        /// <param name="db">Application DbContext abstraction.</param>
        /// <param name="hasher">Password hashing service.</param>
        /// <param name="stamps">Security stamp service (rotate on sensitive changes).</param>
        /// <param name="clock">Time provider used for expiry checks and usage marking.</param>
        /// <param name="validator">FluentValidation validator for <see cref="ResetPasswordDto"/>.</param>
        public ResetPasswordHandler(
            IAppDbContext db,
            IUserPasswordHasher hasher,
            ISecurityStampService stamps,
            IClock clock,
            IValidator<ResetPasswordDto> validator)
        {
            _db = db;
            _hasher = hasher;
            _stamps = stamps;
            _clock = clock;
            _validator = validator;
        }

        /// <summary>
        /// Validates the password reset token and, if valid, updates the user's password hash and security stamp.
        /// </summary>
        /// <param name="dto">The reset DTO including email, token, and the new password.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="Result.Ok"/> on success; <see cref="Result.Fail(string)"/> when token is invalid/expired or user not found.
        /// </returns>
        public async Task<Result> HandleAsync(ResetPasswordDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);

            if (user == null)
                return Result.Fail("Invalid token or email.");

            var token = await _db.Set<UserToken>()
                .Where(t => t.UserId == user.Id && t.Purpose == "PasswordReset" && t.Value == dto.Token)
                .FirstOrDefaultAsync(ct);

            var now = _clock.UtcNow;
            if (token == null || token.UsedAtUtc != null || (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value < now))
                return Result.Fail("Invalid or expired token.");

            user.PasswordHash = _hasher.Hash(dto.NewPassword);
            user.SecurityStamp = _stamps.NewStamp();
            token.MarkUsed(now);

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
