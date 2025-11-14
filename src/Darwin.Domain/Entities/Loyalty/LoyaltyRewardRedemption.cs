using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Represents a single redemption of a reward tier. Points are consumed when the redemption is confirmed.
    /// </summary>
    public sealed class LoyaltyRewardRedemption : BaseEntity
    {
        /// <summary>
        /// Loyalty account that consumes the reward.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Business context for indexing/reporting.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// The program tier that was redeemed (snapshot of rules may be needed in MetadataJson).
        /// </summary>
        public Guid LoyaltyRewardTierId { get; set; }

        /// <summary>
        /// Points consumed for this redemption.
        /// </summary>
        public int PointsSpent { get; set; }

        /// <summary>
        /// Current status in the redemption lifecycle (Pending -> Confirmed/Cancelled).
        /// </summary>
        public LoyaltyRedemptionStatus Status { get; set; } = LoyaltyRedemptionStatus.Pending;

        /// <summary>
        /// Optional location where the redemption was performed.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Optional JSON snapshot of reward parameters at time of redemption (for audit).
        /// </summary>
        public string? MetadataJson { get; set; }
    }
}
