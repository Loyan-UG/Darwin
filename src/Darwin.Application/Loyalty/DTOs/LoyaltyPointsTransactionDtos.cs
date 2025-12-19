using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO for creating points transactions (system flow).
    /// </summary>
    public sealed class LoyaltyPointsTransactionCreateDto
    {
        /// <summary>
        /// Gets or sets the loyalty account identifier.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }

        public Guid? BusinessUserId { get; set; }
        public Guid ConsumerUserId { get; set; }

        /// <summary>
        /// Gets or sets the points delta (positive for accrual, negative for redemption).
        /// </summary>
        public int PointsDelta { get; set; }
        /// <summary>
        /// Gets or sets an optional human-readable note describing the transaction.
        /// </summary>
        public string? Note { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        //public Guid? ScanSessionId { get; set; }
        public Guid? RewardTierId { get; set; }
    }

    /// <summary>
    /// Lightweight list item for grids.
    ///
    /// IMPORTANT (Token-First Rule):
    /// This DTO must never expose internal scan session identifiers.
    /// </summary>
    public sealed class LoyaltyPointsTransactionListItemDto
    {
        /// <summary>
        /// Gets or sets the transaction identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the loyalty account identifier.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the business identifier.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the business user identifier (staff user) associated with the transaction, if any.
        /// </summary>
        public Guid? BusinessUserId { get; set; }

        /// <summary>
        /// Gets or sets the consumer user identifier associated with the account/transaction.
        /// </summary>
        public Guid ConsumerUserId { get; set; }

        /// <summary>
        /// Gets or sets the points delta (positive for accrual, negative for redemption).
        /// </summary>
        public int PointsDelta { get; set; }

        /// <summary>
        /// Gets or sets an optional human-readable note.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the logical occurrence timestamp (UTC) of the transaction.
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional reward tier identifier that caused this transaction (when applicable).
        /// This is a public domain identifier and is safe to expose.
        /// </summary>
        public Guid? RewardTierId { get; set; }
    }
}
