using System;

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
    public string Currency { get; set; } = "EUR";
    public string Interval { get; set; } = string.Empty;
    public int IntervalCount { get; set; }
    public int? TrialDays { get; set; }
    public bool IsActive { get; set; }
}
