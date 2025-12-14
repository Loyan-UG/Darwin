using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Consumer-facing summary model used by mobile applications to display
    /// a lightweight snapshot of a loyalty account for a specific business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This contract intentionally avoids exposing internal server identifiers
    /// (for example, the loyalty account entity id) and focuses on fields that
    /// are directly required by the UI.
    /// </para>
    /// <para>
    /// All string properties are guaranteed to be non-null to keep client-side
    /// code null-safe and predictable.
    /// </para>
    /// </remarks>
    public sealed class LoyaltyAccountSummary
    {
        /// <summary>
        /// Gets the identifier of the business that owns this loyalty account.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets the human-friendly business name. Never null.
        /// </summary>
        public string BusinessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the current spendable points balance for the account.
        /// </summary>
        public int PointsBalance { get; init; }

        /// <summary>
        /// Gets the UTC timestamp of the last accrual for this account, if available.
        /// </summary>
        public DateTime? LastAccrualAtUtc { get; init; }

        /// <summary>
        /// Gets an optional title/label of the next reward the user might reach.
        /// </summary>
        public string? NextRewardTitle { get; init; }
    }
}
