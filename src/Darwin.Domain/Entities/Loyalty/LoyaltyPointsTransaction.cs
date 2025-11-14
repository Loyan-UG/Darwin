using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Ledger entry for points: accrual (+), redemption (-), or adjustment (+/-).
    /// Serves as an auditable log for resolving disputes and analytics.
    /// </summary>
    public sealed class LoyaltyPointsTransaction : BaseEntity
    {
        /// <summary>
        /// Associated loyalty account.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Business context (duplicate of account's business for indexing/reporting convenience).
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Transaction type (Accrual, Redemption, Adjustment).
        /// </summary>
        public LoyaltyPointsTransactionType Type { get; set; } = LoyaltyPointsTransactionType.Accrual;

        /// <summary>
        /// Signed delta applied to the balance (e.g., +1 for accrual, -6 for redemption).
        /// </summary>
        public int PointsDelta { get; set; }

        /// <summary>
        /// Optional reference to a redemption record when Type = Redemption.
        /// </summary>
        public Guid? RewardRedemptionId { get; set; }

        /// <summary>
        /// Optional location context. Helps attribute activity to a branch.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Optional staff/user who performed the operation on the business device.
        /// </summary>
        public Guid? PerformedByUserId { get; set; }

        /// <summary>
        /// Optional opaque client reference (order number, receipt id).
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Optional notes for audit.
        /// </summary>
        public string? Notes { get; set; }
    }
}
