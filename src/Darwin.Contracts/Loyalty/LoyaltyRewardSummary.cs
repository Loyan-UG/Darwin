using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Lightweight reward summary used by mobile apps when listing
    /// rewards that can be redeemed for a specific business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is intentionally independent from the full loyalty
    /// reward tier entity used on the server. It exposes only the
    /// information required by the mobile UI.
    /// </para>
    /// </remarks>
    public sealed class LoyaltyRewardSummary
    {
        /// <summary>
        /// Gets the identifier of the reward tier that can be redeemed.
        /// </summary>
        public Guid LoyaltyRewardTierId { get; init; }

        /// <summary>
        /// Gets the identifier of the business that owns this reward.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets the human-readable reward name shown to the consumer
        /// (for example, "Free Coffee" or "20% Off").
        /// </summary>
        public string Name { get; init; } = default!;

        /// <summary>
        /// Gets an optional longer description that explains the reward
        /// in more detail (terms and conditions, limitations, etc.).
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the number of points required to redeem a single unit
        /// of this reward.
        /// </summary>
        public int RequiredPoints { get; init; }

        /// <summary>
        /// Gets a value indicating whether this reward is currently active
        /// and visible in the loyalty program of the business.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets a value indicating whether this reward is selectable in the
        /// consumer app for the current context (for example, the consumer
        /// already has enough points and the reward is not blocked).
        /// </summary>
        public bool IsSelectable { get; init; }
    }
}
