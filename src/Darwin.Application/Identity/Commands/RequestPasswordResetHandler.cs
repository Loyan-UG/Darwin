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
using Darwin.Application.Identity.Validators;
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
    /// Issues a one-time password reset token and sends it to the user via email.
    /// The token is stored in <see cref="UserToken"/> with <c>Purpose = "PasswordReset"</c>,
    /// has an expiration timestamp, and can be redeemed once by <see cref="ResetPasswordHandler"/>.
    /// </summary>
    public sealed class RequestPasswordResetHandler
    {
        private const string PasswordResetPurpose = "PasswordReset";

        private readonly IAppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IClock _clock;
        private readonly IValidator<RequestPasswordResetDto> _validator;
        private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;
        private readonly ILogger<RequestPasswordResetHandler> _logger;

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
            IValidator<RequestPasswordResetDto> validator,
            IStringLocalizer<CommunicationResource> communicationLocalizer,
            ILogger<RequestPasswordResetHandler> logger)
        {
            _db = db;
            _email = email;
            _clock = clock;
            _validator = validator;
            _communicationLocalizer = communicationLocalizer ?? throw new ArgumentNullException(nameof(communicationLocalizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);

            // Never reveal whether the email exists to the caller.
            if (user == null)
                return Result.Ok();

            var nowUtc = _clock.UtcNow;

            // Invalidate prior active tokens and clear out expired reset tokens for this purpose.
            var old = await _db.Set<UserToken>()
                .Where(t =>
                    t.UserId == user.Id &&
                    t.Purpose == PasswordResetPurpose &&
                    (t.UsedAtUtc == null || (t.ExpiresAtUtc != null && t.ExpiresAtUtc <= nowUtc)))
                .ToListAsync(ct);

            if (old.Count > 0)
            {
                foreach (var t in old)
                    t.UsedAtUtc = nowUtc;
            }

            // Generate an opaque, URL-safe token; persist with expiry.
            var token = RandomTokenGenerator.UrlSafeToken(32);
            var expires = nowUtc.AddHours(2);

            var tokenEntity = new UserToken(user.Id, PasswordResetPurpose, token, expires);
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
                siteSettings?.PasswordResetEmailSubjectTemplate,
                CommunicationTemplateDefaults.LegacyPasswordResetSubjectTemplate,
                "PasswordResetSubjectTemplateDefault");
            var bodyTemplate = CommunicationTemplateDefaults.ResolveTemplate(
                _communicationLocalizer,
                communicationCulture,
                siteSettings?.PasswordResetEmailBodyTemplate,
                CommunicationTemplateDefaults.LegacyPasswordResetBodyTemplate,
                "PasswordResetBodyTemplateDefault");
            var subjectParameters = new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["expires_at_utc"] = expires.ToString("u")
            };
            var bodyParameters = new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["token"] = token,
                ["expires_at_utc"] = expires.ToString("u")
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


            var maskedEmail = MaskEmail(user.Email);
            _logger.LogInformation(
                "Password reset token created for {Email}. Token expires at {ExpiresAtUtc}.",
                maskedEmail,
                expires);

            try
            {
                await _email.SendAsync(
                    recipient,
                    subject,
                    body,
                    ct,
                    new EmailDispatchContext
                    {
                        FlowKey = "PasswordReset",
                        TemplateKey = "PasswordResetEmail",
                        CorrelationKey = tokenEntity.Id.ToString("N"),
                        IntendedRecipientEmail = user.Email,
                        TemplateParameters = bodyParameters
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Password reset email send failed for {Email}. Check Email:Smtp configuration and SMTP relay connectivity.",
                    maskedEmail);
                throw;
            }

            _logger.LogInformation("Password reset email sent successfully to {Email}.", maskedEmail);
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

        /// <summary>
        /// Masks an email for operational logs to reduce sensitive-data exposure.
        /// </summary>
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
}
