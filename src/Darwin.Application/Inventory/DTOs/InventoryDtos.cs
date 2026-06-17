using System;

using Darwin.Domain.Enums;

namespace Darwin.Application.Inventory.DTOs
{
    public enum StockLevelQueueFilter
    {
        All = 0,
        LowStock = 1,
        Reserved = 2,
        InTransit = 3
    }

    public enum PurchaseOrderQueueFilter
    {
        All = 0,
        Draft = 1,
        Issued = 2,
        Received = 3,
        Cancelled = 4,
        StaleIssued = 5
    }

    public enum GoodsReceiptQueueFilter
    {
        All = 0,
        Draft = 1,
        Received = 2,
        Inspected = 3,
        Posted = 4,
        Cancelled = 5
    }

    public enum StockTransferQueueFilter
    {
        All = 0,
        Draft = 1,
        InTransit = 2,
        Completed = 3,
        Cancelled = 4,
        StaleInTransit = 5
    }

    public enum WarehouseQueueFilter
    {
        All = 0,
        Default = 1,
        NoStockLevels = 2
    }

    public enum WarehouseLocationQueueFilter
    {
        All = 0,
        Active = 1,
        Inactive = 2,
        Blocked = 3,
        Bins = 4,
        Docks = 5,
        QualityHold = 6
    }

    public enum ProductTrackingPolicyQueueFilter
    {
        All = 0,
        Active = 1,
        Inactive = 2,
        Archived = 3,
        Tracked = 4
    }

    public enum InventoryLotQueueFilter
    {
        All = 0,
        Draft = 1,
        Active = 2,
        Quarantined = 3,
        Expired = 4,
        Recalled = 5,
        Closed = 6
    }

    public enum InventorySerialUnitQueueFilter
    {
        All = 0,
        Received = 1,
        Available = 2,
        Reserved = 3,
        Picked = 4,
        Shipped = 5,
        Quarantined = 6,
        Scrapped = 7
    }

    public enum HandlingUnitQueueFilter
    {
        All = 0,
        Open = 1,
        Closed = 2,
        InTransit = 3,
        Received = 4,
        BrokenDown = 5,
        Cancelled = 6
    }

    public enum WarehouseLabelTemplateQueueFilter
    {
        All = 0,
        Active = 1,
        Inactive = 2,
        Default = 3
    }

    public enum WarehouseTaskQueueFilter
    {
        All = 0,
        Draft = 1,
        Ready = 2,
        Assigned = 3,
        InProgress = 4,
        Completed = 5,
        Cancelled = 6,
        NeedsAssignment = 7,
        Overdue = 8,
        Shortage = 9
    }

    public enum StockCountQueueFilter
    {
        All = 0,
        Draft = 1,
        InProgress = 2,
        ReviewPending = 3,
        Approved = 4,
        Posted = 5,
        Cancelled = 6,
        Variance = 7
    }

    public enum SupplierQueueFilter
    {
        All = 0,
        MissingAddress = 1,
        HasPurchaseOrders = 2,
        Inactive = 3,
        Blocked = 4
    }

    public enum InventoryLedgerQueueFilter
    {
        All = 0,
        Inbound = 1,
        Outbound = 2,
        Reservations = 3
    }

