using System;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Business-scoped subscription snapshot used by mobile business settings.
/// </summary>
public sealed class BusinessSubscriptionStatusResponse
{
    public bool HasSubscription { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public long UnitPriceMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
}
