using System;
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
    /// Query handler that returns a summary of the loyalty account
    /// for the current authenticated consumer user within a specific
    /// business context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is intended for consumer/mobile scenarios where
    /// the caller is always the account owner. It resolves the user
    /// identifier from <see cref="ICurrentUserService"/> and does not
    /// require the WebApi layer to pass an explicit user id.
    /// </para>
    /// <para>
    /// The result is a <see cref="LoyaltyAccountSummaryDto"/> that
    /// contains the business name, points balance and basic status
    /// information. If no account exists yet for the current user
    /// and the specified business, the handler returns a successful
    /// <see cref="Result{T}"/> with a <c>null</c> value.
    /// </para>
    /// <para>
    /// This query complements <see cref="GetMyLoyaltyAccountsHandler"/>
    /// and <see cref="GetAvailableLoyaltyRewardsForBusinessHandler"/>
    /// by providing a single-account, business-scoped view of the
    /// loyalty state for the current consumer.
    /// </para>
    /// </remarks>
    public sealed class GetMyLoyaltyAccountForBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="GetMyLoyaltyAccountForBusinessHandler"/> class.
        /// </summary>
        /// <param name="db">
        /// Application database abstraction used to query loyalty accounts
        /// and business entities.
        /// </param>
        /// <param name="currentUserService">
        /// Service used to resolve the identifier of the currently
        /// authenticated user.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="db"/> or
        /// <paramref name="currentUserService"/> is <c>null</c>.
        /// </exception>
        public GetMyLoyaltyAccountForBusinessHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }


        /// <summary>
        /// Returns a summary of the loyalty account for the current user
        /// and the specified business, if such an account exists.
        /// </summary>
        /// <param name="businessId">
        /// Identifier of the business whose loyalty program is being queried.
        /// </param>
        /// <param name="ct">
        /// Cancellation token that can be used to cancel the database query.
        /// </param>
        /// <returns>
        /// A <see cref="Result{T}"/> wrapping a
        /// <see cref="LoyaltyAccountSummaryDto"/> instance when an account
        /// is found; otherwise a successful result with <c>null</c> value.
        /// </returns>
        public async Task<Result<LoyaltyAccountSummaryDto?>> HandleAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<LoyaltyAccountSummaryDto?>.Fail("Business id must not be empty.");
            }

            // Resolve the current authenticated user. The implementation of
            // ICurrentUserService is responsible for throwing if no user is
            // available; at this level we assume authorization has already
            // been enforced by the WebApi layer.
            var currentUserId = _currentUserService.GetCurrentUserId();

            // Base query for the loyalty account of the current user within
            // the specified business. Global query filters on IsDeleted are
            // applied by the underlying DbContext, but we still keep the
            // predicate explicit for readability.
            var query =
                from account in _db.Set<LoyaltyAccount>().AsNoTracking()
                join business in _db.Set<Business>().AsNoTracking()
                    on account.BusinessId equals business.Id
                where !account.IsDeleted
                      && !business.IsDeleted
                      && account.BusinessId == businessId
                      && account.UserId == currentUserId
                select new LoyaltyAccountSummaryDto
                {
                    Id = account.Id,
                    BusinessId = business.Id,
                    BusinessName = business.Name,
                    PointsBalance = account.PointsBalance,
                    LifetimePoints = account.LifetimePoints,
                    Status = account.Status,
                    LastAccrualAtUtc = account.LastAccrualAtUtc
                };

            // There can be at most one account per (Business, User) because
            // the domain enforces a unique index on that pair. We still use
            // SingleOrDefaultAsync for clarity and to surface inconsistencies
            // early if they ever occur.
            var summary = await query
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            // Even when no account exists yet, we treat this as a successful
            // query with a null payload. The WebApi layer can decide whether
            // to translate null to HTTP 404 or return a 200 with a null body
            // depending on its contract with mobile clients.
            return Result<LoyaltyAccountSummaryDto?>.Ok(summary);
        }
    }
}
