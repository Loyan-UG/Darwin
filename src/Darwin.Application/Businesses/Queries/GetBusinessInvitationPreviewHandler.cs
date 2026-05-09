using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Resolves a business invitation preview for unauthenticated onboarding clients.
    /// The handler never exposes the raw token back to the caller.
    /// </summary>
    public sealed class GetBusinessInvitationPreviewHandler
    {
        private const int MaxInvitationTokenLength = 256;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetBusinessInvitationPreviewHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Result<BusinessInvitationPreviewDto>> HandleAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<BusinessInvitationPreviewDto>.Fail(_localizer["InvitationTokenRequired"]);
            }

            var trimmedToken = token.Trim();
            if (trimmedToken.Length > MaxInvitationTokenLength)
            {
                return Result<BusinessInvitationPreviewDto>.Fail(_localizer["InvitationNotFound"]);
            }

            var utcNow = _clock.UtcNow;

            var invitation = await _db.Set<BusinessInvitation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Token == trimmedToken && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (invitation is null)
            {
                return Result<BusinessInvitationPreviewDto>.Fail(_localizer["InvitationNotFound"]);
            }

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null || !IsInvitationPreviewAllowed(business))
            {
                return Result<BusinessInvitationPreviewDto>.Fail(_localizer["InvitedBusinessUnavailable"]);
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

        private static bool IsInvitationPreviewAllowed(Business business)
            => business.OperationalStatus == BusinessOperationalStatus.PendingApproval ||
               (business.OperationalStatus == BusinessOperationalStatus.Approved && business.IsActive);
    }
}

