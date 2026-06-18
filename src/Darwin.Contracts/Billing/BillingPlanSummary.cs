using System;

using Darwin.Contracts.Common;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Public billing plan summary for business mobile operators.
/// </summary>
public sealed class BillingPlanSummary
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;
    public string Interval { get; set; } = string.Empty;
    public int IntervalCount { get; set; }
    public int? TrialDays { get; set; }
    public bool IsActive { get; set; }
    public int MaxStaff { get; set; }
    public int MaxRewardTiers { get; set; }
    public int MonthlyPushCampaigns { get; set; }
    public bool CampaignsInApp { get; set; }
    public bool CampaignsPush { get; set; }
    public bool AdvancedTargeting { get; set; }
    public bool Exports { get; set; }
    public bool Sla { get; set; }
}
