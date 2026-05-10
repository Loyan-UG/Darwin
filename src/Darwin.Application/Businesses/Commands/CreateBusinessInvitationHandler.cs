using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Communication;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Issues a business invitation and sends the invitation token by email.
    /// </summary>
    public sealed class CreateBusinessInvitationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IClock _clock;
        private readonly ICurrentUserService _currentUser;
        private readonly IBusinessInvitationLinkBuilder _businessInvitationLinkBuilder;
        private readonly IValidator<BusinessInvitationCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;

        public CreateBusinessInvitationHandler(
            IAppDbContext db,
            IEmailSender emailSender,
            IClock clock,
            ICurrentUserService currentUser,
            IBusinessInvitationLinkBuilder businessInvitationLinkBuilder,
            IValidator<BusinessInvitationCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            IStringLocalizer<CommunicationResource> communicationLocalizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _businessInvitationLinkBuilder = businessInvitationLinkBuilder ?? throw new ArgumentNullException(nameof(businessInvitationLinkBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _communicationLocalizer = communicationLocalizer ?? throw new ArgumentNullException(nameof(communicationLocalizer));
        }

        public async Task<Guid> HandleAsync(BusinessInvitationCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var business = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.BusinessId && !x.IsDeleted, ct);
            if (business is null)
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();

            var existingMember = await (
                from user in _db.Set<User>()
                join member in _db.Set<BusinessMember>() on user.Id equals member.UserId
                where user.NormalizedEmail == normalizedEmail &&
                      !user.IsDeleted &&
                      member.BusinessId == dto.BusinessId &&
                      !member.IsDeleted
                select member.Id)
                .AnyAsync(ct);
            if (existingMember)
                throw new InvalidOperationException(_localizer["BusinessInvitationEmailAlreadyBelongsToExistingMember"]);

            var existingInvitation = await _db.Set<BusinessInvitation>()
                .Where(x => x.BusinessId == dto.BusinessId && x.NormalizedEmail == normalizedEmail && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (existingInvitation != null &&
                (existingInvitation.Status == BusinessInvitationStatus.Pending ||
                 (existingInvitation.Status == BusinessInvitationStatus.Expired && existingInvitation.AcceptedAtUtc == null)))
            {
                throw new InvalidOperationException(_localizer["BusinessInvitationPendingOrExpiredAlreadyExistsForEmail"]);
            }

            var token = RandomTokenGenerator.UrlSafeToken(32);
            var expiresAtUtc = _clock.UtcNow.AddDays(dto.ExpiresInDays);
            var entity = new BusinessInvitation
            {
                BusinessId = dto.BusinessId,
                InvitedByUserId = _currentUser.GetCurrentUserId(),
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                Role = dto.Role,
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                Status = BusinessInvitationStatus.Pending,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
            };

            _db.Set<BusinessInvitation>().Add(entity);
            await _db.SaveChangesAsync(ct);

            var acceptanceLink = _businessInvitationLinkBuilder.BuildAcceptanceLink(token);
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
            var invitedIntroTemplate = CommunicationTemplateDefaults.ResolveText(_communicationLocalizer, communicationCulture, "BusinessInvitationIntroInvitedHtml");
            var subjectParameters = new Dictionary<string, string?>
            {
                ["business_name"] = business.Name,
                ["role"] = dto.Role.ToString(),
                ["invitation_action"] = "invited"
            };
            var bodyParameters = new Dictionary<string, string?>
            {
                ["business_name"] = business.Name,
                ["role"] = dto.Role.ToString(),
                ["token"] = token,
                ["expires_at_utc"] = expiresAtUtc.ToString("u"),
                ["acceptance_link"] = acceptanceLink,
                ["acceptance_link_html"] = string.IsNullOrWhiteSpace(acceptanceLink)
                    ? string.Empty
                    : TransactionalEmailTemplateRenderer.Render(acceptanceLinkTemplate, acceptanceLinkTemplate, new Dictionary<string, string?> { ["acceptance_link"] = acceptanceLink }),
                ["invitation_action"] = "invited",
                ["invitation_intro_html"] = TransactionalEmailTemplateRenderer.Render(
                    invitedIntroTemplate,
                    invitedIntroTemplate,
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
            var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? entity.Email : siteSettings.CommunicationTestInboxEmail!;
            body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, entity.Email, recipient, body);

            await _emailSender.SendAsync(
                recipient,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "BusinessInvitation",
                    TemplateKey = "BusinessInvitationEmail",
                    CorrelationKey = entity.Id.ToString("N"),
                    BusinessId = business.Id,
                    IntendedRecipientEmail = entity.Email,
                    TemplateParameters = bodyParameters
                });
            return entity.Id;
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
