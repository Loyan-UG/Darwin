using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Represents a single scan session between a consumer device and a business device.
    /// The session is identified by its <see cref="BaseEntity.Id"/> and this identifier
    /// is the only value that needs to appear in the QR payload (ScanSessionToken).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consumer app prepares a scan session for a given business and mode
    /// (accrual or redemption). Optional reward selections are serialized into
    /// <see cref="SelectedRewardsJson"/> as a small JSON array.
    /// </para>
    /// <para>
    /// The business app scans the QR, resolves the session by <see cref="Id"/>,
    /// validates expiry and ownership, and then completes either an accrual or a
    /// redemption. After a successful completion the session becomes one-time
    /// use and cannot be reused to avoid replay attacks.
    /// </para>
    /// </remarks>
    public sealed class ScanSession : BaseEntity
    {
        /// <summary>
        /// The QR token that was presented by the consumer.
        /// Kept for audit linking to the ephemeral <see cref="QrCodeToken"/> entity,
        /// but does not need to be part of the public QR payload.
        /// </summary>
        public Guid QrCodeTokenId { get; set; }

        /// <summary>
        /// The loyalty account that this scan session operates on.
        /// This is resolved at session creation time to avoid extra lookups on scan.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Business processing this scan.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional location (branch) on which the scan took place.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Logical mode of the session (Accrual or Redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; set; } = LoyaltyScanMode.Accrual;

        /// <summary>
        /// Lifecycle status of the session (Pending, Completed, Cancelled, Expired).
        /// </summary>
        public LoyaltyScanStatus Status { get; set; } = LoyaltyScanStatus.Pending;

        /// <summary>
        /// Optional JSON payload describing selected rewards for redemption mode.
        /// The JSON format is controlled by the application layer and typically
        /// contains an array of { tierId, requiredPoints, quantity } objects.
        /// </summary>
        public string? SelectedRewardsJson { get; set; }

        /// <summary>
        /// UTC instant at which this session expires and can no longer be used.
        /// Short-lived (e.g., 2–5 minutes) to minimize replay risk.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Optional mobile device installation identifier that initiated the session.
        /// This can be used for device binding policies if needed.
        /// </summary>
        public string? CreatedByDeviceId { get; set; }

        /// <summary>
        /// Outcome of the scan (e.g., Accepted, Expired, Replayed, Rejected).
        /// Kept as a free-form field for analytics and debugging.
        /// </summary>
        public string Outcome { get; set; } = "Pending";

        /// <summary>
        /// Optional reason/details for failure when not accepted.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Optional reference to a resulting transaction (when Accepted and points
        /// were added or a reward was redeemed).
        /// </summary>
        public Guid? ResultingTransactionId { get; set; }
    }
}
