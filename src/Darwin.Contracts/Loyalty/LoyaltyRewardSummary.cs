using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Lightweight reward summary used by mobile apps when listing rewards
    /// that can be redeemed for a specific business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is intentionally independent from server-side entities and exposes
    /// only the information required by the mobile UI.
    /// </para>
    /// <para>
    /// All string properties are configured to be non-null by default to avoid
    /// null-handling overhead in clients.
    /// </para>
    /// </remarks>
    public sealed class LoyaltyRewardSummary
    {
        /// <summary>
        /// Gets the reward tier identifier.
        /// </summary>
        public Guid LoyaltyRewardTierId { get; init; }

        /// <summary>
        /// Gets the business identifier to which this reward belongs.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets the reward name/title. Never null.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets an optional reward description.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the amount of points required to redeem this reward.
        /// </summary>
        public int RequiredPoints { get; init; }

        /// <summary>
        /// Gets whether the underlying loyalty program/tier is active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets whether redeeming this reward requires explicit confirmation
        /// (for example by cashier approval).
        /// </summary>
        public bool RequiresConfirmation { get; init; }

        /// <summary>
        /// Gets whether this reward is currently selectable for the requesting user
        /// (based on balance/account status, as computed by the server).
        /// </summary>
        public bool IsSelectable { get; init; }
    }
}
