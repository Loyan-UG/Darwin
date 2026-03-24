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
        public Guid TierId { get; set; }
        public int PointsSpent { get; set; }
        public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;
        public Guid? LocationId { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime? RedeemedAtUtc { get; set; }

        // Legacy aliases.
        public Guid LoyaltyRewardTierId
        {
            get => TierId;
            set => TierId = value;
        }

        public Guid? BusinessLocationId
        {
            get => LocationId;
            set => LocationId = value;
        }
    }
}
