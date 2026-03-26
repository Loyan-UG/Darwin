namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Aggregated member-facing loyalty overview spanning all businesses.
/// </summary>
public sealed class MyLoyaltyOverviewResponse
{
    /// <summary>Gets or sets the total account count.</summary>
    public int TotalAccounts { get; set; }

    /// <summary>Gets or sets the count of active accounts.</summary>
    public int ActiveAccounts { get; set; }

    /// <summary>Gets or sets the total spendable points balance across all accounts.</summary>
    public int TotalPointsBalance { get; set; }

    /// <summary>Gets or sets the total lifetime points across all accounts.</summary>
    public int TotalLifetimePoints { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the latest accrual across all accounts.</summary>
    public DateTime? LastAccrualAtUtc { get; set; }

    /// <summary>Gets or sets the ordered loyalty account summaries.</summary>
    public IReadOnlyList<LoyaltyAccountSummary> Accounts { get; set; } = Array.Empty<LoyaltyAccountSummary>();
}

/// <summary>
/// Business-scoped member-facing loyalty dashboard.
/// </summary>
public sealed class MyLoyaltyBusinessDashboard
{
    /// <summary>Gets or sets the current business-scoped account.</summary>
    public LoyaltyAccountSummary Account { get; set; } = new();

    /// <summary>Gets or sets the total number of configured rewards.</summary>
    public int AvailableRewardsCount { get; set; }

    /// <summary>Gets or sets the number of currently redeemable rewards.</summary>
    public int RedeemableRewardsCount { get; set; }

    /// <summary>Gets or sets the next attainable reward, if one exists.</summary>
    public LoyaltyRewardSummary? NextReward { get; set; }

    /// <summary>Gets or sets recent transactions for the business-scoped account.</summary>
    public IReadOnlyList<PointsTransaction> RecentTransactions { get; set; } = Array.Empty<PointsTransaction>();

    /// <summary>Gets or sets the points still required to unlock the next reward.</summary>
    public int? PointsToNextReward { get; set; }

    /// <summary>Gets or sets the next reward threshold in points, when one exists.</summary>
    public int? NextRewardRequiredPoints { get; set; }

    /// <summary>Gets or sets the percentage progress toward the next reward threshold.</summary>
    public decimal? NextRewardProgressPercent { get; set; }

    /// <summary>Gets or sets a value indicating whether the loyalty implementation currently tracks point expiry.</summary>
    public bool ExpiryTrackingEnabled { get; set; }

    /// <summary>Gets or sets the points that are known to expire soon, when expiry tracking is enabled.</summary>
    public int PointsExpiringSoon { get; set; }

    /// <summary>Gets or sets the nearest known point-expiry timestamp in UTC, when available.</summary>
    public DateTime? NextPointsExpiryAtUtc { get; set; }
}
