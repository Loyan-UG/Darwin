using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO for creating a reward redemption (system/business app flow).
    /// </summary>
    public sealed class LoyaltyRewardRedemptionCreateDto
    {
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid RewardTierId { get; set; }
        public int PointsSpent { get; set; }

        public DateTime RedeemedAtUtc { get; set; }

        //public Guid? ScanSessionId { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Lightweight list item for grids.
    /// </summary>
    public sealed class LoyaltyRewardRedemptionListItemDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid RewardTierId { get; set; }
        public int PointsSpent { get; set; }

        public DateTime RedeemedAtUtc { get; set; }

        public Guid? ScanSessionId { get; set; }
        public string? Note { get; set; }
    }


    /// <summary>
    /// DTO used to confirm a previously created, pending loyalty reward redemption.
    /// This DTO is intended for the "two-step" redemption flow where:
    ///  - Step 1 (scan time): a LoyaltyRewardRedemption entity is created with Status = Pending
    ///    by another use-case (typically the QR scan processing on the business device).
    ///  - Step 2 (staff confirmation): a business/staff user explicitly confirms the redemption
    ///    after the reward has actually been fulfilled (e.g., the customer received the free item).
    ///
    /// The ConfirmLoyaltyRewardRedemptionHandler uses this DTO to:
    ///  - Validate the request (business consistency, concurrency, etc.).
    ///  - Ensure the account still has enough points to cover the redemption.
    ///  - Change the redemption status from Pending to Confirmed.
    ///  - Create a LoyaltyPointsTransaction of type Redemption and update the account balance.
    ///
    /// AI usage guidance:
    ///  - Use this DTO when you want to finalize a reward that was already requested/created,
    ///    not when starting a new redemption from scratch.
    ///  - The typical producer of the pending redemption is a QR scan flow for tiers where
    ///    AllowSelfRedemption == false.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionDto
    {
        /// <summary>
        /// Identifier of the loyalty reward redemption that should be confirmed.
        /// This must point to an existing LoyaltyRewardRedemption with Status = Pending.
        /// </summary>
        public Guid RedemptionId { get; set; }

        /// <summary>
        /// Business that owns the loyalty program and the redemption.
        /// Used as a safety check to prevent cross-business modifications.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional business location (branch) where the confirmation is performed.
        /// When provided, it overrides the BusinessLocationId stored on the redemption entity.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Optional identifier of the business user (staff) performing the confirmation.
        /// This value is propagated into the resulting LoyaltyPointsTransaction.
        /// </summary>
        public Guid? PerformedByUserId { get; set; }

        /// <summary>
        /// Optional concurrency token. When supplied, the handler will verify that the row
        /// version on the redemption has not changed since the client last read it.
        /// This allows the UI to detect "double edits" on the same redemption.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Result DTO returned after a successful confirmation of a pending loyalty reward redemption.
    /// Designed for both human-facing UIs and automated clients (including AI-based flows).
    ///
    /// It exposes:
    ///  - The redemption that was confirmed.
    ///  - The loyalty account that was affected.
    ///  - The resulting ledger transaction.
    ///  - The new points balance (and lifetime points) on the account.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionResultDto
    {
        /// <summary>
        /// Identifier of the loyalty reward redemption that was confirmed.
        /// </summary>
        public Guid RedemptionId { get; set; }

        /// <summary>
        /// Identifier of the loyalty account whose points were consumed.
        /// </summary>
        public Guid LoyaltyAccountId { get; set; }

        /// <summary>
        /// Business that owns the loyalty program and the redemption.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Optional business location on which the successful confirmation is attributed.
        /// </summary>
        public Guid? BusinessLocationId { get; set; }

        /// <summary>
        /// Identifier of the newly created ledger transaction of type Redemption.
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// New spendable points balance on the loyalty account after the redemption.
        /// </summary>
        public int NewPointsBalance { get; set; }

        /// <summary>
        /// Lifetime points on the account after the confirmation.
        /// Note that the current domain model keeps LifetimePoints as a cumulative
        /// "ever earned" value; redemptions do not decrease it. We still expose the
        /// resulting value here to give clients a complete picture.
        /// </summary>
        public int NewLifetimePoints { get; set; }
    }
}
