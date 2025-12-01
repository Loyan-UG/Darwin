using System;
using System.Collections.Generic;
using Darwin.Contracts.Loyalty;

namespace Darwin.Mobile.Shared.Models.Loyalty
{
    /// <summary>
    /// Client-side projection of a prepared scan session for the consumer app.
    /// This contains everything needed to render the QR and basic context.
    /// </summary>
    public sealed class ScanSessionClientModel
    {
        /// <summary>
        /// Gets the opaque token that should be rendered as a QR code.
        /// This value must not be an internal user identifier.
        /// </summary>
        public string Token { get; init; } = string.Empty;

        /// <summary>
        /// Gets the UTC expiry of the session, if provided by the server.
        /// The UI can use this to show a countdown or trigger refresh.
        /// </summary>
        public DateTimeOffset? ExpiresAtUtc { get; init; }

        /// <summary>
        /// Gets the scan mode for this session (e.g. accrual or redemption).
        /// This is typically mapped from a server-side enum.
        /// </summary>
        public LoyaltyScanMode Mode { get; init; }

        /// <summary>
        /// Gets the set of rewards that were selected when this session
        /// was prepared (for redemption scenarios).
        /// </summary>
        public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; } =
            Array.Empty<LoyaltyRewardSummary>();
    }

    /// <summary>
    /// Client-side projection of a scan session as seen by the business app
    /// after scanning a consumer QR code.
    /// </summary>
    public sealed class BusinessScanSessionClientModel
    {
        /// <summary>
        /// Gets the opaque token that identifies the scan session from the
        /// perspective of the mobile apps. The actual internal session id
        /// remains on the server.
        /// </summary>
        public string Token { get; init; } = string.Empty;

        /// <summary>
        /// Gets the scan mode for this session (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; init; }

        /// <summary>
        /// Gets the business identifier associated with this session.
        /// </summary>
        public Guid BusinessId { get; init; }

        /// <summary>
        /// Gets a lightweight account summary (points balance, etc.).
        /// It must not contain PII; only loyalty-related data is exposed.
        /// </summary>
        public LoyaltyAccountSummary? AccountSummary { get; init; }

        /// <summary>
        /// Gets the rewards that the consumer pre-selected for redemption
        /// as part of this scan session. Empty for accrual sessions.
        /// </summary>
        public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; } =
            Array.Empty<LoyaltyRewardSummary>();

        /// <summary>
        /// Gets a value indicating whether the business is allowed to confirm
        /// accrual for this session according to server-side policy.
        /// </summary>
        public bool CanConfirmAccrual { get; init; }

        /// <summary>
        /// Gets a value indicating whether the business is allowed to confirm
        /// redemption for this session according to server-side policy.
        /// </summary>
        public bool CanConfirmRedemption { get; init; }
    }
}
