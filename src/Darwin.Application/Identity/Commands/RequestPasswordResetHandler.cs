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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Generates a one-time password reset token and sends it via email.
    /// Token is stored in UserToken (Purpose="PasswordReset") with expiration.
    /// </summary>
    public sealed class RequestPasswordResetHandler
    {
        private readonly IAppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IClock _clock;
        private readonly IValidator<RequestPasswordResetDto> _validator;

        public RequestPasswordResetHandler(IAppDbContext db, IEmailSender email, IClock clock, IValidator<RequestPasswordResetDto> validator)
        {
            _db = db; _email = email; _clock = clock; _validator = validator;
        }

        public async Task<Result> HandleAsync(RequestPasswordResetDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);
            if (user == null)
                return Result.Ok(); // Do not reveal user existence.

            // Invalidate old unused tokens
            var old = await _db.Set<UserToken>()
                .Where(t => t.UserId == user.Id && t.Purpose == "PasswordReset" && t.UsedAtUtc == null)
                .ToListAsync(ct);
            if (old.Count > 0)
            {
                foreach (var t in old) t.UsedAtUtc = _clock.UtcNow; // mark used
            }

            var token = RandomTokenGenerator.UrlSafeToken(32);
            var expires = _clock.UtcNow.AddHours(2);

            _db.Set<UserToken>().Add(new UserToken(user.Id, "PasswordReset", token, expires));
            await _db.SaveChangesAsync(ct);

            // TODO: template rendering (subject/body) — move to Infrastructure templating later.
            var subject = "Reset your Darwin account password";
            var body = $"Use the following token to reset your password: <b>{token}</b><br/>" +
                       $"This token expires at {expires:u}.";

            await _email.SendAsync(user.Email, subject, body, ct);
            return Result.Ok();
        }
    }
}
