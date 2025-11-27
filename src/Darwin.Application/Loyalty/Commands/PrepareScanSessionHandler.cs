using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Prepares a new scan session for the current consumer user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handler resolves the loyalty account for the current user and the
    /// specified business. It then creates a short-lived <see cref="ScanSession"/>
    /// with the requested mode (accrual or redemption).
    /// </para>
    /// <para>
    /// For redemption mode, the handler validates that the selected rewards
    /// exist and that the account has enough points. It stores a JSON snapshot
    /// of the selected rewards in the session to support replay-safe validation
    /// on the business device.
    /// </para>
    /// <para>
    /// The returned <see cref="ScanSessionPreparedDto.ScanSessionId"/> is the
    /// only data that needs to appear in the QR code.
    /// </para>
    /// </remarks>
    public sealed class PrepareScanSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        /// <summary>
        /// Default scan session lifetime in minutes. Keeping it short significantly
        /// reduces the replay window while keeping UX acceptable.
        /// </summary>
        private const int DefaultSessionLifetimeMinutes = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareScanSessionHandler"/> class.
        /// </summary>
        public PrepareScanSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Creates a new scan session for the current user and returns the data
        /// required to render the QR code.
        /// </summary>
        public async Task<Result<ScanSessionPreparedDto>> HandleAsync(
            PrepareScanSessionDto dto,
            CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty)
            {
                return Result<ScanSessionPreparedDto>.Fail("BusinessId is required.");
            }

            var userId = _currentUserService.GetCurrentUserId();

            // Resolve existing loyalty account for this user and business.
            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a =>
                    a.BusinessId == dto.BusinessId &&
                    a.UserId == userId &&
                    !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ScanSessionPreparedDto>.Fail("Loyalty account not found for the specified business.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<ScanSessionPreparedDto>.Fail("Loyalty account is not active.");
            }

            // If redemption mode, validate reward selections and ensure points are sufficient.
            string? selectedRewardsJson = null;

            if (dto.Mode == LoyaltyScanMode.Redemption &&
                dto.SelectedRewardTierIds.Count > 0)
            {
                selectedRewardsJson = await BuildAndValidateSelectedRewardsJsonAsync(
                    dto.SelectedRewardTierIds,
                    account,
                    ct).ConfigureAwait(false);

                if (selectedRewardsJson is null)
                {
                    return Result<ScanSessionPreparedDto>.Fail("Insufficient points for selected rewards.");
                }
            }

            var now = _clock.UtcNow;
            var expiresAt = now.AddMinutes(DefaultSessionLifetimeMinutes);

            var session = new ScanSession
            {
                // QrCodeTokenId can be left as Guid.Empty when we use ScanSessionId directly
                // as QR payload. It remains available for future correlation if needed.
                QrCodeTokenId = Guid.Empty,
                LoyaltyAccountId = account.Id,
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Mode = dto.Mode,
                Status = LoyaltyScanStatus.Pending,
                SelectedRewardsJson = selectedRewardsJson,
                ExpiresAtUtc = expiresAt,
                CreatedByDeviceId = dto.DeviceId,
                Outcome = "Pending"
            };

            _db.Set<ScanSession>().Add(session);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var resultDto = new ScanSessionPreparedDto
            {
                ScanSessionId = session.Id,
                Mode = session.Mode,
                ExpiresAtUtc = session.ExpiresAtUtc,
                CurrentPointsBalance = account.PointsBalance
            };

            return Result<ScanSessionPreparedDto>.Ok(resultDto);
        }

        /// <summary>
        /// Validates the selected reward tiers against the loyalty account and
        /// returns a JSON payload describing the selections if valid.
        /// </summary>
        /// <param name="rewardTierIds">The reward tiers selected for redemption.</param>
        /// <param name="account">The loyalty account of the current user.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A JSON string representing the selected rewards, or <c>null</c> when
        /// the account does not have enough points.
        /// </returns>
        private async Task<string?> BuildAndValidateSelectedRewardsJsonAsync(
            IReadOnlyCollection<Guid> rewardTierIds,
            LoyaltyAccount account,
            CancellationToken ct)
        {
            var tiers = await _db.Set<LoyaltyRewardTier>()
                .AsQueryable()
                .Where(t =>
                    rewardTierIds.Contains(t.Id) &&
                    !t.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (tiers.Count == 0)
            {
                return null;
            }

            var items = new List<SelectedRewardItemDto>(tiers.Count);
            var totalRequiredPoints = 0;

            foreach (var tier in tiers)
            {
                // PointsRequired is the threshold for a single redemption.
                var requiredPerUnit = tier.PointsRequired;
                if (requiredPerUnit <= 0)
                {
                    continue;
                }

                var item = new SelectedRewardItemDto
                {
                    LoyaltyRewardTierId = tier.Id,
                    Quantity = 1,
                    RequiredPointsPerUnit = requiredPerUnit
                };

                items.Add(item);
                totalRequiredPoints += requiredPerUnit;
            }

            if (totalRequiredPoints <= 0)
            {
                return null;
            }

            if (account.PointsBalance < totalRequiredPoints)
            {
                return null;
            }

            // Serialize with System.Text.Json; Application layer is allowed to depend on BCL.
            return JsonSerializer.Serialize(items);
        }
    }
}
