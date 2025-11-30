using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload used by the business app to confirm an
    /// accrual (earning points) for a scan session.
    /// </summary>
    public sealed class ConfirmAccrualRequest
    {
        /// <summary>
        /// Gets the identifier of the scan session to confirm.
        /// </summary>
        public Guid ScanSessionId { get; init; }

        /// <summary>
        /// Gets the number of points to add to the loyalty account
        /// for this accrual operation.
        /// </summary>
        public int Points { get; init; } = 1;

        /// <summary>
        /// Gets an optional note or reference that can be used for
        /// audit, troubleshooting or integration with external systems.
        /// </summary>
        public string? Note { get; init; }
    }
}
