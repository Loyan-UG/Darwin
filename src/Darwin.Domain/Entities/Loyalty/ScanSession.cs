using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Represents a single scan session between a consumer device and a business device.
    /// This entity is an internal record and MUST NOT be identified or referenced by its
    /// <see cref="BaseEntity.Id"/> outside the Application/Infrastructure boundary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consumer app prepares a scan session for a given business and mode (accrual or redemption).
    /// The only value that is allowed to appear in the QR payload is the opaque <see cref="QrCodeToken.Token"/>
    /// (also referred to as the scan session token in contracts).
    /// </para>
    /// <para>
    /// The business app scans the QR token and the Application layer resolves that token to the underlying
    /// <see cref="QrCodeToken"/> and <see cref="ScanSession"/> records. The resolution validates expiry,
    /// one-time use consumption, business binding, and scan session state.
    /// </para>
    /// <para>
    /// After a successful completion the session becomes one-time use and cannot be reused to reduce replay risk.
    /// </para>
    /// </remarks>
    public sealed class ScanSession : BaseEntity
    {
        /// <summary>
        /// Foreign key linking this session to the ephemeral <see cref="QrCodeToken"/> record.
        /// The QR payload contains ONLY <see cref="QrCodeToken.Token"/>; this identifier is internal
        /// and exists for correlation, auditing and server-side resolution.
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
