namespace Darwin.Application.Loyalty.DTOs;

/// <summary>
/// Aggregated member-facing loyalty overview spanning all businesses.
/// </summary>
public sealed class MyLoyaltyOverviewDto
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

    /// <summary>Gets or sets the ordered list of loyalty account summaries.</summary>
    public IReadOnlyList<LoyaltyAccountSummaryDto> Accounts { get; set; } = Array.Empty<LoyaltyAccountSummaryDto>();
}

/// <summary>
/// Aggregated business-scoped loyalty dashboard for the current member.
/// </summary>
public sealed class MyLoyaltyBusinessDashboardDto
{
    /// <summary>Gets or sets the current business-scoped loyalty account.</summary>
    public LoyaltyAccountSummaryDto Account { get; set; } = new();

    /// <summary>Gets or sets the total number of rewards currently configured for the business.</summary>
    public int AvailableRewardsCount { get; set; }

    /// <summary>Gets or sets the number of rewards currently redeemable by the member.</summary>
    public int RedeemableRewardsCount { get; set; }

    /// <summary>Gets or sets the next attainable reward, if one exists.</summary>
    public LoyaltyRewardSummaryDto? NextReward { get; set; }

    /// <summary>Gets or sets the most recent loyalty transactions for this business.</summary>
    public IReadOnlyList<LoyaltyPointsTransactionDto> RecentTransactions { get; set; } = Array.Empty<LoyaltyPointsTransactionDto>();
}
