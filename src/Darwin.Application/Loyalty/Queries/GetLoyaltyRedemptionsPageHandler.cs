using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
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
                var term = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.user.Email, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)), term, QueryLikePattern.EscapeCharacter) ||
                    (x.rewardTier != null && x.rewardTier.Description != null && EF.Functions.Like(x.rewardTier.Description, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.scan != null && EF.Functions.Like(x.scan.Outcome, term, QueryLikePattern.EscapeCharacter)));
            }

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.redemption.Status == status.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var rows = await baseQuery
                .OrderByDescending(x => x.redemption.RedeemedAtUtc ?? x.redemption.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Id = x.redemption.Id,
                    LoyaltyAccountId = x.redemption.LoyaltyAccountId,
                    BusinessId = x.redemption.BusinessId,
                    ConsumerUserId = x.account.UserId,
                    RewardTierId = x.redemption.LoyaltyRewardTierId,
                    RewardTierDescription = x.rewardTier != null ? x.rewardTier.Description : null,
                    RewardTierType = x.rewardTier != null ? x.rewardTier.RewardType : (LoyaltyRewardType?)null,
                    PointsSpent = x.redemption.PointsSpent,
                    Status = x.redemption.Status,
                    RedeemedAtUtc = x.redemption.RedeemedAtUtc ?? x.redemption.CreatedAtUtc,
                    Note = x.tx != null ? x.tx.Notes : null,
                    ConsumerDisplayName =
                        string.IsNullOrWhiteSpace(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                            ? x.user.Email
                            : ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim(),
                    ConsumerEmail = x.user.Email,
                    ScanStatus = x.scan != null ? (LoyaltyScanStatus?)x.scan.Status : null,
                    ScanOutcome = x.scan != null ? x.scan.Outcome : null,
                    ScanFailureReason = x.scan != null ? x.scan.FailureReason : null,
                    BusinessLocationId = x.redemption.BusinessLocationId,
                    RowVersion = x.redemption.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var items = rows
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
