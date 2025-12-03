using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload returned after attempting to confirm an
    /// accrual operation for a scan session.
    /// </summary>
    public sealed class ConfirmAccrualResponse
    {
        /// <summary>
        /// Gets a value indicating whether the accrual operation
        /// was successfully applied.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the new points balance for the loyalty account after
        /// the accrual has been applied, if available.
        /// </summary>
        public int? NewBalance { get; init; }

        /// <summary>
        /// Gets an optional updated snapshot of the loyalty account after
        /// the accrual operation has been processed. New clients should
        /// prefer this property over <see cref="NewBalance"/> as it provides
        /// richer context (business name, timestamps, next reward, etc.).
        /// When <c>null</c>, clients can fall back to <see cref="NewBalance"/>.
        /// </summary>
        public LoyaltyAccountSummary? UpdatedAccount { get; init; }

        /// <summary>
        /// Gets a machine-readable error code in case the operation
        /// failed (for example, "SESSION_EXPIRED" or "ACCOUNT_LOCKED").
        /// </summary>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Gets a human-readable error message that can be displayed
        /// to the cashier or logged for diagnostics.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }
}
