using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;

namespace Darwin.WebApi.Services
{
    /// <summary>
    /// Presentation-focused helper for loyalty-related UI shaping tasks.
    /// Responsibilities:
    /// - Enrich a set of selected reward tier ids with public reward metadata
    ///   (name, description, required points, flags) by joining against the
    ///   business's available rewards.
    /// - Provide a short-term cache for available rewards to reduce load on
    ///   the Application layer and DB for hot paths (mobile -> server).
    /// - Surface a strict vs tolerant policy: callers may request fail-fast
    ///   behavior when missing reward ids are considered critical.
    /// 
    /// Note: This service is intentionally part of the WebApi layer since it
    /// shapes contract types (Darwin.Contracts) and concerns presentation.
    /// </summary>
    public interface ILoyaltyPresentationService
    {
        /// <summary>
        /// Enriches the requested reward tier ids for a business and returns
        /// a Result containing the mapped contract list on success.
        /// 
        /// If <paramref name="failIfMissing"/> is true and any requested tier id
        /// cannot be found among available rewards, the returned Result is a Fail.
        /// If <paramref name="failIfMissing"/> is false, a best-effort list is returned.
        /// </summary>
        Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> EnrichSelectedRewardsAsync(
            Guid businessId,
            IReadOnlyCollection<Guid>? selectedTierIds,
            bool failIfMissing,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the available rewards for the business (from cache when possible).
        /// This returns a failure Result if the underlying query failed.
        /// </summary>
        Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default);
    }
}