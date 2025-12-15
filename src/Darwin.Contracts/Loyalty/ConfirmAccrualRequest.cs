using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload used by the business app to confirm an accrual
    /// (earning points) for an existing scan session.
    /// </summary>
    /// <remarks>
    /// The scan session is identified exclusively by an opaque, short-lived token.
    /// Clients must not attempt to parse or infer any internal identifiers from it.
    /// </remarks>
    public sealed class ConfirmAccrualRequest
    {
        /// <summary>
        /// Gets the opaque scan session token to confirm.
        /// This is the only identifier allowed outside the server boundary.
        /// </summary>
        public string ScanSessionToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets the number of points to add to the loyalty account for this accrual.
        /// </summary>
        public int Points { get; init; } = 1;

        /// <summary>
        /// Gets an optional note/reference for audit, troubleshooting, or POS integration.
        /// </summary>
        public string? Note { get; init; }
    }
}
