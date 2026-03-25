using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Ledger entry for points: accrual, redemption, or adjustment.
    /// </summary>
    public sealed class LoyaltyPointsTransaction : BaseEntity
    {
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public LoyaltyPointsTransactionType Type { get; set; } = LoyaltyPointsTransactionType.Accrual;
        public int PointsDelta { get; set; }
        public Guid? RewardRedemptionId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }

        // New-model aliases.
        public int Points
        {
            get => PointsDelta;
            set => PointsDelta = value;
        }

        public Guid? ReferenceId
        {
            get => RewardRedemptionId;
            set => RewardRedemptionId = value;
        }

        public Guid? UserId
        {
            get => PerformedByUserId;
            set => PerformedByUserId = value;
        }
    }
}
