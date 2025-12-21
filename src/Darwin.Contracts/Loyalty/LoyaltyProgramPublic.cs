using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Public-facing representation of a loyalty program for consumer/mobile screens.
    /// This model intentionally avoids internal server details and is safe to expose publicly.
    /// </summary>
    public sealed class LoyaltyProgramPublic
    {
        /// <summary>
        /// Gets or sets the loyalty program identifier.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets or sets the business identifier that owns this program.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets or sets the display name of the loyalty program.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the program is currently active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets or sets the ordered list of reward tiers available for this program.
        /// </summary>
        public IReadOnlyList<LoyaltyRewardTierPublic> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTierPublic>();
    }
}
