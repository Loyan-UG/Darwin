using System;
using System.Collections.Generic;

namespace Darwin.Application.Billing;

/// <summary>
/// Application-layer billing plan summary DTO.
/// </summary>
public sealed class BillingPlanSummaryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long PriceMinor { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Interval { get; init; } = string.Empty;
    public int IntervalCount { get; init; }
    public int? TrialDays { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Application-layer response for billing plans query.
/// </summary>
public sealed class GetBillingPlansDto
{
    public IReadOnlyList<BillingPlanSummaryDto> Items { get; init; } = new List<BillingPlanSummaryDto>();
}
