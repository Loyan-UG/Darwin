using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Issues a fresh email-confirmation token and delivers it to the account email address.
    /// </summary>
    public sealed class RequestEmailConfirmationHandler
    {
        private const string EmailConfirmationPurpose = "EmailConfirmation";

        private readonly IAppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IClock _clock;
        private readonly IValidator<RequestEmailConfirmationDto> _validator;
        private readonly ILogger<RequestEmailConfirmationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RequestEmailConfirmationHandler"/>.
        /// </summary>
        public RequestEmailConfirmationHandler(
            IAppDbContext db,
            IEmailSender email,
            IClock clock,
            IValidator<RequestEmailConfirmationDto> validator,
            ILogger<RequestEmailConfirmationHandler> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a new confirmation token for an unconfirmed user and sends the activation email.
        /// Missing users return success to avoid user enumeration.
        /// </summary>
        public async Task<Result> HandleAsync(RequestEmailConfirmationDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && !x.IsDeleted, ct);

            if (user is null || user.EmailConfirmed)
            {
                return Result.Ok();
            }

            var utcNow = _clock.UtcNow;
            var expiresAtUtc = utcNow.AddHours(24);

            var activeTokens = await _db.Set<UserToken>()
                .Where(x => x.UserId == user.Id &&
                            x.Purpose == EmailConfirmationPurpose &&
                            x.UsedAtUtc == null)
                .ToListAsync(ct);

            foreach (var activeToken in activeTokens)
            {
                activeToken.MarkUsed(utcNow);
            }

            var tokenValue = RandomTokenGenerator.UrlSafeToken(32);
            _db.Set<UserToken>().Add(new UserToken(user.Id, EmailConfirmationPurpose, tokenValue, expiresAtUtc));
            await _db.SaveChangesAsync(ct);

            var subject = "Confirm your Darwin account email";
            var body = $"Use the following token to confirm your email address: <b>{tokenValue}</b><br/>" +
                       $"This token expires at {expiresAtUtc:u}.";

            await _email.SendAsync(
                user.Email,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "AccountActivation"
                });
            _logger.LogInformation("Email confirmation token issued for {Email}.", MaskEmail(user.Email));
            return Result.Ok();
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "(empty)";
            }

            var at = email.IndexOf('@');
            if (at <= 1)
            {
                return "***";
            }

            var prefix = email.Substring(0, Math.Min(2, at));
            return $"{prefix}***{email.Substring(at)}";
        }
    }

    /// <summary>
    /// Confirms a user's email address using a one-time token.
    /// </summary>
    public sealed class ConfirmEmailHandler
    {
        private const string EmailConfirmationPurpose = "EmailConfirmation";

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<ConfirmEmailDto> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfirmEmailHandler"/>.
        /// </summary>
        public ConfirmEmailHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<ConfirmEmailDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Confirms the target email address when the supplied token is valid and unexpired.
        /// </summary>
        public async Task<Result> HandleAsync(ConfirmEmailDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && !x.IsDeleted, ct);

            if (user is null)
            {
                return Result.Fail("Invalid or expired confirmation token.");
            }

            var utcNow = _clock.UtcNow;
            var token = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(x => x.UserId == user.Id &&
                                          x.Purpose == EmailConfirmationPurpose &&
                                          x.Value == dto.Token &&
                                          x.UsedAtUtc == null,
                    ct);

            if (token is null)
            {
                return Result.Fail("Invalid or expired confirmation token.");
            }

            if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value < utcNow)
            {
                return Result.Fail("Invalid or expired confirmation token.");
            }

            user.EmailConfirmed = true;
            token.MarkUsed(utcNow);
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
