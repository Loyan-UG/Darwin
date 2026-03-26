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

        public RevokeBusinessInvitationHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<BusinessInvitationRevokeDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessInvitationRevokeDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (invitation is null)
                throw new InvalidOperationException("Invitation not found.");

            if (invitation.Status == BusinessInvitationStatus.Accepted)
                throw new InvalidOperationException("Accepted invitations cannot be revoked.");

            invitation.Revoke(_clock.UtcNow, string.IsNullOrWhiteSpace(dto.Note) ? invitation.Note : dto.Note.Trim());
            await _db.SaveChangesAsync(ct);
        }
    }
}
