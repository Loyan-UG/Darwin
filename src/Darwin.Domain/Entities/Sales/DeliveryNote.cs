using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Sales;

/// <summary>
/// Formal internal Sales delivery document created from shipment-line quantities.
/// </summary>
public sealed class DeliveryNote : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public DeliveryNoteStatus Status { get; set; } = DeliveryNoteStatus.Draft;
    public Guid? PreparedByUserId { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public Guid? ShippedByUserId { get; set; }
    public Guid? DeliveredByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public string? Carrier { get; set; }
    public string? Service { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ProviderShipmentReference { get; set; }
    public string ShippingAddressJson { get; set; } = "{}";
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public int TotalQuantity { get; set; }
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<DeliveryNoteLine> Lines { get; set; } = new();
}

/// <summary>
/// Delivery note line snapshot derived from a shipment line and its order-line snapshot.
/// </summary>
public sealed class DeliveryNoteLine : BaseEntity
{
    public Guid DeliveryNoteId { get; set; }
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
