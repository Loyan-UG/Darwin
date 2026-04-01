using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Reissues an existing business invitation with a fresh token and expiration window.
    /// </summary>
    public sealed class ResendBusinessInvitationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IClock _clock;
        private readonly IBusinessInvitationLinkBuilder _businessInvitationLinkBuilder;
        private readonly IValidator<BusinessInvitationResendDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ResendBusinessInvitationHandler(
            IAppDbContext db,
            IEmailSender emailSender,
            IClock clock,
            IBusinessInvitationLinkBuilder businessInvitationLinkBuilder,
            IValidator<BusinessInvitationResendDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _businessInvitationLinkBuilder = businessInvitationLinkBuilder ?? throw new ArgumentNullException(nameof(businessInvitationLinkBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessInvitationResendDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (invitation is null)
                throw new InvalidOperationException(_localizer["InvitationNotFound"]);

            if (invitation.Status == BusinessInvitationStatus.Accepted)
                throw new InvalidOperationException(_localizer["AcceptedInvitationsCannotBeResent"]);

            if (invitation.Status == BusinessInvitationStatus.Revoked)
                throw new InvalidOperationException(_localizer["RevokedInvitationsCannotBeResent"]);

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId, ct);
            if (business is null)
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);

            invitation.Token = RandomTokenGenerator.UrlSafeToken(32);
            invitation.ExpiresAtUtc = _clock.UtcNow.AddDays(dto.ExpiresInDays);
            invitation.Status = BusinessInvitationStatus.Pending;
            invitation.AcceptedAtUtc = null;
            invitation.AcceptedByUserId = null;
            invitation.RevokedAtUtc = null;

            await _db.SaveChangesAsync(ct);

            var acceptanceLink = _businessInvitationLinkBuilder.BuildAcceptanceLink(invitation.Token);
            var siteSettings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
            var subject = ApplySubjectPrefix(
                siteSettings?.TransactionalEmailSubjectPrefix,
                TransactionalEmailTemplateRenderer.Render(
                    siteSettings?.BusinessInvitationEmailSubjectTemplate,
                    $"Invitation to join {business.Name} on Darwin",
                    new Dictionary<string, string?>
                    {
                        ["business_name"] = business.Name,
                        ["role"] = invitation.Role.ToString(),
                        ["invitation_action"] = "reissued"
                    }));
            var body = TransactionalEmailTemplateRenderer.Render(
                siteSettings?.BusinessInvitationEmailBodyTemplate,
                $"<p>Hello,</p>" +
                $"<p>Your invitation to join <strong>{business.Name}</strong> on Darwin as <strong>{invitation.Role}</strong> has been reissued.</p>" +
                (string.IsNullOrWhiteSpace(acceptanceLink)
                    ? string.Empty
                    : $"<p><a href=\"{acceptanceLink}\">Open your invitation</a></p>") +
                $"<p>Your new invitation token is:</p>" +
                $"<p><code>{invitation.Token}</code></p>" +
                $"<p>This invitation expires at <strong>{invitation.ExpiresAtUtc:u}</strong>.</p>",
                new Dictionary<string, string?>
                {
                    ["business_name"] = business.Name,
                    ["role"] = invitation.Role.ToString(),
                    ["token"] = invitation.Token,
                    ["expires_at_utc"] = invitation.ExpiresAtUtc.ToString("u"),
                    ["acceptance_link"] = acceptanceLink,
                    ["acceptance_link_html"] = string.IsNullOrWhiteSpace(acceptanceLink)
                        ? string.Empty
                        : $"<p><a href=\"{acceptanceLink}\">Open your invitation</a></p>",
                    ["invitation_action"] = "reissued",
                    ["invitation_intro_html"] = $"Your invitation to join <strong>{business.Name}</strong> on Darwin as <strong>{invitation.Role}</strong> has been reissued."
                });
            var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? invitation.Email : siteSettings.CommunicationTestInboxEmail!;
            body = ApplyRecipientOverrideNotice(invitation.Email, recipient, body);

            await _emailSender.SendAsync(
                recipient,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "BusinessInvitation",
                    BusinessId = invitation.BusinessId
                });
        }

        private static string ApplySubjectPrefix(string? prefix, string subject)
        {
            return string.IsNullOrWhiteSpace(prefix) ? subject : $"{prefix.Trim()} {subject}";
        }

        private static string ApplyRecipientOverrideNotice(string originalRecipient, string effectiveRecipient, string body)
        {
            if (string.Equals(originalRecipient, effectiveRecipient, StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }

            return $"<p><strong>Original recipient:</strong> {originalRecipient}</p>{body}";
        }
    }
}
