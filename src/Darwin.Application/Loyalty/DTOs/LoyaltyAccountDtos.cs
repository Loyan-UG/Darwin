using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Read model for a loyalty account used in mobile/consumer screens.
    /// </summary>
    public sealed class LoyaltyAccountDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public LoyaltyAccountStatus Status { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimePoints { get; set; }
        public DateTime? LastAccrualAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Request DTO to get or create (if missing) an account for a user in a business context.
    /// </summary>
    public sealed class GetOrCreateLoyaltyAccountDto
    {
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
    }



    /// <summary>
    /// DTO used by business/staff to adjust a customer's loyalty points.
    /// This is used to correct mistakes or apply manual bonuses outside
    /// the automatic accrual/redemption flow.
    /// </summary>
    public sealed class AdjustLoyaltyPointsDto
    {
        /// <summary>
        /// Identifier of the loyalty account whose balance should be adjusted.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Business to which the account belongs. Used as a safety check to
        /// prevent cross-business modifications.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional business location where the adjustment is performed.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Optional staff or business user performing the adjustment.
        /// </summary>
        public Guid? PerformedByUserId { get; set; }

        /// <summary>
        /// Signed amount of points to apply. Positive values add points,
        /// negative values subtract points. Zero is not allowed.
        /// </summary>
        public int PointsDelta { get; set; }

        /// <summary>
        /// Optional free-form reason for the adjustment. 
        /// Recommended to be set for auditability, and required when the
        /// adjustment is negative.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Optional client-side reference (e.g., receipt number, incident id).
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Optional concurrency token. When provided, the handler will enforce
        /// optimistic concurrency on the loyalty account row.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Result DTO returned after a successful points adjustment. 
    /// Provides the new balances and the identifier of the created ledger entry.
    /// </summary>
    public sealed class AdjustLoyaltyPointsResultDto
    {
        /// <summary>
        /// Identifier of the adjusted loyalty account.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Identifier of the adjustment transaction recorded in the ledger.
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// New spendable points balance after the adjustment.
        /// </summary>
        public int NewPointsBalance { get; set; }

        /// <summary>
        /// New lifetime points value after the adjustment. 
        /// Depending on business rules, the lifetime value may or may not be
        /// decremented for negative adjustments.
        /// </summary>
        public int NewLifetimePoints { get; set; }
    }


    /// <summary>
    /// DTO used to suspend a loyalty account, preventing further accrual and redemption.
    /// </summary>
    public sealed class SuspendLoyaltyAccountDto
    {
        /// <summary>
        /// Identifier of the loyalty account to suspend.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Optional concurrency token. When provided, the handler will enforce
        /// optimistic concurrency for the status change.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// DTO used to reactivate a previously suspended loyalty account.
    /// </summary>
    public sealed class ActivateLoyaltyAccountDto
    {
        /// <summary>
        /// Identifier of the loyalty account to activate.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Optional concurrency token. When provided, the handler will enforce
        /// optimistic concurrency for the status change.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
