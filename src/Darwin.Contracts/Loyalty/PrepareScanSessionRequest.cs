using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload used by the consumer app to prepare a new
    /// loyalty scan session for a given business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The resulting scan session identifier is encoded into the QR code
    /// displayed on the consumer device and later scanned by the business app.
    /// </para>
    /// </remarks>
    public sealed class PrepareScanSessionRequest
    {
        /// <summary>
        /// Gets the identifier of the business for which the scan session
        /// is being created.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets the identifier of the specific business location (branch)
        /// if the consumer explicitly selected one, or <c>null</c> if the
        /// location is inferred by the backend.
        /// </summary>
        public Guid? BusinessLocationId { get; init; }

        /// <summary>
        /// Gets the intended mode of the scan session (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; init; } = LoyaltyScanMode.Accrual;

        /// <summary>
        /// Gets the list of reward tier identifiers that the consumer selected
        /// for redemption when the mode is <see cref="LoyaltyScanMode.Redemption"/>.
        /// This list is typically empty when the mode is accrual.
        /// </summary>
        public IReadOnlyList<Guid> SelectedRewardTierIds { get; init; }
            = Array.Empty<Guid>();

        /// <summary>
        /// Gets an optional device identifier used by the backend for
        /// diagnostics, fraud detection or analytics.
        /// </summary>
        public string? DeviceId { get; init; }
    }
}
