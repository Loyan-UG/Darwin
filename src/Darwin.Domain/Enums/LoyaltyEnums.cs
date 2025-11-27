namespace Darwin.Domain.Enums
{
    /// <summary>How points are accrued.</summary>
    public enum LoyaltyAccrualMode
    {
        PerVisit = 0,
        AmountBased = 1
    }

    /// <summary>Reward type offered at a tier.</summary>
    public enum LoyaltyRewardType
    {
        FreeItem = 0,
        PercentDiscount = 1,
        AmountDiscount = 2
    }

    /// <summary>Logical status of a loyalty account.</summary>
    public enum LoyaltyAccountStatus
    {
        Active = 0,
        Suspended = 1,
        Closed = 2
    }

    /// <summary>Ledger entry type for points.</summary>
    public enum LoyaltyPointsTransactionType
    {
        Accrual = 0,
        Redemption = 1,
        Adjustment = 2
    }

    /// <summary>Lifecycle of a redemption operation.</summary>
    public enum LoyaltyRedemptionStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2
    }

    /// <summary>Purpose of a short-lived QR token.</summary>
    public enum QrTokenPurpose
    {
        Accrual = 0,
        Redemption = 1,
        IdentityOnly = 2
    }

    /// <summary>
    /// Represents the high-level mode of a loyalty scan session.
    /// </summary>
    public enum LoyaltyScanMode
    {
        /// <summary>
        /// The scan session is used to accrue points (earn).
        /// </summary>
        Accrual = 0,

        /// <summary>
        /// The scan session is used to redeem one or more rewards (spend).
        /// </summary>
        Redemption = 1
    }

    /// <summary>
    /// Represents the lifecycle status of a scan session.
    /// </summary>
    public enum LoyaltyScanStatus
    {
        /// <summary>
        /// Session has been created and is waiting to be processed by a business device.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Session has been successfully processed (either accrual or redemption).
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Session has been explicitly cancelled by either party.
        /// </summary>
        Cancelled = 2,

        /// <summary>
        /// Session has expired and can no longer be used.
        /// </summary>
        Expired = 3
    }
}
