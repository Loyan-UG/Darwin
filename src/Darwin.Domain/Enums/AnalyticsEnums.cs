namespace Darwin.Domain.Enums
{
    /// <summary>
    /// High-level report category for analytics exports.
    /// Extend as mobile/business requirements evolve.
    /// </summary>
    public enum AnalyticsReportType : short
    {
        Custom = 0,
        LoyaltyVisits = 10,
        LoyaltyRedemptions = 11,
        LoyaltyTransactions = 12,
        TopCustomers = 20,
        BusinessOverview = 30
    }

    /// <summary>
    /// Output format for analytics exports.
    /// </summary>
    public enum AnalyticsExportFormat : short
    {
        Csv = 1,
        Pdf = 2
    }

    /// <summary>
    /// Lifecycle status for analytics export jobs.
    /// </summary>
    public enum AnalyticsExportStatus : short
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }
}
