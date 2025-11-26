using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Retrieves all loyalty accounts for the current user across all businesses.
    /// </summary>
    /// <remarks>
    /// This handler is designed for the consumer mobile app "My accounts" screen.
    /// It relies on <see cref="ICurrentUserService"/> to determine the current actor
    /// and then queries all <see cref="LoyaltyAccount"/> entities for that user.
    ///
    /// The query performs a join with <see cref="Business"/> to provide a human-friendly
    /// business name, but it does not expose any internal user identifiers.
    /// </remarks>
    public sealed class GetMyLoyaltyAccountsHandler
    {
        private readonly IAppDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMyLoyaltyAccountsHandler"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The application database context abstraction used to query loyalty accounts and businesses.
        /// </param>
        /// <param name="currentUserService">
        /// Service providing the identifier of the current authenticated user.
        /// </param>
        public GetMyLoyaltyAccountsHandler(
            IAppDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Gets a list of loyalty account summaries for the current user.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing a read-only list of
        /// <see cref="LoyaltyAccountSummaryDto"/> on success, or an error message on failure.
        /// </returns>
        public async Task<Result<IReadOnlyList<LoyaltyAccountSummaryDto>>> HandleAsync(
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetCurrentUserId();

            // Query all accounts for the current user and join with businesses to obtain names.
            var query =
                from account in _dbContext.Set<LoyaltyAccount>().AsQueryable()
                join business in _dbContext.Set<Business>().AsQueryable()
                    on account.BusinessId equals business.Id
                where account.UserId == userId && !account.IsDeleted && !business.IsDeleted
                orderby business.Name, account.CreatedAtUtc
                select new LoyaltyAccountSummaryDto
                {
                    Id = account.Id,
                    BusinessId = account.BusinessId,
                    BusinessName = business.Name,
                    PointsBalance = account.PointsBalance,
                    LifetimePoints = account.LifetimePoints,
                    Status = account.Status,
                    LastAccrualAtUtc = account.LastAccrualAtUtc
                };

            var items = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<LoyaltyAccountSummaryDto>>.Ok(items);
        }
    }
}
