namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Describes the type of a timeline entry returned by the unified loyalty timeline endpoint.
    /// </summary>
    /// <remarks>
    /// This enum is intentionally contract-level and stable for mobile parsing.
    /// </remarks>
    public enum LoyaltyTimelineEntryKind
    {
        /// <summary>
        /// A points ledger transaction (accrual, redemption, or adjustment).
        /// </summary>
        PointsTransaction = 0,

        /// <summary>
        /// A reward redemption record (confirmed redemption event).
        /// </summary>
        RewardRedemption = 1
    }
}
