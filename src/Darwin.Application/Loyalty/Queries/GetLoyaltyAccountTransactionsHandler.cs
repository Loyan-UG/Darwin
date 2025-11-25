using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Query handler that returns the most recent loyalty points transactions
    /// for a given loyalty account. This is primarily used by mobile/consumer
    /// apps to display the points ledger history for the currently active account.
    /// </summary>
    public sealed class GetLoyaltyAccountTransactionsHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLoyaltyAccountTransactionsHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction used for querying loyalty entities.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public GetLoyaltyAccountTransactionsHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns the most recent transactions for the specified loyalty account.
        /// The result is ordered by creation time descending (newest first) and
        /// limited by <paramref name="maxCount"/> to avoid excessive payloads.
        /// </summary>
        /// <param name="loyaltyAccountId">Identifier of the loyalty account whose ledger should be returned.</param>
        /// <param name="maxCount">
        /// Maximum number of rows to return. If zero or negative, a sensible default
        /// (currently 50) is applied.
        /// </param>
        /// <param name="ct">Cancellation token used to cancel the database query.</param>
        /// <returns>
        /// A read-only list of <see cref="LoyaltyPointsTransactionListItemDto"/> items representing
        /// the points ledger for the given account.
        /// </returns>
        public async Task<IReadOnlyList<LoyaltyPointsTransactionListItemDto>> HandleAsync(
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

            // Base query: all transactions for the given account.
            var txQuery = _db.Set<LoyaltyPointsTransaction>().AsNoTracking()
                .Where(t => t.LoyaltyAccountId == loyaltyAccountId);

            // Join with account to obtain the consumer user id.
            var accountQuery = _db.Set<LoyaltyAccount>().AsNoTracking();

            // Optional join with scan sessions to expose ScanSessionId via ResultingTransactionId.
            var scanQuery = _db.Set<ScanSession>().AsNoTracking();

            var query =
                from tx in txQuery
                join account in accountQuery
                    on tx.LoyaltyAccountId equals account.Id
                join scan in scanQuery
                    on tx.Id equals scan.ResultingTransactionId into scanGroup
                from scan in scanGroup.DefaultIfEmpty()
                orderby tx.CreatedAtUtc descending
                select new LoyaltyPointsTransactionListItemDto
                {
                    Id = tx.Id,
                    LoyaltyAccountId = tx.LoyaltyAccountId,
                    BusinessId = tx.BusinessId,

                    // PerformedByUserId is the staff/business user on the business device.
                    BusinessUserId = tx.PerformedByUserId,
                    // Consumer user id is carried by the loyalty account itself.
                    ConsumerUserId = account.UserId,

                    PointsDelta = tx.PointsDelta,
                    // Domain uses "Notes"; DTO exposes "Note".
                    Note = tx.Notes,
                    // Use creation timestamp as the logical occurrence timestamp.
                    OccurredAtUtc = tx.CreatedAtUtc,

                    // When the transaction originated from a scan, expose the scan session id.
                    ScanSessionId = scan != null ? scan.Id : (Guid?)null,
                    // In the domain the link is RewardRedemptionId; the DTO uses RewardTierId.
                    // This maps the redemption foreign key so the client can resolve tier details if needed.
                    RewardTierId = tx.RewardRedemptionId
                };

            var items = await query
                .Take(maxCount)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return items;
        }
    }
}
