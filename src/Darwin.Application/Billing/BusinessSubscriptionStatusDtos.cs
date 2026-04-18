using System;

namespace Darwin.Application.Billing;

/// <summary>
/// Application-layer DTO for business subscription status snapshots.
/// </summary>
public sealed class BusinessSubscriptionStatusDto
{
    public bool HasSubscription { get; init; }
    public Guid SubscriptionId { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string PlanCode { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public long UnitPriceMinor { get; init; }
    public string Currency { get; init; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CurrentPeriodEndUtc { get; init; }
    public DateTime? TrialEndsAtUtc { get; init; }
    public DateTime? CanceledAtUtc { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
}
