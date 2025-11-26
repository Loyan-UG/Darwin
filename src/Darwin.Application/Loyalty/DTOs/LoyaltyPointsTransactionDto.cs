using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Represents a single transaction entry in a loyalty account ledger
    /// from the perspective of the consumer.
    /// </summary>
    /// <remarks>
    /// The DTO is used by "My history" screens to show accruals, redemptions,
    /// and manual adjustments performed by staff.
    /// </remarks>
    public sealed class LoyaltyPointsTransactionDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the transaction.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the loyalty account that owns this transaction.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the type of the transaction (Accrual, Redemption, Adjustment).
        /// </summary>
        public LoyaltyPointsTransactionType Type { get; set; }

        /// <summary>
        /// Gets or sets the signed delta applied to the account balance.
        /// For example, +1 for accrual or -6 for redemption.
        /// </summary>
        public int PointsDelta { get; set; }

        /// <summary>
        /// Gets or sets the creation time of the transaction in UTC.
        /// This is taken from the base entity timestamp.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets an optional reference such as an order number or receipt identifier.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Gets or sets optional free-form notes that were stored with the transaction,
        /// typically written by staff for audit purposes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier of the business location where the
        /// transaction originated. This can be used to show branch-level history.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier of the reward redemption associated
        /// with this transaction when the transaction type is Redemption.
        /// </summary>
        public Guid? RewardRedemptionId { get; set; }
    }
}