    /// <summary>Manual or system-driven inventory adjustment.</summary>
    public sealed class InventoryAdjustDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid VariantId { get; set; }
        /// <summary>Delta applied to on-hand stock (positive receipt, negative write-off).</summary>
        public int QuantityDelta { get; set; }
        /// <summary>Reason code or description (e.g., GoodsReceipt, Adjustment, ShipmentAllocation).</summary>
        public string Reason { get; set; } = "Adjustment";
        /// <summary>Optional reference aggregate id (order, return, etc.).</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Reserve quantity for outstanding carts/orders.</summary>
    public sealed class InventoryReserveDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Reservation";
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Release previously reserved quantity (e.g., cart abandonment, order cancel).</summary>
    public sealed class InventoryReleaseReservationDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "ReservationRelease";
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Lightweight ledger row for admin reports.</summary>
    public sealed class InventoryTransactionRowDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid VariantId { get; set; }
        public int QuantityDelta { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Request to allocate inventory for a placed order.
    /// This operation reduces on-hand stock and releases an equal reserved amount per line.
    /// One ledger row (negative QuantityDelta) is appended per affected variant.
    /// </summary>
    public sealed class InventoryAllocateForOrderDto
    {
        /// <summary>Order identifier used for correlation and idempotency.</summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Optional warehouse override for all lines in this request.
        /// When omitted, the handler resolves a warehouse from existing stock levels.
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>Lines to allocate; each entry references a variant and a quantity to allocate.</summary>
        public List<InventoryAllocateForOrderLineDto> Lines { get; set; } = new();
    }

    /// <summary>Single allocation line for a specific variant.</summary>
    public sealed class InventoryAllocateForOrderLineDto
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>
        /// Optional line-specific warehouse selection.
        /// When present, it takes precedence over the parent request warehouse.
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>Quantity to allocate (must be positive).</summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Lightweight warehouse lookup item for dropdowns and command forms.
    /// </summary>
    public sealed class WarehouseLookupItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Request to process a customer return (goods received back to stock).
    /// This operation increases on-hand stock and appends a positive ledger row.
    /// </summary>
    public sealed class InventoryReturnReceiptDto
    {
        /// <summary>Optional target warehouse identifier for the returned stock.</summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>Returned variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Positive quantity to receive back.</summary>
        public int Quantity { get; set; }

        /// <summary>Optional return/case identifier (used for correlation/idempotency).</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Reason tag recorded in the ledger; default is 'ReturnReceipt'.</summary>
        public string Reason { get; set; } = "ReturnReceipt";
    }

    /// <summary>
    /// Warehouse list row for admin grids.
    /// </summary>
    public sealed class WarehouseListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
        public int StockLevelCount { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int DefaultCount { get; set; }
        public int NoStockLevelsCount { get; set; }
    }

    /// <summary>
    /// Warehouse create payload.
    /// </summary>
    public class WarehouseCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Warehouse edit payload with concurrency token.
    /// </summary>
    public sealed class WarehouseEditDto : WarehouseCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseLocationListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid? ParentLocationId { get; set; }
        public string? ParentCode { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WarehouseLocationType LocationType { get; set; } = WarehouseLocationType.Bin;
        public WarehouseLocationStatus Status { get; set; } = WarehouseLocationStatus.Active;
        public string? Barcode { get; set; }
        public int SortOrder { get; set; }
        public int ChildCount { get; set; }
    }

    public sealed class WarehouseLocationOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int BlockedCount { get; set; }
        public int BinCount { get; set; }
        public int DockCount { get; set; }
        public int QualityHoldCount { get; set; }
    }

    public class WarehouseLocationCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? ParentLocationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WarehouseLocationType LocationType { get; set; } = WarehouseLocationType.Bin;
        public WarehouseLocationStatus Status { get; set; } = WarehouseLocationStatus.Active;
        public string? Barcode { get; set; }
        public int SortOrder { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class WarehouseLocationEditDto : WarehouseLocationCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseLocationDetailDto : WarehouseLocationEditDto
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public List<WarehouseLocationTreeItemDto> Children { get; set; } = new();
    }

    public sealed class WarehouseLocationTreeItemDto
    {
        public Guid Id { get; set; }
        public Guid? ParentLocationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WarehouseLocationType LocationType { get; set; } = WarehouseLocationType.Bin;
        public WarehouseLocationStatus Status { get; set; } = WarehouseLocationStatus.Active;
        public int SortOrder { get; set; }
        public List<WarehouseLocationTreeItemDto> Children { get; set; } = new();
    }

