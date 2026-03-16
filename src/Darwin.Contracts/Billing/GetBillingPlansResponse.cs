using System.Collections.Generic;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Response payload containing available billing plans.
/// </summary>
public sealed class GetBillingPlansResponse
{
    public IReadOnlyList<BillingPlanSummary> Items { get; set; } = new List<BillingPlanSummary>();
}
