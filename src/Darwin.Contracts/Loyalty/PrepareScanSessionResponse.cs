using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload returned after preparing a scan session
    /// on the consumer device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ScanSessionToken"/> value is encoded into the QR code
    /// and represents the only information that needs to be present in the
    /// QR payload displayed by the consumer app.
    /// </para>
    /// </remarks>
    public sealed class PrepareScanSessionResponse
    {
        /// <summary>
        /// Gets or sets the opaque scan session token that should be encoded
        /// into the QR code. This must be treated as a short-lived secret and
        /// never logged in plaintext.
        /// </summary>
        public string ScanSessionToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets the mode of the scan session (accrual or redemption) as
        /// persisted on the backend.
        /// </summary>
        public LoyaltyScanMode Mode { get; init; }

        /// <summary>
        /// Gets the absolute UTC timestamp at which this scan session expires
        /// and the QR code should no longer be accepted by the business app.
        /// </summary>
        public DateTime ExpiresAtUtc { get; init; }

        /// <summary>
        /// Gets the current points balance of the consumer for this business
        /// at the time the scan session was created.
        /// </summary>
        public int CurrentPointsBalance { get; init; }

        /// <summary>
        /// Gets the list of rewards that were selected for redemption at the
        /// time the session was created. This list is primarily used to show
        /// a confirmation to the consumer in the UI.
        /// </summary>
        public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; }
            = Array.Empty<LoyaltyRewardSummary>();
    }

}
