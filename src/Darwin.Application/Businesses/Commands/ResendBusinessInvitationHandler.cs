using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Communication;
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
        private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;

        public ResendBusinessInvitationHandler(
            IAppDbContext db,
            IEmailSender emailSender,
            IClock clock,
            IBusinessInvitationLinkBuilder businessInvitationLinkBuilder,
            IValidator<BusinessInvitationResendDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            IStringLocalizer<CommunicationResource> communicationLocalizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _businessInvitationLinkBuilder = businessInvitationLinkBuilder ?? throw new ArgumentNullException(nameof(businessInvitationLinkBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _communicationLocalizer = communicationLocalizer ?? throw new ArgumentNullException(nameof(communicationLocalizer));
        }

        public async Task HandleAsync(BusinessInvitationResendDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (invitation is null)
                throw new InvalidOperationException(_localizer["InvitationNotFound"]);

            if (invitation.Status == BusinessInvitationStatus.Accepted)
                throw new InvalidOperationException(_localizer["AcceptedInvitationsCannotBeResent"]);

            if (invitation.Status == BusinessInvitationStatus.Revoked)
                throw new InvalidOperationException(_localizer["RevokedInvitationsCannotBeResent"]);

            var currentVersion = invitation.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (requestVersion.Length == 0 || !currentVersion.SequenceEqual(requestVersion))
                throw new InvalidOperationException(_localizer["ItemConcurrencyConflict"]);

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId && !x.IsDeleted, ct);
            if (business is null)
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);

            invitation.Token = RandomTokenGenerator.UrlSafeToken(32);
            invitation.ExpiresAtUtc = _clock.UtcNow.AddDays(dto.ExpiresInDays);
            invitation.Status = BusinessInvitationStatus.Pending;
            invitation.AcceptedAtUtc = null;
            invitation.AcceptedByUserId = null;
            invitation.RevokedAtUtc = null;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(_localizer["ItemConcurrencyConflict"]);
            }

            var acceptanceLink = _businessInvitationLinkBuilder.BuildAcceptanceLink(invitation.Token);
            var siteSettings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);
            var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(business.DefaultCulture, siteSettings?.DefaultCulture);
            var subjectTemplate = CommunicationTemplateDefaults.ResolveTemplate(
                _communicationLocalizer,
                communicationCulture,
                siteSettings?.BusinessInvitationEmailSubjectTemplate,
                CommunicationTemplateDefaults.LegacyBusinessInvitationSubjectTemplate,
                "BusinessInvitationSubjectTemplateDefault");
            var bodyTemplate = CommunicationTemplateDefaults.ResolveTemplate(
                _communicationLocalizer,
                communicationCulture,
                siteSettings?.BusinessInvitationEmailBodyTemplate,
                CommunicationTemplateDefaults.LegacyBusinessInvitationBodyTemplate,
                "BusinessInvitationBodyTemplateDefault");
            var acceptanceLinkTemplate = CommunicationTemplateDefaults.ResolveText(_communicationLocalizer, communicationCulture, "BusinessInvitationAcceptanceLinkHtml");
            var reissuedIntroTemplate = CommunicationTemplateDefaults.ResolveText(_communicationLocalizer, communicationCulture, "BusinessInvitationIntroReissuedHtml");
            var subjectParameters = new Dictionary<string, string?>
            {
                ["business_name"] = business.Name,
                ["role"] = invitation.Role.ToString(),
                ["invitation_action"] = "reissued"
            };
            var bodyParameters = new Dictionary<string, string?>
            {
                ["business_name"] = business.Name,
                ["role"] = invitation.Role.ToString(),
                ["token"] = invitation.Token,
                ["expires_at_utc"] = invitation.ExpiresAtUtc.ToString("u"),
                ["acceptance_link"] = acceptanceLink,
                ["acceptance_link_html"] = string.IsNullOrWhiteSpace(acceptanceLink)
                    ? string.Empty
                    : TransactionalEmailTemplateRenderer.Render(acceptanceLinkTemplate, acceptanceLinkTemplate, new Dictionary<string, string?> { ["acceptance_link"] = acceptanceLink }),
                ["invitation_action"] = "reissued",
                ["invitation_intro_html"] = TransactionalEmailTemplateRenderer.Render(
                    reissuedIntroTemplate,
                    reissuedIntroTemplate,
                    subjectParameters)
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
            var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? invitation.Email : siteSettings.CommunicationTestInboxEmail!;
            body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, invitation.Email, recipient, body);

            await _emailSender.SendAsync(
                recipient,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "BusinessInvitation",
                    TemplateKey = "BusinessInvitationEmail",
                    CorrelationKey = invitation.Id.ToString("N"),
                    BusinessId = invitation.BusinessId,
                    IntendedRecipientEmail = invitation.Email,
                    TemplateParameters = bodyParameters
                });
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
    }
}
