using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload sent by the business app after scanning the QR code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ScanSessionId"/> is the value that was encoded into the
    /// QR code on the consumer device.
    /// </para>
    /// </remarks>
    public sealed class ProcessScanSessionForBusinessRequest
    {
        /// <summary>
        /// Gets the identifier of the scan session that was obtained from
        /// the scanned QR code.
        /// </summary>
        public Guid ScanSessionId { get; init; }
    }
}
