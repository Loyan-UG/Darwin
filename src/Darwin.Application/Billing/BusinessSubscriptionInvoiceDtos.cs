using Darwin.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Darwin.Application.Billing;

/// <summary>
/// Queue filters for business subscription invoices in the admin workspace.
/// </summary>
public enum BusinessSubscriptionInvoiceQueueFilter
{
    All = 0,
    Open = 1,
    Paid = 2,
    Draft = 3,
    Uncollectible = 4,
    HostedLinkMissing = 5,
    Stripe = 6,
    Overdue = 7,
    PdfMissing = 8
}

/// <summary>
/// Lightweight invoice row for business subscription support.
/// </summary>
public sealed class BusinessSubscriptionInvoiceListItemDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid BusinessSubscriptionId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? ProviderInvoiceId { get; init; }
    public SubscriptionInvoiceStatus Status { get; init; }
    public long TotalMinor { get; init; }
    public string Currency { get; init; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
    public DateTime IssuedAtUtc { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }
    public string? HostedInvoiceUrl { get; init; }
    public string? PdfUrl { get; init; }
    public string? FailureReason { get; init; }
    public string? PlanName { get; init; }
    public string? PlanCode { get; init; }
    public bool HasHostedInvoiceUrl => !string.IsNullOrWhiteSpace(HostedInvoiceUrl);
    public bool HasPdfUrl => !string.IsNullOrWhiteSpace(PdfUrl);
    public bool IsStripe => string.Equals(Provider, "Stripe", StringComparison.OrdinalIgnoreCase);
    public bool IsOverdue => Status == SubscriptionInvoiceStatus.Open && DueAtUtc.HasValue && DueAtUtc.Value < DateTime.UtcNow;
}

/// <summary>
/// Summary counters for business subscription invoice operations.
/// </summary>
public sealed class BusinessSubscriptionInvoiceOpsSummaryDto
{
    public int TotalCount { get; init; }
    public int OpenCount { get; init; }
    public int PaidCount { get; init; }
    public int DraftCount { get; init; }
    public int UncollectibleCount { get; init; }
    public int HostedLinkMissingCount { get; init; }
    public int StripeCount { get; init; }
    public int OverdueCount { get; init; }
    public int PdfMissingCount { get; init; }
}

/// <summary>
/// Result page for business subscription invoices.
/// </summary>
public sealed class GetBusinessSubscriptionInvoicesPageDto
{
    public IReadOnlyList<BusinessSubscriptionInvoiceListItemDto> Items { get; init; } = new List<BusinessSubscriptionInvoiceListItemDto>();
    public int Total { get; init; }
}
