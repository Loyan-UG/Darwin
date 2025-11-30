using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload returned after attempting to confirm
    /// the redemption operation for a scan session.
    /// </summary>
    public sealed class ConfirmRedemptionResponse
    {
        /// <summary>
        /// Gets a value indicating whether the redemption operation
        /// was successfully applied.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the new points balance for the loyalty account after
        /// the redemption has been applied, if available.
        /// </summary>
        public int? NewBalance { get; init; }

        /// <summary>
        /// Gets a machine-readable error code in case the operation
        /// failed (for example, "SESSION_EXPIRED" or "INSUFFICIENT_POINTS").
        /// </summary>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Gets a human-readable error message that can be displayed
        /// to the cashier or logged for diagnostics.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }
}
