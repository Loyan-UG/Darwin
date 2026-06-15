using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.DTOs;

public enum DeliveryNoteDocumentFilter
{
    All = 0,
    Draft = 1,
    Prepared = 2,
    Issued = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Open = 7
}

public class DeliveryNoteListItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public DeliveryNoteStatus Status { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public int TotalQuantity { get; set; }
    public long TotalGrossMinor { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class DeliveryNoteDetailDto : DeliveryNoteListItemDto
{
    public string? Service { get; set; }
    public string? ProviderShipmentReference { get; set; }
    public string ShippingAddressJson { get; set; } = "{}";
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public Guid? ShippedByUserId { get; set; }
    public Guid? DeliveredByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string? OrderNumber { get; set; }
    public List<DeliveryNoteLineDetailDto> Lines { get; set; } = new();
}

public sealed class DeliveryNoteLineDetailDto
{
    public Guid Id { get; set; }
    public Guid OrderLineId { get; set; }
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

public sealed class DeliveryNoteCreateFromShipmentDto
{
    public Guid ShipmentId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? InternalNotes { get; set; }
}

public class DeliveryNoteLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? ActorUserId { get; set; }
    public string? Reason { get; set; }
}
