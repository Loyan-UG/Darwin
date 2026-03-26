using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Resolves a business invitation preview for unauthenticated onboarding clients.
    /// The handler never exposes the raw token back to the caller.
    /// </summary>
    public sealed class GetBusinessInvitationPreviewHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessInvitationPreviewHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Result<BusinessInvitationPreviewDto>> HandleAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<BusinessInvitationPreviewDto>.Fail("Invitation token is required.");
            }

            var trimmedToken = token.Trim();
            var utcNow = DateTime.UtcNow;

            var invitation = await _db.Set<BusinessInvitation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Token == trimmedToken && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (invitation is null)
            {
                return Result<BusinessInvitationPreviewDto>.Fail("Invitation not found.");
            }

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null || !business.IsActive)
            {
                return Result<BusinessInvitationPreviewDto>.Fail("The invited business is not available.");
            }

            var effectiveStatus = invitation.Status == BusinessInvitationStatus.Pending && invitation.ExpiresAtUtc <= utcNow
                ? BusinessInvitationStatus.Expired
                : invitation.Status;

            var hasExistingUser = await _db.Set<User>()
                .AsNoTracking()
                .AnyAsync(x => x.NormalizedEmail == invitation.NormalizedEmail && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            return Result<BusinessInvitationPreviewDto>.Ok(new BusinessInvitationPreviewDto
            {
                InvitationId = invitation.Id,
                BusinessId = business.Id,
                BusinessName = business.Name,
                Email = invitation.Email,
                Role = invitation.Role.ToString(),
                Status = effectiveStatus.ToString(),
                ExpiresAtUtc = invitation.ExpiresAtUtc,
                HasExistingUser = hasExistingUser
            });
        }
    }
}
