using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Business-facing view of a scan session after the QR code
    /// has been processed by the backend.
    /// </summary>
    public sealed class ProcessScanSessionForBusinessResponse
    {
        /// <summary>
        /// Gets the mode of the scan session (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; init; }

        /// <summary>
        /// Gets the identifier of the business that owns the loyalty account.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets the identifier of the specific business location (branch),
        /// if applicable.
        /// </summary>
        public Guid? BusinessLocationId { get; init; }

        /// <summary>
        /// Gets or sets the loyalty account snapshot for this session.
        /// </summary>
        public BusinessLoyaltyAccountSummary AccountSummary { get; set; }
            = new BusinessLoyaltyAccountSummary();

        /// <summary>
        /// Gets an optional human-friendly alias or display name that can be
        /// used by the cashier to confirm the customer's identity verbally.
        /// This should not contain sensitive personal information.
        /// </summary>
        public string? CustomerDisplayName { get; init; }

        /// <summary>
        /// Gets the list of rewards that the consumer selected for redemption
        /// in this scan session. The list is empty when the session is in
        /// accrual mode.
        /// </summary>
        public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; }
            = Array.Empty<LoyaltyRewardSummary>();

        /// <summary>
        /// Gets the set of actions that the business is allowed to perform
        /// for this scan session (for example, confirm accrual or redemption).
        /// </summary>
        public LoyaltyScanAllowedActions AllowedActions { get; init; }
    }
}
