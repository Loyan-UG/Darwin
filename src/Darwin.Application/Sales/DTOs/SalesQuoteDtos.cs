using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.DTOs;

public enum SalesQuoteDocumentFilter
{
    All = 0,
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    ExpiringSoon = 4,
    Converted = 5,
    Closed = 6
}

public sealed class SalesQuoteLineEditDto
{
    public Guid? Id { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceNetMinor { get; set; }
    public long UnitPriceGrossMinor { get; set; }
    public decimal TaxRate { get; set; }
    public int SortOrder { get; set; }
}

public class SalesQuoteCreateDto
{
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OpportunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public DateTime? ValidUntilUtc { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public string? CustomerSnapshotJson { get; set; }
    public string? BillingAddressJson { get; set; }
    public string? ShippingAddressJson { get; set; }
    public string? InternalNotes { get; set; }
    public List<SalesQuoteLineEditDto> Lines { get; set; } = new();
}

public sealed class SalesQuoteEditDto : SalesQuoteCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class SalesQuoteLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? ActorUserId { get; set; }
    public string? Reason { get; set; }
}

public sealed class SalesQuoteConvertDto : SalesQuoteLifecycleDto
{
    public Guid ConvertedOrderId { get; set; }
}

public sealed class SalesQuoteCreateOrderDto : SalesQuoteLifecycleDto
{
}

public sealed class SalesQuoteListItemDto
{
    public Guid Id { get; set; }
    public string? QuoteNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public SalesQuoteStatus Status { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? ConvertedOrderId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int LineCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class SalesQuoteDetailDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? ConvertedOrderId { get; set; }
    public string? ConvertedOrderNumber { get; set; }
    public string? QuoteNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public SalesQuoteStatus Status { get; set; }
    public string Currency { get; set; } = string.Empty;
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
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IReadOnlyList<SalesQuoteLineDetailDto> Lines { get; set; } = Array.Empty<SalesQuoteLineDetailDto>();
}

public sealed class SalesQuoteLineDetailDto
{
    public Guid Id { get; set; }
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
