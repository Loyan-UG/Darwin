using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Sales;

/// <summary>
/// Formal internal Sales return document. It models return lifecycle, eligibility, evidence, and links to authoritative refund/inventory records.
/// </summary>
public sealed class ReturnOrder : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? ReturnOrderNumber { get; set; }
    public ReturnOrderStatus Status { get; set; } = ReturnOrderStatus.Requested;
    public Guid? RequestedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public Guid? ReturnShipmentQueuedByUserId { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public Guid? InspectedByUserId { get; set; }
    public Guid? RefundReadyByUserId { get; set; }
    public Guid? RefundedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? ReturnShipmentQueuedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? InspectedAtUtc { get; set; }
    public DateTime? RefundReadyAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public string CustomerSnapshotJson { get; set; } = "{}";
    public string ShippingAddressJson { get; set; } = "{}";
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    public int ScrappedQuantity { get; set; }
    public int RestockQuantity { get; set; }
    public long RequestedGrossMinor { get; set; }
    public long ApprovedGrossMinor { get; set; }
    public long AcceptedGrossMinor { get; set; }
    public long RefundEligibleGrossMinor { get; set; }
    public List<ReturnOrderLine> Lines { get; set; } = new();
    public List<ReturnOrderRefundLink> RefundLinks { get; set; } = new();
}

/// <summary>
/// Return order line snapshot over the current order/shipment line foundation.
/// </summary>
public sealed class ReturnOrderLine : BaseEntity
{
    public Guid ReturnOrderId { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid? ShipmentLineId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid? RestockWarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    public int ScrappedQuantity { get; set; }
    public int RestockQuantity { get; set; }
    public long UnitPriceNetMinor { get; set; }
    public long UnitPriceGrossMinor { get; set; }
    public decimal TaxRate { get; set; }
    public long RequestedNetMinor { get; set; }
    public long RequestedTaxMinor { get; set; }
    public long RequestedGrossMinor { get; set; }
    public long ApprovedNetMinor { get; set; }
    public long ApprovedTaxMinor { get; set; }
    public long ApprovedGrossMinor { get; set; }
    public long AcceptedNetMinor { get; set; }
    public long AcceptedTaxMinor { get; set; }
    public long AcceptedGrossMinor { get; set; }
    public ReturnInspectionDisposition Disposition { get; set; } = ReturnInspectionDisposition.NotInspected;
    public int SortOrder { get; set; }
}

/// <summary>
/// Link from a return order to one or more authoritative refund settlement records.
/// </summary>
public sealed class ReturnOrderRefundLink : BaseEntity
{
    public Guid ReturnOrderId { get; set; }
    public Guid RefundId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public string? Notes { get; set; }
}
