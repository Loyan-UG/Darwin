using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Query handler that returns a unified loyalty timeline page for the current user and a given business.
    /// The timeline merges:
    /// - <see cref="LoyaltyPointsTransaction"/> (ledger entries)
    /// - <see cref="LoyaltyRewardRedemption"/> (reward usage records)
    ///
    /// Paging is implemented using keyset (cursor) based on (OccurredAtUtc, Id) descending order to ensure:
    /// - Deterministic order even when multiple rows share the same timestamp
    /// - Provider-agnostic behavior (works beyond SQL Server)
    /// - Better performance compared to large OFFSET paging
    ///
    /// IMPORTANT (Token-First Rule):
    /// This handler must never expose internal scan-session identifiers. It surfaces only user-safe ids
    /// for transactions/redemptions and relies on other dedicated QR/token components for scan flows.
    /// </summary>
    public sealed class GetMyLoyaltyTimelinePageHandler
    {
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMyLoyaltyTimelinePageHandler"/> class.
        /// </summary>
        /// <param name="dbContext">Application database abstraction.</param>
        /// <param name="currentUserService">Current user accessor.</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
        public GetMyLoyaltyTimelinePageHandler(
            IAppDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Returns a single page of unified timeline entries for the current user in the specified business.
        /// </summary>
        /// <param name="dto">Paging request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing a single page of timeline entries.</returns>
        public async Task<Result<LoyaltyTimelinePageDto>> HandleAsync(
            GetMyLoyaltyTimelinePageDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (dto.BusinessId == Guid.Empty) return Result<LoyaltyTimelinePageDto>.Fail("Business id must not be empty.");

            var pageSize = dto.PageSize <= 0 ? DefaultPageSize : dto.PageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            // Cursor correctness:
            // For deterministic keyset paging, both cursor parts must be provided together.
            if ((dto.BeforeAtUtc is null) != (dto.BeforeId is null))
            {
                return Result<LoyaltyTimelinePageDto>.Fail("Invalid cursor. Both BeforeAtUtc and BeforeId must be provided together.");
            }

            var userId = _currentUserService.GetCurrentUserId();

            // Resolve the loyalty account for the current user and business.
            var account = await _dbContext
                .Set<LoyaltyAccount>()
                .AsNoTracking()
                .Where(a => a.BusinessId == dto.BusinessId && a.UserId == userId && !a.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<LoyaltyTimelinePageDto>.Fail("Loyalty account not found for the specified business and user.");
            }

            // Transactions query projected into unified timeline entries.
            // We keep OccurredAtUtc aligned with existing handlers (CreatedAtUtc).
            var txQuery =
                from tx in _dbContext.Set<LoyaltyPointsTransaction>().AsNoTracking()
                where tx.LoyaltyAccountId == account.Id && !tx.IsDeleted
                select new LoyaltyTimelineEntryDto
                {
                    Kind = LoyaltyTimelineEntryKind.PointsTransaction,
                    Id = tx.Id,
                    LoyaltyAccountId = tx.LoyaltyAccountId,
                    BusinessId = tx.BusinessId,
                    OccurredAtUtc = tx.CreatedAtUtc,
                    PointsDelta = tx.PointsDelta,
                    PointsSpent = null,
                    RewardTierId = null,
                    Reference = tx.Reference,
                    Note = tx.Notes
                };

            // Redemptions query projected into unified timeline entries.
            // RedeemedAtUtc in some handlers uses CreatedAtUtc; we keep the same.
            var redemptionQuery =
                from r in _dbContext.Set<LoyaltyRewardRedemption>().AsNoTracking()
                where r.LoyaltyAccountId == account.Id && !r.IsDeleted
                select new LoyaltyTimelineEntryDto
                {
                    Kind = LoyaltyTimelineEntryKind.RewardRedemption,
                    Id = r.Id,
                    LoyaltyAccountId = r.LoyaltyAccountId,
                    BusinessId = r.BusinessId,
                    OccurredAtUtc = r.CreatedAtUtc,
                    PointsDelta = null,
                    PointsSpent = r.PointsSpent,
                    RewardTierId = r.LoyaltyRewardTierId,
                    Reference = null,
                    Note = null
                };

            // Merge using Concat and apply keyset paging on the unified projection.
            // Ordering: newest first; tie-breaker is Id descending.
            var merged = txQuery.Concat(redemptionQuery);

            if (dto.BeforeAtUtc is not null && dto.BeforeId is not null)
            {
                var beforeAt = dto.BeforeAtUtc.Value;
                var beforeId = dto.BeforeId.Value;

                // Keyset predicate for descending order:
                // (OccurredAtUtc < beforeAt) OR (OccurredAtUtc == beforeAt AND Id < beforeId)
                merged =
                    merged.Where(x =>
                        x.OccurredAtUtc < beforeAt ||
                        (x.OccurredAtUtc == beforeAt && x.Id.CompareTo(beforeId) < 0));
            }

            var items = await merged
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.Id)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var page = new LoyaltyTimelinePageDto
            {
                Items = items
            };

            // Compute next cursor from the last returned item (oldest in this page due to descending order).
            if (items.Count > 0)
            {
                var last = items[^1];
                page.NextBeforeAtUtc = last.OccurredAtUtc;
                page.NextBeforeId = last.Id;
            }

            return Result<LoyaltyTimelinePageDto>.Ok(page);
        }
    }
}
