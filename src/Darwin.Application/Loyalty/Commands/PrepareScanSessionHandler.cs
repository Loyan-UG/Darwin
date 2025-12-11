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
    /// on the business device and exposes the accepted reward tier identifiers
    /// as part of the result DTO for UI confirmation on the consumer device.
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
        /// Internal helper payload used when building the selected rewards
        /// snapshot for a redemption scan session.
        /// </summary>
        private sealed class SelectedRewardsPayload
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SelectedRewardsPayload"/> class.
            /// </summary>
            /// <param name="json">The JSON payload representing the selected rewards.</param>
            /// <param name="tierIds">The list of reward tier identifiers that were accepted.</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="json"/> or <paramref name="tierIds"/> is <c>null</c>.
            /// </exception>
            public SelectedRewardsPayload(string json, IReadOnlyList<Guid> tierIds)
            {
                Json = json ?? throw new ArgumentNullException(nameof(json));
                TierIds = tierIds ?? throw new ArgumentNullException(nameof(tierIds));
            }

            /// <summary>
            /// Gets the JSON payload that will be stored on the scan session entity.
            /// </summary>
            public string Json { get; }

            /// <summary>
            /// Gets the identifiers of the reward tiers that were actually accepted
            /// for redemption and serialized into <see cref="Json"/>.
            /// </summary>
            public IReadOnlyList<Guid> TierIds { get; }
        }

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
            IReadOnlyList<Guid> selectedRewardTierIds = Array.Empty<Guid>();

            if (dto.Mode == LoyaltyScanMode.Redemption &&
                dto.SelectedRewardTierIds.Count > 0)
            {
                var payload = await BuildAndValidateSelectedRewardsPayloadAsync(
                        dto.SelectedRewardTierIds,
                        account,
                        ct)
                    .ConfigureAwait(false);

                if (payload is null)
                {
                    return Result<ScanSessionPreparedDto>.Fail("Insufficient points for selected rewards.");
                }

                selectedRewardsJson = payload.Json;
                selectedRewardTierIds = payload.TierIds;
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
                CurrentPointsBalance = account.PointsBalance,
                SelectedRewardTierIds = selectedRewardTierIds
            };

            return Result<ScanSessionPreparedDto>.Ok(resultDto);
        }

        /// <summary>
        /// Validates the selected reward tiers against the loyalty account and
        /// returns a payload describing the selections if valid.
        /// </summary>
        /// <param name="rewardTierIds">The reward tiers selected for redemption.</param>
        /// <param name="account">The loyalty account of the current user.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="SelectedRewardsPayload"/> containing both the JSON string
        /// representing the selected rewards and the list of accepted reward tier
        /// identifiers, or <c>null</c> when the account does not have enough points
        /// or no valid rewards could be built.
        /// </returns>
        private async Task<SelectedRewardsPayload?> BuildAndValidateSelectedRewardsPayloadAsync(
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

            if (items.Count == 0)
            {
                return null;
            }

            // Serialize with System.Text.Json; Application layer is allowed to depend on BCL.
            var json = JsonSerializer.Serialize(items);

            // Expose the accepted tier ids so that the caller can enrich the response
            // using read-side queries without having to parse JSON again.
            var tierIds = items
                .Select(i => i.LoyaltyRewardTierId)
                .Distinct()
                .ToArray();

            return new SelectedRewardsPayload(json, tierIds);
        }
    }
}
