using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Loyalty
{
    /// <summary>
    /// Represents a single scan attempt on the business device.
    /// Provides additional auditability around the QR token consumption, device, and outcome.
    /// </summary>
    public sealed class ScanSession : BaseEntity
    {
        /// <summary>
        /// The QR token that was presented by the consumer.
        /// </summary>
        public Guid QrCodeTokenId { get; set; }

        /// <summary>
        /// Business processing this scan.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional location (branch) on which the scan took place.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Outcome of the scan (e.g., Accepted, Expired, Replayed, Rejected).
        /// </summary>
        public string Outcome { get; set; } = "Accepted";

        /// <summary>
        /// Optional reason/details for failure when not accepted.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Optional reference to a resulting transaction (when Accepted and points were added or a reward was redeemed).
        /// </summary>
        public Guid? ResultingTransactionId { get; set; }
    }
}
