using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Public-facing representation of a loyalty reward tier.
    /// </summary>
    public sealed class LoyaltyRewardTierPublic
    {
        /// <summary>
        /// Gets or sets the reward tier identifier.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets or sets the number of points required to unlock/redeem this tier.
        /// </summary>
        public int PointsRequired { get; init; }

        /// <summary>
        /// Gets or sets the reward type as a string to keep the contract stable and decoupled from Domain enums.
        /// </summary>
        /// <remarks>
        /// WebApi maps the server-side enum to its name (e.g. "FreeItem", "PercentDiscount", "AmountDiscount").
        /// </remarks>
        public string RewardType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional numeric value for this reward type (e.g., 20 for 20% discount).
        /// </summary>
        public decimal? RewardValue { get; init; }

        /// <summary>
        /// Gets or sets the optional textual description shown to customers (e.g., "Free coffee").
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the reward may be redeemed without staff confirmation.
        /// </summary>
        public bool AllowSelfRedemption { get; init; }
    }
}
