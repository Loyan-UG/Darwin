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
        /// <summary>
        /// Gets or sets the loyalty account id that owns this transaction.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the business id that issued the transaction.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the transaction type.
        /// </summary>
        public LoyaltyPointsTransactionType Type { get; set; } = LoyaltyPointsTransactionType.Accrual;

        /// <summary>
        /// Gets or sets the signed points delta applied to the account balance.
        /// Positive values accrue points; negative values redeem or correct them.
        /// </summary>
        public int PointsDelta { get; set; }

        /// <summary>
        /// Gets or sets the optional linked redemption id when this transaction settles a reward.
        /// </summary>
        public Guid? RewardRedemptionId { get; set; }

        /// <summary>
        /// Gets or sets the optional business location id where the transaction occurred.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Gets or sets the optional operator user id that performed the transaction.
        /// </summary>
        public Guid? PerformedByUserId { get; set; }

        /// <summary>
        /// Gets or sets an optional external or operator-facing reference string.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Gets or sets optional internal notes.
        /// Avoid storing secrets or sensitive PII beyond approved audit requirements.
        /// </summary>
        public string? Notes { get; set; }
    }
}
