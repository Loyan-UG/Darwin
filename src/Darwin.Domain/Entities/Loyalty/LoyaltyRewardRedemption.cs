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
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid LoyaltyRewardTierId { get; set; }
        public int PointsSpent { get; set; }
        public LoyaltyRedemptionStatus Status { get; set; } = LoyaltyRedemptionStatus.Pending;
        public Guid? BusinessLocationId { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime? RedeemedAtUtc { get; set; }

        // New-model aliases.
        public Guid TierId
        {
            get => LoyaltyRewardTierId;
            set => LoyaltyRewardTierId = value;
        }

        public Guid? LocationId
        {
            get => BusinessLocationId;
            set => BusinessLocationId = value;
        }
    }
}
