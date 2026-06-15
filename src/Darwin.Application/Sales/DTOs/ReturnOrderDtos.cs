using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.DTOs;

public enum ReturnOrderDocumentFilter
{
    All = 0,
    Requested = 1,
    Approved = 2,
    ReturnShipmentQueued = 3,
    Received = 4,
    Inspected = 5,
    RefundReady = 6,
    Refunded = 7,
    Closed = 8,
    Cancelled = 9,
    Open = 10
}

public sealed class ReturnOrderCreateLineDto
{
    public Guid OrderLineId { get; set; }
    public Guid? ShipmentLineId { get; set; }
    public int RequestedQuantity { get; set; }
}

public sealed class ReturnOrderQuantityLineDto
{
    public Guid LineId { get; set; }
    public int Quantity { get; set; }
}

public sealed class ReturnOrderInspectionLineDto
{
    public Guid LineId { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    public int ScrappedQuantity { get; set; }
    public int RestockQuantity { get; set; }
    public Guid? RestockWarehouseId { get; set; }
}

public sealed class ReturnOrderCreateDto
{
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? CustomerSnapshotJson { get; set; }
    public string? ShippingAddressJson { get; set; }
    public string? InternalNotes { get; set; }
    public List<ReturnOrderCreateLineDto> Lines { get; set; } = new();
}

public class ReturnOrderLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? ActorUserId { get; set; }
    public string? Reason { get; set; }
}

public sealed class ReturnOrderApproveDto : ReturnOrderLifecycleDto
{
    public List<ReturnOrderQuantityLineDto> Lines { get; set; } = new();
}

public sealed class ReturnOrderQueueShipmentDto : ReturnOrderLifecycleDto
{
    public Guid? ShipmentId { get; set; }
}

public sealed class ReturnOrderReceiveDto : ReturnOrderLifecycleDto
{
    public List<ReturnOrderQuantityLineDto> Lines { get; set; } = new();
}

public sealed class ReturnOrderInspectDto : ReturnOrderLifecycleDto
{
    public List<ReturnOrderInspectionLineDto> Lines { get; set; } = new();
}

public sealed class ReturnOrderLinkRefundDto : ReturnOrderLifecycleDto
{
    public Guid RefundId { get; set; }
    public string? Notes { get; set; }
}

public class ReturnOrderListItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? ReturnOrderNumber { get; set; }
    public ReturnOrderStatus Status { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RestockQuantity { get; set; }
    public long RefundEligibleGrossMinor { get; set; }
    public long LinkedRefundGrossMinor { get; set; }
    public long RemainingRefundGrossMinor { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? InspectedAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class ReturnOrderDetailDto : ReturnOrderListItemDto
{
    public string CustomerSnapshotJson { get; set; } = "{}";
    public string ShippingAddressJson { get; set; } = "{}";
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string? OrderNumber { get; set; }
    public long RequestedGrossMinor { get; set; }
    public long ApprovedGrossMinor { get; set; }
    public long AcceptedGrossMinor { get; set; }
    public List<ReturnOrderLineDetailDto> Lines { get; set; } = new();
    public List<ReturnOrderRefundLinkDto> RefundLinks { get; set; } = new();
}

public sealed class ReturnOrderLineDetailDto
{
    public Guid Id { get; set; }
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
    public long RequestedGrossMinor { get; set; }
    public long ApprovedGrossMinor { get; set; }
    public long AcceptedGrossMinor { get; set; }
    public ReturnInspectionDisposition Disposition { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ReturnOrderRefundLinkDto
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
