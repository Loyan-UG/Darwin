using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Represents a single reward selection inside a scan session.
    /// </summary>
    public sealed class SelectedRewardItemDto
    {
        /// <summary>
        /// Gets or sets the identifier of the reward tier selected for redemption.
        /// </summary>
        public Guid LoyaltyRewardTierId { get; set; }

        /// <summary>
        /// Gets or sets the number of times this reward tier is requested.
        /// Defaults to 1 for simple coffee-stamp style programs.
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of points required for a single unit
        /// of this reward tier at the time of selection.
        /// This is stored in the session for replay-safe validation.
        /// </summary>
        public int RequiredPointsPerUnit { get; set; }
    }

    /// <summary>
    /// Input data for preparing a new scan session on the consumer device.
    /// </summary>
    public sealed class PrepareScanSessionDto
    {
        /// <summary>
        /// Gets or sets the identifier of the business for which the session
        /// is being created.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the location identifier if the consumer explicitly
        /// selected a branch (optional).
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Gets or sets the scan mode (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; set; } = LoyaltyScanMode.Accrual;

        /// <summary>
        /// Gets or sets an optional list of reward tier identifiers that the
        /// consumer selected for redemption.
        /// Only used when <see cref="Mode"/> is <see cref="LoyaltyScanMode.Redemption"/>.
        /// </summary>
        public List<Guid> SelectedRewardTierIds { get; set; } = new();

        /// <summary>
        /// Gets or sets an optional device installation identifier used for
        /// device binding and audit.
        /// </summary>
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// Represents the result of preparing a scan session on the consumer device.
    /// </summary>
    public sealed class ScanSessionPreparedDto
    {
        /// <summary>
        /// Gets or sets the identifier of the newly created scan session.
        /// This value is encoded into the QR code.
        /// </summary>
        public Guid ScanSessionId { get; set; }

        /// <summary>
        /// Gets or sets the scan mode of the session.
        /// </summary>
        public LoyaltyScanMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the UTC expiry time of the session.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the current points balance of the underlying loyalty account.
        /// This is useful for the consumer UI to show feedback.
        /// </summary>
        public int CurrentPointsBalance { get; set; }
    }

    /// <summary>
    /// Business-facing view of a scan session after scanning the QR code.
    /// </summary>
    public sealed class ScanSessionBusinessViewDto
    {
        /// <summary>
        /// Gets or sets the identifier of the scan session.
        /// </summary>
        public Guid ScanSessionId { get; set; }

        /// <summary>
        /// Gets or sets the mode of the session (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the loyalty account identifier.
        /// This is typically not shown in the UI but used for follow-up calls.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the current points balance for the account at the time
        /// the session was processed.
        /// </summary>
        public int CurrentPointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the optional human-friendly customer display name
        /// (e.g., from the user profile) for cashier confirmation.
        /// </summary>
        public string? CustomerDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the list of selected rewards to redeem in this session.
        /// Empty for accrual mode.
        /// </summary>
        public List<SelectedRewardItemDto> SelectedRewards { get; set; } = new();
    }

    /// <summary>
    /// Input data for confirming a redemption based on an existing scan session.
    /// </summary>
    public sealed class ConfirmRedemptionFromSessionDto
    {
        /// <summary>
        /// Gets or sets the identifier of the scan session to confirm.
        /// </summary>
        public Guid ScanSessionId { get; set; }
    }

    /// <summary>
    /// Input data for confirming an accrual based on an existing scan session.
    /// </summary>
    public sealed class ConfirmAccrualFromSessionDto
    {
        /// <summary>
        /// Gets or sets the identifier of the scan session to confirm.
        /// </summary>
        public Guid ScanSessionId { get; set; }

        /// <summary>
        /// Gets or sets the number of points to add for this accrual.
        /// For per-visit programs this is typically 1.
        /// </summary>
        public int Points { get; set; } = 1;

        /// <summary>
        /// Gets or sets an optional note or reference for audit purposes.
        /// </summary>
        public string? Note { get; set; }
    }


    /// <summary>
    /// Represents the outcome of confirming an accrual based on a scan session.
    /// </summary>
    public sealed class ConfirmAccrualResultDto
    {
        /// <summary>
        /// Gets or sets the identifier of the loyalty account whose balance was updated.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the new points balance after the accrual.
        /// </summary>
        public int NewPointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the new lifetime points value after the accrual.
        /// </summary>
        public int NewLifetimePoints { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the created points transaction.
        /// </summary>
        public Guid PointsTransactionId { get; set; }
    }

    /// <summary>
    /// Represents the outcome of confirming a redemption based on a scan session.
    /// </summary>
    public sealed class ConfirmRedemptionResultDto
    {
        /// <summary>
        /// Gets or sets the identifier of the loyalty account whose balance was updated.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Gets or sets the number of points that were consumed by the redemption.
        /// </summary>
        public int PointsSpent { get; set; }

        /// <summary>
        /// Gets or sets the new points balance after the redemption.
        /// </summary>
        public int NewPointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the loyalty reward redemption record.
        /// When multiple reward tiers were redeemed in one session, the first tier
        /// is used as the primary identifier and the full selection is stored
        /// in the redemption metadata.
        /// </summary>
        public Guid RewardRedemptionId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the created points transaction.
        /// </summary>
        public Guid PointsTransactionId { get; set; }
    }
}
