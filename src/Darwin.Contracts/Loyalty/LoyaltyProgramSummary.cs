using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Program overview displayed on business profile or details screens.
    /// </summary>
    /// <remarks>
    /// This contract is designed for mobile UI consumption and therefore keeps
    /// payload size small while preserving non-null defaults where appropriate.
    /// </remarks>
    public sealed class LoyaltyProgramSummary
    {
        /// <summary>
        /// Gets the identifier of the loyalty program.
        /// </summary>
        public Guid LoyaltyProgramId { get; init; }

        /// <summary>
        /// Gets the program name. Never null.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the accrual mode string used by clients to render labels or logic.
        /// Defaults to <c>PerVisit</c> for backward-compatible behavior.
        /// </summary>
        public string AccrualMode { get; init; } = "PerVisit";

        /// <summary>
        /// Gets the reward tiers defined for this program. Never null.
        /// </summary>
        public IReadOnlyList<LoyaltyRewardTier> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTier>();
    }
}
