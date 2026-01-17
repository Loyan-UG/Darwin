namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Billing interval units for subscriptions.
    /// </summary>
    public enum BillingInterval : short
    {
        Day = 1,
        Week = 2,
        Month = 3,
        Year = 4
    }

    /// <summary>
    /// Subscription lifecycle aligned with common provider concepts (e.g., Stripe).
    /// </summary>
    public enum SubscriptionStatus : short
    {
        Trialing = 0,
        Active = 1,
        PastDue = 2,
        Canceled = 3,
        Unpaid = 4,
        Incomplete = 5,
        IncompleteExpired = 6,
        Paused = 7
    }

    /// <summary>
    /// Invoice status aligned with common provider concepts.
    /// </summary>
    public enum SubscriptionInvoiceStatus : short
    {
        Draft = 0,
        Open = 1,
        Paid = 2,
        Void = 3,
        Uncollectible = 4
    }
}
