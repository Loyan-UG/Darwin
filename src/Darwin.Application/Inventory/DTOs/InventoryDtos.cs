using System;

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

    public enum SupplierQueueFilter
    {
        All = 0,
        MissingAddress = 1,
        HasPurchaseOrders = 2
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
        public int PurchaseOrderCount { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class SupplierOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int MissingAddressCount { get; set; }
        public int HasPurchaseOrdersCount { get; set; }
    }

    /// <summary>
    /// Supplier create payload.
    /// </summary>
    public class SupplierCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Supplier edit payload with concurrency token.
    /// </summary>
    public sealed class SupplierEditDto : SupplierCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
        public int Quantity { get; set; }
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
        public DateTime OrderedAtUtc { get; set; }
        public int LineCount { get; set; }
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
        public string Status { get; set; } = "Draft";
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
}
