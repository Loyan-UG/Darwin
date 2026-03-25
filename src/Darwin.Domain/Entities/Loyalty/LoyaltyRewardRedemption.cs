using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Represents a single redemption of a reward tier.
    /// </summary>
    public sealed class LoyaltyRewardRedemption : BaseEntity
    {
        /// <summary>
        /// Gets or sets the loyalty account id that redeemed the reward.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the business id that owns the loyalty program.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the redeemed reward tier id.
        /// </summary>
        public Guid LoyaltyRewardTierId { get; set; }

        /// <summary>
        /// Gets or sets the number of points spent for the redemption.
        /// </summary>
        public int PointsSpent { get; set; }

        /// <summary>
        /// Gets or sets the redemption lifecycle status.
        /// </summary>
        public LoyaltyRedemptionStatus Status { get; set; } = LoyaltyRedemptionStatus.Pending;

        /// <summary>
        /// Gets or sets the optional business location id where the redemption was fulfilled.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Gets or sets optional metadata in JSON form.
        /// Do not place secrets in this field.
        /// </summary>
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the redemption completed.
        /// </summary>
        public DateTime? RedeemedAtUtc { get; set; }
    }
}
