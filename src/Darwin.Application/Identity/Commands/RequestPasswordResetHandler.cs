using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Issues a one-time password reset token and sends it to the user via email.
    /// The token is stored in <see cref="UserToken"/> with <c>Purpose = "PasswordReset"</c>,
    /// has an expiration timestamp, and can be redeemed once by <see cref="ResetPasswordHandler"/>.
    /// </summary>
    public sealed class RequestPasswordResetHandler
    {
        private readonly IAppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IClock _clock;
        private readonly IValidator<RequestPasswordResetDto> _validator;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        /// <param name="db">Application DbContext abstraction used to query and persist entities.</param>
        /// <param name="email">Email sender abstraction used to deliver password reset links.</param>
        /// <param name="clock">Time provider used to compute expiration and mark usage time.</param>
        /// <param name="validator">FluentValidation validator for the <see cref="RequestPasswordResetDto"/>.</param>
        public RequestPasswordResetHandler(
            IAppDbContext db,
            IEmailSender email,
            IClock clock,
            IValidator<RequestPasswordResetDto> validator)
        {
            _db = db;
            _email = email;
            _clock = clock;
            _validator = validator;
        }

        /// <summary>
        /// Generates a single-use, time-limited token for password reset and emails it to the user.
        /// If the user does not exist, returns <see cref="Result.Ok"/> to avoid user enumeration.
        /// </summary>
        /// <param name="dto">The request DTO containing the email address to reset.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="Result.Ok"/> always, even when the user does not exist; otherwise stores the token and emails it.
        /// </returns>
        public async Task<Result> HandleAsync(RequestPasswordResetDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);

            // Never reveal whether the email exists to the caller.
            if (user == null)
                return Result.Ok();

            // Invalidate any previously issued (but unused) tokens.
            var old = await _db.Set<UserToken>()
                .Where(t => t.UserId == user.Id && t.Purpose == "PasswordReset" && t.UsedAtUtc == null)
                .ToListAsync(ct);

            if (old.Count > 0)
            {
                foreach (var t in old)
                    t.UsedAtUtc = _clock.UtcNow;
            }

            // Generate an opaque, URL-safe token; persist with expiry.
            var token = RandomTokenGenerator.UrlSafeToken(32);
            var expires = _clock.UtcNow.AddHours(2);

            _db.Set<UserToken>().Add(new UserToken(user.Id, "PasswordReset", token, expires));
            await _db.SaveChangesAsync(ct);

            // NOTE: For production, switch this to a templating engine and a branded reset URL.
            var subject = "Reset your Darwin account password";
            var body = $"Use the following token to reset your password: <b>{token}</b><br/>" +
                       $"This token expires at {expires:u}.";

            await _email.SendAsync(user.Email, subject, body, ct);
            return Result.Ok();
        }
    }
}
