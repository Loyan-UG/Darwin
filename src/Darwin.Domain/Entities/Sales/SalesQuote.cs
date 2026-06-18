using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Sales;

/// <summary>
/// Internal Sales quote document. It is separate from CRM opportunities and does not replace orders.
/// </summary>
public sealed class SalesQuote : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? ConvertedOrderId { get; set; }
    public string? QuoteNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public SalesQuoteStatus Status { get; set; } = SalesQuoteStatus.Draft;
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public DateTime? ValidUntilUtc { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? SentByUserId { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public Guid? ConvertedByUserId { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
    public DateTime? ConvertedAtUtc { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public string CustomerSnapshotJson { get; set; } = "{}";
    public string BillingAddressJson { get; set; } = "{}";
    public string ShippingAddressJson { get; set; } = "{}";
    public string? InternalNotes { get; set; }
    public List<SalesQuoteLine> Lines { get; set; } = new();
}

/// <summary>
/// Snapshot line for an internal Sales quote.
/// </summary>
public sealed class SalesQuoteLine : BaseEntity
{
    public Guid SalesQuoteId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceNetMinor { get; set; }
    public long UnitPriceGrossMinor { get; set; }
    public decimal TaxRate { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public int SortOrder { get; set; }
}
