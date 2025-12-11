using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload used by the business app to confirm the
    /// redemption of rewards for a scan session.
    /// </summary>
    public sealed class ConfirmRedemptionRequest
    {
        /// <summary>
        /// Gets the identifier of the scan session to confirm.
        /// </summary>
        public string ScanSessionToken { get; set; } = string.Empty;
    }
}
