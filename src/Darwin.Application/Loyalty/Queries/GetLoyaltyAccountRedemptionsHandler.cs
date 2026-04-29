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
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLoyaltyAccountRedemptionsHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction used for querying loyalty entities.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public GetLoyaltyAccountRedemptionsHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
                throw new ArgumentException(_localizer["LoyaltyAccountIdRequired"], nameof(loyaltyAccountId));
            }

            if (maxCount <= 0)
            {
                maxCount = 50;
            }

            var redemptionQuery = _db.Set<LoyaltyRewardRedemption>().AsNoTracking()
                .Where(r => r.LoyaltyAccountId == loyaltyAccountId && !r.IsDeleted);

            var accountQuery = _db.Set<LoyaltyAccount>().AsNoTracking().Where(a => !a.IsDeleted);
            var userQuery = _db.Set<User>().AsNoTracking().Where(u => !u.IsDeleted);
            var txQuery = _db.Set<LoyaltyPointsTransaction>().AsNoTracking().Where(t => !t.IsDeleted);
            var scanQuery = _db.Set<ScanSession>().AsNoTracking().Where(s => !s.IsDeleted);
            var rewardTierQuery = _db.Set<LoyaltyRewardTier>().AsNoTracking().Where(t => !t.IsDeleted);

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
                select new
                {
                    Id = redemption.Id,
                    LoyaltyAccountId = redemption.LoyaltyAccountId,
                    BusinessId = redemption.BusinessId,
                    ConsumerUserId = account.UserId,
                    RewardTierId = redemption.LoyaltyRewardTierId,
                    RewardTierDescription = rewardTier != null ? rewardTier.Description : null,
                    RewardTierType = rewardTier != null ? rewardTier.RewardType : (Darwin.Domain.Enums.LoyaltyRewardType?)null,
                    PointsSpent = redemption.PointsSpent,
                    Status = redemption.Status,
                    RedeemedAtUtc = redemption.RedeemedAtUtc ?? redemption.CreatedAtUtc,
                    Note = tx != null ? tx.Notes : null,
                    ConsumerDisplayName =
                        string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    ConsumerEmail = user.Email,
                    ScanStatus = scan != null ? (Darwin.Domain.Enums.LoyaltyScanStatus?)scan.Status : null,
                    ScanOutcome = scan != null ? scan.Outcome : null,
                    ScanFailureReason = scan != null ? scan.FailureReason : null,
                    BusinessLocationId = redemption.BusinessLocationId,
                    RowVersion = redemption.RowVersion
                };

            var items = await query
                .Take(maxCount)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return items
                .Select(x => new LoyaltyRewardRedemptionListItemDto
                {
                    Id = x.Id,
                    LoyaltyAccountId = x.LoyaltyAccountId,
                    BusinessId = x.BusinessId,
                    ConsumerUserId = x.ConsumerUserId,
                    RewardTierId = x.RewardTierId,
                    RewardLabel = x.RewardTierDescription is { Length: > 0 }
                        ? x.RewardTierDescription
                        : x.RewardTierType?.ToString() ?? x.RewardTierId.ToString(),
                    PointsSpent = x.PointsSpent,
                    Status = x.Status,
                    RedeemedAtUtc = x.RedeemedAtUtc,
                    Note = x.Note,
                    ConsumerDisplayName = x.ConsumerDisplayName,
                    ConsumerEmail = x.ConsumerEmail,
                    ScanStatus = x.ScanStatus,
                    ScanOutcome = x.ScanOutcome,
                    ScanFailureReason = x.ScanFailureReason,
                    BusinessLocationId = x.BusinessLocationId,
                    RowVersion = x.RowVersion
                })
                .ToList();
        }
    }
}
