using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Customer account for a given business' loyalty program. Holds point balances and lifecycle status.
    /// Never expose internal UserId in QR or external channels; use external tokens for presentation.
    /// </summary>
    public sealed class LoyaltyAccount : BaseEntity
    {
        /// <summary>
        /// Business the account belongs to (one account per business per user).
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Owning user (consumer) inside the platform.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Logical status of the account (e.g., Active, Suspended).
        /// </summary>
        public LoyaltyAccountStatus Status { get; set; } = LoyaltyAccountStatus.Active;

        /// <summary>
        /// Current spendable point balance. Keep integer to avoid decimals for points.
        /// </summary>
        public int PointsBalance { get; set; } = 0;

        /// <summary>
        /// Cumulative points ever earned. Useful for analytics and tier progress.
        /// </summary>
        public int LifetimePoints { get; set; } = 0;

        /// <summary>
        /// Last accrual timestamp in UTC for displaying "Last visit".
        /// </summary>
        public DateTime? LastAccrualAtUtc { get; set; }

        /// <summary>
        /// Navigation: transaction ledger (accrual, redemption, adjustments).
        /// </summary>
        public List<LoyaltyPointsTransaction> Transactions { get; set; } = new();

        /// <summary>
        /// Navigation: redemption history entries.
        /// </summary>
        public List<LoyaltyRewardRedemption> Redemptions { get; set; } = new();
    }
}
