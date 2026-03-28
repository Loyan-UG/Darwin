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
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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

        public CreateBusinessInvitationHandler(
            IAppDbContext db,
            IEmailSender emailSender,
            IClock clock,
            ICurrentUserService currentUser,
            IBusinessInvitationLinkBuilder businessInvitationLinkBuilder,
            IValidator<BusinessInvitationCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _businessInvitationLinkBuilder = businessInvitationLinkBuilder ?? throw new ArgumentNullException(nameof(businessInvitationLinkBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(BusinessInvitationCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var business = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.BusinessId, ct);
            if (business is null)
                throw new InvalidOperationException("Business not found.");

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();

            var existingMember = await (
                from user in _db.Set<User>()
                join member in _db.Set<BusinessMember>() on user.Id equals member.UserId
                where user.NormalizedEmail == normalizedEmail &&
                      !user.IsDeleted &&
                      member.BusinessId == dto.BusinessId
                select member.Id)
                .AnyAsync(ct);
            if (existingMember)
                throw new InvalidOperationException("This email already belongs to a member of the business. Assign or update the existing membership instead.");

            var existingInvitation = await _db.Set<BusinessInvitation>()
                .Where(x => x.BusinessId == dto.BusinessId && x.NormalizedEmail == normalizedEmail)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (existingInvitation != null &&
                (existingInvitation.Status == BusinessInvitationStatus.Pending ||
                 (existingInvitation.Status == BusinessInvitationStatus.Expired && existingInvitation.AcceptedAtUtc == null)))
            {
                throw new InvalidOperationException("A pending or expired invitation already exists for this email. Use resend instead of creating a duplicate invitation.");
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
            var siteSettings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
            var subject = ApplySubjectPrefix(
                siteSettings?.TransactionalEmailSubjectPrefix,
                TransactionalEmailTemplateRenderer.Render(
                    siteSettings?.BusinessInvitationEmailSubjectTemplate,
                    $"Invitation to join {business.Name} on Darwin",
                    new Dictionary<string, string?>
                    {
                        ["business_name"] = business.Name,
                        ["role"] = dto.Role.ToString(),
                        ["invitation_action"] = "invited"
                    }));
            var body = TransactionalEmailTemplateRenderer.Render(
                siteSettings?.BusinessInvitationEmailBodyTemplate,
                $"<p>Hello,</p>" +
                $"<p>You have been invited to join <strong>{business.Name}</strong> on Darwin as <strong>{dto.Role}</strong>.</p>" +
                (string.IsNullOrWhiteSpace(acceptanceLink)
                    ? string.Empty
                    : $"<p><a href=\"{acceptanceLink}\">Open your invitation</a></p>") +
                $"<p>Your invitation token is:</p>" +
                $"<p><code>{token}</code></p>" +
                $"<p>This invitation expires at <strong>{expiresAtUtc:u}</strong>.</p>" +
                $"<p>Use this token in the Darwin business onboarding flow or contact your administrator if you need assistance.</p>",
                new Dictionary<string, string?>
                {
                    ["business_name"] = business.Name,
                    ["role"] = dto.Role.ToString(),
                    ["token"] = token,
                    ["expires_at_utc"] = expiresAtUtc.ToString("u"),
                    ["acceptance_link"] = acceptanceLink,
                    ["acceptance_link_html"] = string.IsNullOrWhiteSpace(acceptanceLink)
                        ? string.Empty
                        : $"<p><a href=\"{acceptanceLink}\">Open your invitation</a></p>",
                    ["invitation_action"] = "invited",
                    ["invitation_intro_html"] = $"You have been invited to join <strong>{business.Name}</strong> on Darwin as <strong>{dto.Role}</strong>."
                });
            var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? entity.Email : siteSettings.CommunicationTestInboxEmail!;
            body = ApplyRecipientOverrideNotice(entity.Email, recipient, body);

            await _emailSender.SendAsync(
                recipient,
                subject,
                body,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "BusinessInvitation",
                    BusinessId = business.Id
                });
            return entity.Id;
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
