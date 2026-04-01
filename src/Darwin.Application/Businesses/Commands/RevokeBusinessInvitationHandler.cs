using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Revokes an existing business invitation.
    /// </summary>
    public sealed class RevokeBusinessInvitationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<BusinessInvitationRevokeDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public RevokeBusinessInvitationHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<BusinessInvitationRevokeDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessInvitationRevokeDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (invitation is null)
                throw new InvalidOperationException(_localizer["InvitationNotFound"]);

            if (invitation.Status == BusinessInvitationStatus.Accepted)
                throw new InvalidOperationException(_localizer["AcceptedInvitationsCannotBeRevoked"]);

            invitation.Revoke(_clock.UtcNow, string.IsNullOrWhiteSpace(dto.Note) ? invitation.Note : dto.Note.Trim());
            await _db.SaveChangesAsync(ct);
        }
    }
}
