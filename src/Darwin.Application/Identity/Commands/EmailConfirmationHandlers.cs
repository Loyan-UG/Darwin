using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Communication;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Shared.Results;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;
        private readonly ILogger<RequestEmailConfirmationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RequestEmailConfirmationHandler"/>.
        /// </summary>
        public RequestEmailConfirmationHandler(
            IAppDbContext db,
            IEmailSender email,
            IClock clock,
            IValidator<RequestEmailConfirmationDto> validator,
            IStringLocalizer<CommunicationResource> communicationLocalizer,
            ILogger<RequestEmailConfirmationHandler> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _communicationLocalizer = communicationLocalizer ?? throw new ArgumentNullException(nameof(communicationLocalizer));
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
            var tokenEntity = new UserToken(user.Id, EmailConfirmationPurpose, tokenValue, expiresAtUtc);
            _db.Set<UserToken>().Add(tokenEntity);
            await _db.SaveChangesAsync(ct);

            var siteSettings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);
            var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(user.Locale, siteSettings?.DefaultCulture);
            var subjectTemplate = CommunicationTemplateDefaults.ResolveTemplate(
                _communicationLocalizer,
                communicationCulture,
                siteSettings?.AccountActivationEmailSubjectTemplate,
                CommunicationTemplateDefaults.LegacyAccountActivationSubjectTemplate,
                "AccountActivationSubjectTemplateDefault");
            var bodyTemplate = CommunicationTemplateDefaults.ResolveTemplate(
                _communicationLocalizer,
                communicationCulture,
                siteSettings?.AccountActivationEmailBodyTemplate,
                CommunicationTemplateDefaults.LegacyAccountActivationBodyTemplate,
                "AccountActivationBodyTemplateDefault");
            var subjectParameters = new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["expires_at_utc"] = expiresAtUtc.ToString("u")
            };
            var bodyParameters = new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["token"] = tokenValue,
                ["expires_at_utc"] = expiresAtUtc.ToString("u")
            };
            var subject = ApplySubjectPrefix(
                siteSettings?.TransactionalEmailSubjectPrefix,
                TransactionalEmailTemplateRenderer.Render(
                    subjectTemplate,
                    subjectTemplate,
                    subjectParameters));
            var body = TransactionalEmailTemplateRenderer.Render(
                bodyTemplate,
                bodyTemplate,
                bodyParameters);
            var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? user.Email : siteSettings.CommunicationTestInboxEmail!;
            body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, user.Email, recipient, body);

            await _email.SendAsync(
                recipient,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "AccountActivation",
                    TemplateKey = "AccountActivationEmail",
                    CorrelationKey = tokenEntity.Id.ToString("N"),
                    IntendedRecipientEmail = user.Email,
                    TemplateParameters = bodyParameters
                });
            _logger.LogInformation("Email confirmation token issued for {Email}.", MaskEmail(user.Email));
            return Result.Ok();
        }

        private static string ApplySubjectPrefix(string? prefix, string subject)
        {
            return string.IsNullOrWhiteSpace(prefix) ? subject : $"{prefix.Trim()} {subject}";
        }

        private static string ApplyRecipientOverrideNotice(IStringLocalizer<CommunicationResource> localizer, string? culture, string originalRecipient, string effectiveRecipient, string body)
        {
            if (string.Equals(originalRecipient, effectiveRecipient, StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }

            var noticeTemplate = CommunicationTemplateDefaults.ResolveText(localizer, culture, "RecipientOverrideNoticeHtml");
            var notice = TransactionalEmailTemplateRenderer.Render(
                noticeTemplate,
                noticeTemplate,
                new Dictionary<string, string?>
                {
                    ["original_recipient"] = originalRecipient
                });
            return $"{notice}{body}";
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfirmEmailHandler"/>.
        /// </summary>
        public ConfirmEmailHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<ConfirmEmailDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
                return Result.Fail(_localizer["InvalidOrExpiredConfirmationToken"]);
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
                return Result.Fail(_localizer["InvalidOrExpiredConfirmationToken"]);
            }

            if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value < utcNow)
            {
                return Result.Fail(_localizer["InvalidOrExpiredConfirmationToken"]);
            }

            user.EmailConfirmed = true;
            token.MarkUsed(utcNow);
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
