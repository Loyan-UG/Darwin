using System;
using System.Collections.Generic;
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
    /// Retrieves the transaction history of the current user for a specific business.
    /// </summary>
    /// <remarks>
    /// This handler resolves the loyalty account for the current user and business,
    /// and then returns a chronological list of <see cref="LoyaltyPointsTransactionDto"/>.
    ///
    /// It is designed for "My history" style screens in the consumer app, scoped to a
    /// single business. Pagination can be added later if required; for MVP and typical
    /// cafe usage, the amount of data is expected to be modest.
    /// </remarks>
    public sealed class GetMyLoyaltyHistoryHandler
    {
        private readonly IAppDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMyLoyaltyHistoryHandler"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The application database context abstraction used to query loyalty accounts and transactions.
        /// </param>
        /// <param name="currentUserService">
        /// Service providing the identifier of the current authenticated user.
        /// </param>
        public GetMyLoyaltyHistoryHandler(
            IAppDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Gets the transaction history of the current user for the specified business.
        /// </summary>
        /// <param name="businessId">
        /// The identifier of the business for which the history should be retrieved.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing a read-only list of
        /// <see cref="LoyaltyPointsTransactionDto"/> on success, or an error message on failure.
        /// </returns>
        public async Task<Result<IReadOnlyList<LoyaltyPointsTransactionDto>>> HandleAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetCurrentUserId();

            // First resolve the loyalty account for the current user and business.
            var account = await _dbContext
                .Set<LoyaltyAccount>()
                .AsQueryable()
                .Where(a => a.BusinessId == businessId && a.UserId == userId && !a.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (account is null)
            {
                // No account yet for this business; from the app perspective it is fine
                // to treat this as an empty history instead of an error, but returning
                // an explicit failure makes the semantics clear to the caller.
                return Result<IReadOnlyList<LoyaltyPointsTransactionDto>>.Fail(
                    "Loyalty account not found for the specified business and user.");
            }

            // Query all transactions for the resolved account, ordered by newest first.
            var query =
                from tx in _dbContext.Set<LoyaltyPointsTransaction>().AsQueryable()
                where tx.LoyaltyAccountId == account.Id && !tx.IsDeleted
                orderby tx.CreatedAtUtc descending
                select new LoyaltyPointsTransactionDto
                {
                    Id = tx.Id,
                    LoyaltyAccountId = tx.LoyaltyAccountId,
                    Type = tx.Type,
                    PointsDelta = tx.PointsDelta,
                    CreatedAtUtc = tx.CreatedAtUtc,
                    Reference = tx.Reference,
                    Notes = tx.Notes,
                    BusinessLocationId = tx.BusinessLocationId,
                    RewardRedemptionId = tx.RewardRedemptionId
                };

            var items = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<LoyaltyPointsTransactionDto>>.Ok(items);
        }
    }
}
