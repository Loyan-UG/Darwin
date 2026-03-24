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
        public int Points { get; set; }
        public Guid? ReferenceId { get; set; }
        public Guid? UserId { get; set; }

        // Legacy aliases.
        public int PointsDelta
        {
            get => Points;
            set => Points = value;
        }

        public Guid? RewardRedemptionId
        {
            get => ReferenceId;
            set => ReferenceId = value;
        }

        public Guid? PerformedByUserId
        {
            get => UserId;
            set => UserId = value;
        }

        public Guid? BusinessLocationId { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }
}