    public sealed class WarehouseLocationArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ProductTrackingPolicyListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string VariantSku { get; set; } = string.Empty;
        public ProductTrackingMode TrackingMode { get; set; } = ProductTrackingMode.Untracked;
        public ProductTrackingPolicyStatus Status { get; set; } = ProductTrackingPolicyStatus.Active;
        public bool RequiresSupplierLot { get; set; }
        public bool RequiresExpiryDate { get; set; }
        public bool RequiresHandlingUnit { get; set; }
    }

    public sealed class ProductTrackingPolicyOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int TrackedCount { get; set; }
        public int RequiresExpiryCount { get; set; }
        public int RequiresHandlingUnitCount { get; set; }
    }

    public class ProductTrackingPolicyCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public ProductTrackingMode TrackingMode { get; set; } = ProductTrackingMode.Untracked;
        public ProductTrackingPolicyStatus Status { get; set; } = ProductTrackingPolicyStatus.Active;
        public bool RequiresSupplierLot { get; set; }
        public bool RequiresExpiryDate { get; set; }
        public bool RequiresHandlingUnit { get; set; }
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class ProductTrackingPolicyEditDto : ProductTrackingPolicyCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ProductTrackingPolicyDetailDto : ProductTrackingPolicyEditDto
    {
        public string VariantSku { get; set; } = string.Empty;
    }

    public sealed class ProductTrackingPolicyArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InventoryLotListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string VariantSku { get; set; } = string.Empty;
        public string LotCode { get; set; } = string.Empty;
        public string? SupplierLotCode { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public InventoryLotStatus Status { get; set; } = InventoryLotStatus.Draft;
    }

    public sealed class InventoryLotOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int QuarantinedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int RecalledCount { get; set; }
    }

    public class InventoryLotCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public string? SupplierLotCode { get; set; }
        public DateTime? ManufactureDateUtc { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public InventoryLotStatus Status { get; set; } = InventoryLotStatus.Draft;
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class InventoryLotEditDto : InventoryLotCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InventoryLotDetailDto : InventoryLotEditDto
    {
        public string VariantSku { get; set; } = string.Empty;
    }

    public sealed class InventoryLotArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InventorySerialUnitListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string VariantSku { get; set; } = string.Empty;
        public Guid? InventoryLotId { get; set; }
        public string? LotCode { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDateUtc { get; set; }
        public InventorySerialUnitStatus Status { get; set; } = InventorySerialUnitStatus.Received;
    }

    public sealed class InventorySerialUnitOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int AvailableCount { get; set; }
        public int ReservedCount { get; set; }
        public int QuarantinedCount { get; set; }
        public int ScrappedCount { get; set; }
    }

    public class InventorySerialUnitCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime? ManufactureDateUtc { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public InventorySerialUnitStatus Status { get; set; } = InventorySerialUnitStatus.Received;
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class InventorySerialUnitEditDto : InventorySerialUnitCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InventorySerialUnitDetailDto : InventorySerialUnitEditDto
    {
        public string VariantSku { get; set; } = string.Empty;
        public string? LotCode { get; set; }
    }

    public sealed class InventorySerialUnitArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class HandlingUnitListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
        public Guid? ParentHandlingUnitId { get; set; }
        public string? ParentCode { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public HandlingUnitType HandlingUnitType { get; set; } = HandlingUnitType.Pallet;
        public HandlingUnitStatus Status { get; set; } = HandlingUnitStatus.Open;
        public int ContentCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    public sealed class HandlingUnitOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int ClosedCount { get; set; }
        public int InTransitCount { get; set; }
        public int ReceivedCount { get; set; }
    }

    public class HandlingUnitCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? ParentHandlingUnitId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public HandlingUnitType HandlingUnitType { get; set; } = HandlingUnitType.Pallet;
        public HandlingUnitStatus Status { get; set; } = HandlingUnitStatus.Open;
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
        public List<HandlingUnitContentDto> Contents { get; set; } = new();
    }

    public class HandlingUnitEditDto : HandlingUnitCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class HandlingUnitDetailDto : HandlingUnitEditDto
    {
        public string? WarehouseName { get; set; }
        public string? LocationCode { get; set; }
        public string? ParentCode { get; set; }
    }

    public sealed class HandlingUnitContentDto
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public string? SkuSnapshot { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    public sealed class HandlingUnitArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseLabelTemplateListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public WarehouseLabelTemplateStatus Status { get; set; } = WarehouseLabelTemplateStatus.Active;
        public WarehouseLabelTemplateFormat Format { get; set; } = WarehouseLabelTemplateFormat.Html;
        public bool IsDefault { get; set; }
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }
    }

    public sealed class WarehouseLabelTemplateOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int DefaultCount { get; set; }
    }

    public class WarehouseLabelTemplateCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public WarehouseLabelTemplateStatus Status { get; set; } = WarehouseLabelTemplateStatus.Active;
        public WarehouseLabelTemplateFormat Format { get; set; } = WarehouseLabelTemplateFormat.Html;
        public bool IsDefault { get; set; }
        public int WidthMm { get; set; } = 70;
        public int HeightMm { get; set; } = 35;
        public string ContentTemplate { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class WarehouseLabelTemplateEditDto : WarehouseLabelTemplateCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseLabelTemplateArchiveDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseLabelTemplateDetailDto : WarehouseLabelTemplateEditDto
    {
    }

    public sealed class WarehouseLocationLabelItemDto
    {
        public Guid LocationId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ParentCode { get; set; } = string.Empty;
        public string RenderedContent { get; set; } = string.Empty;
    }

    public sealed class WarehouseLocationLabelRenderDto
    {
        public Guid BusinessId { get; set; }
        public Guid TemplateId { get; set; }
        public WarehouseLabelTemplateFormat Format { get; set; }
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }
        public List<WarehouseLocationLabelItemDto> Labels { get; set; } = new();
    }

    public sealed class WarehouseTaskListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid? FromLocationId { get; set; }
        public string? FromLocationCode { get; set; }
        public Guid? ToLocationId { get; set; }
        public string? ToLocationCode { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public string? TaskNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public WarehouseTaskType TaskType { get; set; } = WarehouseTaskType.General;
        public WarehouseTaskStatus Status { get; set; } = WarehouseTaskStatus.Draft;
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public WarehouseTaskSourceType SourceType { get; set; } = WarehouseTaskSourceType.Manual;
        public Guid? SourceEntityId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public int LineCount { get; set; }
        public int RequestedQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public int ShortQuantity { get; set; }
        public bool HasShortage { get; set; }
    }

    public sealed class WarehouseTaskOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ReadyCount { get; set; }
        public int AssignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int NeedsAssignmentCount { get; set; }
        public int OverdueCount { get; set; }
        public int ShortageCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class WarehouseTaskCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public WarehouseTaskType TaskType { get; set; } = WarehouseTaskType.General;
        public WarehouseTaskStatus Status { get; set; } = WarehouseTaskStatus.Draft;
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public WarehouseTaskSourceType SourceType { get; set; } = WarehouseTaskSourceType.Manual;
        public Guid? SourceEntityId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<WarehouseTaskLineDto> Lines { get; set; } = new();
    }

    public class WarehouseTaskEditDto : WarehouseTaskCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class WarehouseTaskDetailDto : WarehouseTaskEditDto
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string? FromLocationCode { get; set; }
        public string? ToLocationCode { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public string? TaskNumber { get; set; }
        public DateTime? ReadyAtUtc { get; set; }
        public DateTime? AssignedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
    }

    public sealed class WarehouseTaskLineDto
    {
        public Guid Id { get; set; }
        public Guid? ProductVariantId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }
        public string? SkuSnapshot { get; set; }
        public string Description { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public int ShortQuantity { get; set; }
        public string? ShortReason { get; set; }
        public int SortOrder { get; set; }
        public string? SourceLineType { get; set; }
        public Guid? SourceLineId { get; set; }
        public string? MetadataJson { get; set; }
    }

    public sealed class WarehouseTaskLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public WarehouseTaskStatus TargetStatus { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class CreateWarehouseReceivingTaskFromGoodsReceiptDto
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public string? InternalNotes { get; set; }
    }

    public sealed class CreateWarehousePutawayTaskFromGoodsReceiptDto
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid ToLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public string? InternalNotes { get; set; }
    }

    public sealed class CreateWarehousePickingTaskFromOrderDto
    {
        public Guid OrderId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public string? InternalNotes { get; set; }
    }

    public sealed class StockCountListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? CountNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public StockCountType CountType { get; set; } = StockCountType.Cycle;
        public StockCountSessionStatus Status { get; set; } = StockCountSessionStatus.Draft;
        public DateTime? CountWindowStartUtc { get; set; }
        public DateTime? CountWindowEndUtc { get; set; }
        public int LineCount { get; set; }
        public int VarianceLineCount { get; set; }
        public int TotalExpectedQuantity { get; set; }
        public int TotalCountedQuantity { get; set; }
        public int TotalVarianceQuantity { get; set; }
    }

    public sealed class StockCountOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int InProgressCount { get; set; }
        public int ReviewPendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PostedCount { get; set; }
        public int VarianceCount { get; set; }
    }

    public class StockCountCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public StockCountType CountType { get; set; } = StockCountType.Cycle;
        public DateTime? CountWindowStartUtc { get; set; }
        public DateTime? CountWindowEndUtc { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<StockCountLineDto> Lines { get; set; } = new();
    }

    public class StockCountEditDto : StockCountCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class StockCountDetailDto : StockCountEditDto
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string? LocationCode { get; set; }
        public string? CountNumber { get; set; }
        public StockCountSessionStatus Status { get; set; } = StockCountSessionStatus.Draft;
        public DateTime? PreparedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CountedAtUtc { get; set; }
        public DateTime? ReviewRequestedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public sealed class StockCountLineDto
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? LocationId { get; set; }
        public string? SkuSnapshot { get; set; }
        public string Description { get; set; } = string.Empty;
        public int ExpectedQuantity { get; set; }
        public int CountedQuantity { get; set; }
        public int VarianceQuantity { get; set; }
        public StockCountLineReviewStatus ReviewStatus { get; set; } = StockCountLineReviewStatus.Pending;
        public bool AdjustmentPosted { get; set; }
        public string? ReviewNotes { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    public sealed class StockCountLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public StockCountSessionStatus TargetStatus { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Supplier list row for admin grids.
    /// </summary>
    public sealed class SupplierListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Code { get; set; }
        public string Status { get; set; } = "Active";
        public string? PreferredCurrency { get; set; }
        public int? PaymentTermDays { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Website { get; set; }
        public string? TaxRegistrationNumber { get; set; }
        public int PurchaseOrderCount { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class SupplierOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int MissingAddressCount { get; set; }
        public int HasPurchaseOrdersCount { get; set; }
        public int InactiveCount { get; set; }
        public int BlockedCount { get; set; }
    }

    /// <summary>
    /// Supplier create payload.
    /// </summary>
    public class SupplierCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Status { get; set; } = "Active";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public string? PreferredCurrency { get; set; }
        public int? PaymentTermDays { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Website { get; set; }
        public string? TaxRegistrationNumber { get; set; }
        public string? ExternalNotes { get; set; }
    }

    /// <summary>
    /// Supplier edit payload with concurrency token.
    /// </summary>
    public sealed class SupplierEditDto : SupplierCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<SupplierContactDto> Contacts { get; set; } = new();
        public List<SupplierDocumentDto> Documents { get; set; } = new();
    }

    public sealed class SupplierContactDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public SupplierContactRole Role { get; set; } = SupplierContactRole.General;
        public string Name { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class SupplierContactEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public SupplierContactRole Role { get; set; } = SupplierContactRole.General;
        public string Name { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class SupplierDocumentDto
    {
        public Guid Id { get; set; }
        public DocumentRecordKind DocumentKind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
        public string MetadataJson { get; set; } = "{}";
    }

    public sealed class SupplierDocumentRegisterDto
    {
        public Guid SupplierId { get; set; }
        public Guid BusinessId { get; set; }
        public DocumentRecordKind DocumentKind { get; set; } = DocumentRecordKind.Attachment;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public string? ContentHash { get; set; }
        public string StorageProvider { get; set; } = "External";
        public string StorageContainer { get; set; } = "supplier-documents";
        public string StorageKey { get; set; } = string.Empty;
        public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
        public string MetadataJson { get; set; } = "{}";
    }

    /// <summary>
    /// Stock level admin projection.
    /// </summary>
    public sealed class StockLevelListItemDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public int InTransitQuantity { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Stock level create payload.
    /// </summary>
    public class StockLevelCreateDto
    {
        public Guid WarehouseId { get; set; }
        public Guid ProductVariantId { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public int InTransitQuantity { get; set; }
    }

    /// <summary>
    /// Stock level edit payload with concurrency token.
    /// </summary>
    public sealed class StockLevelEditDto : StockLevelCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Stock transfer line payload.
    /// </summary>
    public sealed class StockTransferLineDto
    {
        public Guid ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Stock transfer list row.
    /// </summary>
    public sealed class StockTransferListItemDto
    {
        public Guid Id { get; set; }
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public string ToWarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsStale { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class StockTransferOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int InTransitCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int StaleInTransitCount { get; set; }
    }

    /// <summary>
    /// Stock transfer create payload.
    /// </summary>
    public class StockTransferCreateDto
    {
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public string Status { get; set; } = "Draft";
        public List<StockTransferLineDto> Lines { get; set; } = new();
    }

    /// <summary>
    /// Stock transfer edit payload with concurrency token.
    /// </summary>
    public sealed class StockTransferEditDto : StockTransferCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class StockTransferLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>
    /// Purchase order line payload.
    /// </summary>
    public sealed class PurchaseOrderLineDto
    {
        public Guid ProductVariantId { get; set; }
        public string? SupplierSku { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int CancelledQuantity { get; set; }
        public long UnitCostMinor { get; set; }
        public long TotalCostMinor { get; set; }
    }

    /// <summary>
    /// Purchase order list row.
    /// </summary>
    public sealed class PurchaseOrderListItemDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public Guid BusinessId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public DateTime OrderedAtUtc { get; set; }
        public DateTime? ExpectedDeliveryDateUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public DateTime? ReceivedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public int LineCount { get; set; }
        public int OrderedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public bool IsStale { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PurchaseOrderOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int IssuedCount { get; set; }
        public int ReceivedCount { get; set; }
        public int CancelledCount { get; set; }
        public int StaleIssuedCount { get; set; }
        public int PartiallyReceivedCount { get; set; }
    }

    /// <summary>
    /// Purchase order create payload.
    /// </summary>
    public class PurchaseOrderCreateDto
    {
        public Guid SupplierId { get; set; }
        public Guid BusinessId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderedAtUtc { get; set; }
        public string Currency { get; set; } = "EUR";
        public DateTime? ExpectedDeliveryDateUtc { get; set; }
        public string Status { get; set; } = "Draft";
        public string? InternalNotes { get; set; }
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    }

    /// <summary>
    /// Purchase order edit payload with concurrency token.
    /// </summary>
    public sealed class PurchaseOrderEditDto : PurchaseOrderCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PurchaseOrderLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
    }

    public sealed class GoodsReceiptLineDto
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderLineId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string? SupplierSku { get; set; }
        public string? Description { get; set; }
        public int OrderedQuantity { get; set; }
        public int PreviouslyReceivedQuantity { get; set; }
        public int RemainingQuantity => Math.Max(0, OrderedQuantity - PreviouslyReceivedQuantity);
        public int ReceivedQuantity { get; set; }
        public int AcceptedQuantity { get; set; }
        public int RejectedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
        public long UnitCostMinor { get; set; }
        public long TotalCostMinor { get; set; }
        public int SortOrder { get; set; }
    }

    public sealed class GoodsReceiptListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? GoodsReceiptNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ReceivedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public int LineCount { get; set; }
        public int ReceivedQuantity { get; set; }
        public int AcceptedQuantity { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class GoodsReceiptOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int ReceivedCount { get; set; }
        public int InspectedCount { get; set; }
        public int PostedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public sealed class GoodsReceiptCreateDto
    {
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string? InternalNotes { get; set; }
    }

    public sealed class GoodsReceiptDetailDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? GoodsReceiptNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ReceivedAtUtc { get; set; }
        public DateTime? InspectedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? InternalNotes { get; set; }
        public List<GoodsReceiptLineDto> Lines { get; set; } = new();
    }

    public sealed class GoodsReceiptLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
        public List<GoodsReceiptLineDto> Lines { get; set; } = new();
    }
}
