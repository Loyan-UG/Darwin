using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Returns paged redemption rows for admin troubleshooting across a business.
    /// </summary>
    public sealed class GetLoyaltyRedemptionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyRedemptionsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(IReadOnlyList<LoyaltyRewardRedemptionListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            LoyaltyRedemptionStatus? status = null,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var baseQuery =
                from redemption in _db.Set<LoyaltyRewardRedemption>().AsNoTracking()
                join account in _db.Set<LoyaltyAccount>().AsNoTracking() on redemption.LoyaltyAccountId equals account.Id
                join user in _db.Set<User>().AsNoTracking() on account.UserId equals user.Id
                join rewardTier in _db.Set<LoyaltyRewardTier>().AsNoTracking() on redemption.LoyaltyRewardTierId equals rewardTier.Id into rewardTierGroup
                from rewardTier in rewardTierGroup.DefaultIfEmpty()
                join tx in _db.Set<LoyaltyPointsTransaction>().AsNoTracking() on redemption.Id equals tx.RewardRedemptionId into txGroup
                from tx in txGroup.DefaultIfEmpty()
                join scan in _db.Set<ScanSession>().AsNoTracking() on tx.Id equals scan.ResultingTransactionId into scanGroup
                from scan in scanGroup.DefaultIfEmpty()
                where redemption.BusinessId == businessId &&
                      !redemption.IsDeleted &&
                      !account.IsDeleted &&
                      !user.IsDeleted &&
                      (rewardTier == null || !rewardTier.IsDeleted) &&
                      (tx == null || !tx.IsDeleted) &&
                      (scan == null || !scan.IsDeleted)
                select new { redemption, account, user, rewardTier, tx, scan };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.user.Email.Contains(term) ||
                    ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Contains(term) ||
                    (x.rewardTier != null && x.rewardTier.Description != null && x.rewardTier.Description.Contains(term)) ||
                    (x.scan != null && x.scan.Outcome.Contains(term)));
            }

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.redemption.Status == status.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.redemption.RedeemedAtUtc ?? x.redemption.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LoyaltyRewardRedemptionListItemDto
                {
                    Id = x.redemption.Id,
                    LoyaltyAccountId = x.redemption.LoyaltyAccountId,
                    BusinessId = x.redemption.BusinessId,
                    ConsumerUserId = x.account.UserId,
                    RewardTierId = x.redemption.LoyaltyRewardTierId,
                    RewardLabel = x.rewardTier != null
                        ? (!string.IsNullOrWhiteSpace(x.rewardTier.Description) ? x.rewardTier.Description! : x.rewardTier.RewardType.ToString())
                        : x.redemption.LoyaltyRewardTierId.ToString(),
                    PointsSpent = x.redemption.PointsSpent,
                    Status = x.redemption.Status,
                    RedeemedAtUtc = x.redemption.RedeemedAtUtc ?? x.redemption.CreatedAtUtc,
                    Note = x.tx != null ? x.tx.Notes : null,
                    ConsumerDisplayName =
                        string.IsNullOrWhiteSpace(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                            ? x.user.Email
                            : ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim(),
                    ConsumerEmail = x.user.Email,
                    ScanStatus = x.scan != null ? x.scan.Status : null,
                    ScanOutcome = x.scan != null ? x.scan.Outcome : null,
                    ScanFailureReason = x.scan != null ? x.scan.FailureReason : null,
                    BusinessLocationId = x.redemption.BusinessLocationId,
                    RowVersion = x.redemption.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<LoyaltyRedemptionOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var query =
                from redemption in _db.Set<LoyaltyRewardRedemption>().AsNoTracking()
                join tx in _db.Set<LoyaltyPointsTransaction>().AsNoTracking() on redemption.Id equals tx.RewardRedemptionId into txGroup
                from tx in txGroup.DefaultIfEmpty()
                join scan in _db.Set<ScanSession>().AsNoTracking() on tx.Id equals scan.ResultingTransactionId into scanGroup
                from scan in scanGroup.DefaultIfEmpty()
                where redemption.BusinessId == businessId &&
                      !redemption.IsDeleted &&
                      (tx == null || !tx.IsDeleted) &&
                      (scan == null || !scan.IsDeleted)
                select new { redemption.Status, ScanFailureReason = scan != null ? scan.FailureReason : null, ScanStatus = scan != null ? scan.Status : (LoyaltyScanStatus?)null };

            return await query
                .GroupBy(_ => 1)
                .Select(group => new LoyaltyRedemptionOpsSummaryDto
                {
                    TotalCount = group.Count(),
                    PendingCount = group.Count(x => x.Status == LoyaltyRedemptionStatus.Pending),
                    CompletedCount = group.Count(x => x.Status == LoyaltyRedemptionStatus.Completed),
                    CancelledCount = group.Count(x => x.Status == LoyaltyRedemptionStatus.Cancelled),
                    ScanFailureCount = group.Count(x =>
                        x.ScanStatus == LoyaltyScanStatus.Cancelled ||
                        x.ScanStatus == LoyaltyScanStatus.Expired ||
                        (x.ScanFailureReason != null && x.ScanFailureReason != string.Empty))
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false)
                ?? new LoyaltyRedemptionOpsSummaryDto();
        }
    }
}
