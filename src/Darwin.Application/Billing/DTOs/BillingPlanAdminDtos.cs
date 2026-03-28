using Darwin.Domain.Enums;

namespace Darwin.Application.Billing.DTOs;

public class BillingPlanCreateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public BillingInterval Interval { get; set; } = BillingInterval.Month;
    public int IntervalCount { get; set; } = 1;
    public int? TrialDays { get; set; }
    public bool IsActive { get; set; } = true;
    public string FeaturesJson { get; set; } = "{}";
}

public sealed class BillingPlanEditDto : BillingPlanCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BillingPlanListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public BillingInterval Interval { get; set; } = BillingInterval.Month;
    public int IntervalCount { get; set; }
    public int? TrialDays { get; set; }
    public bool IsActive { get; set; }
    public bool HasFeatures { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public enum BillingPlanQueueFilter
{
    All = 0,
    Active = 1,
    Inactive = 2,
    Trial = 3,
    MissingFeatures = 4,
    InUse = 5
}

public sealed class BillingPlanOpsSummaryDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TrialCount { get; set; }
    public int MissingFeaturesCount { get; set; }
    public int InUseCount { get; set; }
}
