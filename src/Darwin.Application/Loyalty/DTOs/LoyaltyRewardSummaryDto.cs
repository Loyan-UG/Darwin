using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Read model representing a single loyalty reward tier as exposed to
    /// consumer-facing applications. This DTO is intentionally aligned with
    /// the corresponding contract type used by the WebApi layer so that
    /// mapping between Application and Contracts remains straightforward.
    /// </summary>
    public sealed class LoyaltyRewardSummaryDto
    {
        /// <summary>
        /// Gets or sets the identifier of the loyalty reward tier.
        /// </summary>
        public Guid LoyaltyRewardTierId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the business that owns the reward.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the human-friendly name of the reward as shown to customers.
        /// </summary>
        /// <remarks>
        /// At the time of writing, reward tiers in the domain model do not expose a
        /// dedicated name field. For MVP implementations, this property is populated
        /// from the tier's description or, as a fallback, from the parent program's
        /// name. Once the domain model grows a dedicated title/name field for tiers,
        /// this mapping should be revisited accordingly.
        /// </remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional textual description of the reward. This is
        /// typically a user-facing phrase such as "Free drink" or "10% discount".
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the number of points required to unlock or redeem the reward.
        /// </summary>
        public int RequiredPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the underlying loyalty program
        /// is currently active. Individual tiers currently do not have an explicit
        /// active flag; if this changes, the mapping logic should be updated.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reward is currently selectable
        /// for the requesting consumer, based on the account status and points balance.
        /// </summary>
        public bool IsSelectable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reward requires explicit
        /// staff confirmation at redemption time.
        /// </summary>
        /// <remarks>
        /// This is typically the inverse of <c>AllowSelfRedemption</c> in the domain
        /// tier entity; high-value or sensitive rewards should require confirmation.
        /// </remarks>
        public bool RequiresConfirmation { get; set; }
    }
}
