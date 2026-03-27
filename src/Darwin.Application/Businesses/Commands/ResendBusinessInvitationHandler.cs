using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Darwin.Shared.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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

        public ResendBusinessInvitationHandler(
            IAppDbContext db,
            IEmailSender emailSender,
            IClock clock,
            IBusinessInvitationLinkBuilder businessInvitationLinkBuilder,
            IValidator<BusinessInvitationResendDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _businessInvitationLinkBuilder = businessInvitationLinkBuilder ?? throw new ArgumentNullException(nameof(businessInvitationLinkBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessInvitationResendDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (invitation is null)
                throw new InvalidOperationException("Invitation not found.");

            if (invitation.Status == BusinessInvitationStatus.Accepted)
                throw new InvalidOperationException("Accepted invitations cannot be resent.");

            if (invitation.Status == BusinessInvitationStatus.Revoked)
                throw new InvalidOperationException("Revoked invitations cannot be resent. Create a new invitation instead.");

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId, ct);
            if (business is null)
                throw new InvalidOperationException("Business not found.");

            invitation.Token = RandomTokenGenerator.UrlSafeToken(32);
            invitation.ExpiresAtUtc = _clock.UtcNow.AddDays(dto.ExpiresInDays);
            invitation.Status = BusinessInvitationStatus.Pending;
            invitation.AcceptedAtUtc = null;
            invitation.AcceptedByUserId = null;
            invitation.RevokedAtUtc = null;

            await _db.SaveChangesAsync(ct);

            var acceptanceLink = _businessInvitationLinkBuilder.BuildAcceptanceLink(invitation.Token);
            var subject = $"Invitation to join {business.Name} on Darwin";
            var body =
                $"<p>Hello,</p>" +
                $"<p>Your invitation to join <strong>{business.Name}</strong> on Darwin as <strong>{invitation.Role}</strong> has been reissued.</p>" +
                (string.IsNullOrWhiteSpace(acceptanceLink)
                    ? string.Empty
                    : $"<p><a href=\"{acceptanceLink}\">Open your invitation</a></p>") +
                $"<p>Your new invitation token is:</p>" +
                $"<p><code>{invitation.Token}</code></p>" +
                $"<p>This invitation expires at <strong>{invitation.ExpiresAtUtc:u}</strong>.</p>";

            await _emailSender.SendAsync(invitation.Email, subject, body, ct);
        }
    }
}
