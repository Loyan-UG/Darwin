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
}
