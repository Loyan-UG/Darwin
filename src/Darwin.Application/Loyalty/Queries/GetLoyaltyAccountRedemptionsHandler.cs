using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Query handler that returns redemption history entries for a specific loyalty account.
    /// Redemptions are derived from <see cref="LoyaltyRewardRedemption"/> and, when possible,
    /// enriched with scan-session information and notes from the associated points transaction.
    /// </summary>
    public sealed class GetLoyaltyAccountRedemptionsHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLoyaltyAccountRedemptionsHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction used for querying loyalty entities.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public GetLoyaltyAccountRedemptionsHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns the most recent reward redemptions for the specified loyalty account.
        /// The result is ordered by creation time descending (newest first) and limited
        /// by <paramref name="maxCount"/> for practical payload sizes.
        /// </summary>
        /// <param name="loyaltyAccountId">Identifier of the loyalty account whose redemptions should be returned.</param>
        /// <param name="maxCount">
        /// Maximum number of rows to return. If zero or negative, a default
        /// (currently 50) is applied.
        /// </param>
        /// <param name="ct">Cancellation token used to cancel the database query.</param>
        /// <returns>
        /// A read-only list of <see cref="LoyaltyRewardRedemptionListItemDto"/> entries
        /// describing the redemption history for the account.
        /// </returns>
        public async Task<IReadOnlyList<LoyaltyRewardRedemptionListItemDto>> HandleAsync(
            Guid loyaltyAccountId,
            int maxCount = 50,
            CancellationToken ct = default)
        {
            if (loyaltyAccountId == Guid.Empty)
            {
                throw new ArgumentException("Loyalty account id must not be empty.", nameof(loyaltyAccountId));
            }

            if (maxCount <= 0)
            {
                maxCount = 50;
            }

            var redemptionQuery = _db.Set<LoyaltyRewardRedemption>().AsNoTracking()
                .Where(r => r.LoyaltyAccountId == loyaltyAccountId);

            var accountQuery = _db.Set<LoyaltyAccount>().AsNoTracking();
            var userQuery = _db.Set<User>().AsNoTracking();
            var txQuery = _db.Set<LoyaltyPointsTransaction>().AsNoTracking();
            var scanQuery = _db.Set<ScanSession>().AsNoTracking();
            var rewardTierQuery = _db.Set<LoyaltyRewardTier>().AsNoTracking();

            var query =
                from redemption in redemptionQuery
                join account in accountQuery
                    on redemption.LoyaltyAccountId equals account.Id
                join user in userQuery
                    on account.UserId equals user.Id
                join rewardTier in rewardTierQuery
                    on redemption.LoyaltyRewardTierId equals rewardTier.Id into rewardTierGroup
                from rewardTier in rewardTierGroup.DefaultIfEmpty()
                join tx in txQuery
                    on redemption.Id equals tx.RewardRedemptionId into txGroup
                from tx in txGroup.DefaultIfEmpty()
                join scan in scanQuery
                    on tx.Id equals scan.ResultingTransactionId into scanGroup
                from scan in scanGroup.DefaultIfEmpty()
                orderby redemption.CreatedAtUtc descending
                select new LoyaltyRewardRedemptionListItemDto
                {
                    Id = redemption.Id,
                    LoyaltyAccountId = redemption.LoyaltyAccountId,
                    BusinessId = redemption.BusinessId,
                    ConsumerUserId = account.UserId,
                    RewardTierId = redemption.LoyaltyRewardTierId,
                    RewardLabel = rewardTier != null
                        ? (!string.IsNullOrWhiteSpace(rewardTier.Description)
                            ? rewardTier.Description!
                            : rewardTier.RewardType.ToString())
                        : redemption.LoyaltyRewardTierId.ToString(),
                    PointsSpent = redemption.PointsSpent,
                    Status = redemption.Status,
                    RedeemedAtUtc = redemption.RedeemedAtUtc ?? redemption.CreatedAtUtc,
                    Note = tx != null ? tx.Notes : null,
                    ConsumerDisplayName =
                        string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    ConsumerEmail = user.Email,
                    ScanStatus = scan != null ? scan.Status : null,
                    ScanOutcome = scan != null ? scan.Outcome : null,
                    ScanFailureReason = scan != null ? scan.FailureReason : null,
                    BusinessLocationId = redemption.BusinessLocationId,
                    RowVersion = redemption.RowVersion
                };

            var items = await query
                .Take(maxCount)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return items;
        }
    }
}
