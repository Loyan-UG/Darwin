using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Query handler that returns the list of loyalty rewards available for
    /// the current consumer user in the context of a specific business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is used by consumer-facing applications to populate the
    /// "available rewards" screen before a scan session is prepared. It combines
    /// the loyalty program configuration for the given business with the current
    /// user's loyalty account balance in order to determine which rewards are
    /// selectable.
    /// </para>
    /// <para>
    /// The handler does not modify any state. It only reads the active loyalty
    /// program and its tiers together with the user's account (if present) and
    /// projects them into <see cref="LoyaltyRewardSummaryDto"/> instances. The
    /// result is designed to map 1:1 to the corresponding WebApi contract type.
    /// </para>
    /// </remarks>
    public sealed class GetAvailableLoyaltyRewardsForBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="GetAvailableLoyaltyRewardsForBusinessHandler"/> class.
        /// </summary>
        /// <param name="db">
        /// Application database abstraction used to query loyalty programs,
        /// reward tiers, and loyalty accounts.
        /// </param>
        /// <param name="currentUserService">
        /// Service providing the identifier of the current authenticated user.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any dependency is <c>null</c>.
        /// </exception>
        public GetAvailableLoyaltyRewardsForBusinessHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Retrieves the list of loyalty rewards defined for the specified business
        /// and marks which of them are currently selectable by the calling user.
        /// </summary>
        /// <param name="businessId">
        /// The identifier of the business whose loyalty rewards should be queried.
        /// </param>
        /// <param name="ct">
        /// An optional cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing a read-only list of
        /// <see cref="LoyaltyRewardSummaryDto"/> on success, or an error message
        /// on failure.
        /// </returns>
        public async Task<Result<IReadOnlyList<LoyaltyRewardSummaryDto>>> HandleAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummaryDto>>.Fail("BusinessId is required.");
            }

            var userId = _currentUserService.GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                // Defensive check; in typical WebApi flows the current user is always
                // authenticated for consumer endpoints.
                return Result<IReadOnlyList<LoyaltyRewardSummaryDto>>.Fail("Unauthorized.");
            }

            // Load the active loyalty program for the specified business. If there is
            // no active program, we return an empty list rather than an error so that
            // the client can simply display "no rewards available".
            var program = await _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.BusinessId == businessId && !x.IsDeleted && x.IsActive,
                    ct)
                .ConfigureAwait(false);

            if (program is null)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummaryDto>>.Ok(
                    Array.Empty<LoyaltyRewardSummaryDto>());
            }

            // Load the loyalty account for the current user and business, if any.
            // The account may not exist yet; in that case we treat the balance as
            // zero and the account as inactive when computing IsSelectable.
            var account = await _db.Set<LoyaltyAccount>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    a => a.BusinessId == businessId && a.UserId == userId && !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            var pointsBalance = account?.PointsBalance ?? 0;
            var isAccountActive = account is not null &&
                                  account.Status == LoyaltyAccountStatus.Active;

            // Load all non-deleted reward tiers for the program. Ordering by
            // PointsRequired ensures a stable and intuitive display order.
            var tiers = await _db.Set<LoyaltyRewardTier>()
                .AsNoTracking()
                .Where(t => t.LoyaltyProgramId == program.Id && !t.IsDeleted)
                .OrderBy(t => t.PointsRequired)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (tiers.Count == 0)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummaryDto>>.Ok(
                    Array.Empty<LoyaltyRewardSummaryDto>());
            }

            var results = new List<LoyaltyRewardSummaryDto>(tiers.Count);

            foreach (var tier in tiers)
            {
                // Derive a human-friendly name. Until the domain model exposes a
                // dedicated title field for tiers, we fall back to the description
                // or, if that is not available, to the program's name.
                var name = !string.IsNullOrWhiteSpace(tier.Description)
                    ? tier.Description
                    : program.Name;

                // A reward is selectable only when the account is active and the
                // current balance is sufficient to cover the required points.
                var isSelectable = isAccountActive && pointsBalance >= tier.PointsRequired;

                var dto = new LoyaltyRewardSummaryDto
                {
                    LoyaltyRewardTierId = tier.Id,
                    BusinessId = program.BusinessId,
                    Name = name,
                    Description = tier.Description,
                    RequiredPoints = tier.PointsRequired,
                    IsActive = program.IsActive,
                    RequiresConfirmation = !tier.AllowSelfRedemption,
                    IsSelectable = isSelectable
                };

                results.Add(dto);
            }

            return Result<IReadOnlyList<LoyaltyRewardSummaryDto>>.Ok(results);
        }
    }
}
