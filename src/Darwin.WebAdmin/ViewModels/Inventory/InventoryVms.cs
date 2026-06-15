using System;
using System.Collections.Generic;
using Darwin.Application.Inventory.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.Inventory
{
    /// <summary>
    /// Lightweight view model representing a single ledger row for display in Admin.
    /// This mirrors the projection returned by the Inventory ledger query
    /// (fields are kept generic to avoid tight coupling to the entity).
    /// </summary>
    public sealed class InventoryLedgerItemVm
    {
        /// <summary>Associated warehouse identifier.</summary>
        public Guid WarehouseId { get; set; }

        /// <summary>Warehouse display name.</summary>
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>Associated product variant identifier.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Signed quantity delta; positive means stock added, negative means stock removed.</summary>
        public int QuantityDelta { get; set; }

        /// <summary>Reason label stored with the transaction (e.g., "OrderPaid-Reserve").</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Optional correlation identifier (e.g., OrderId or ReturnId) to achieve idempotency and traceability.</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Creation timestamp (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Paged list view model for browsing the inventory ledger.
    /// Provides optional filters (by variant, date range) to narrow down results.
    /// </summary>
    public sealed class InventoryLedgerListVm
    {
        /// <summary>Optional filter by variant id.</summary>
        public Guid? VariantId { get; set; }

        /// <summary>Optional filter by warehouse id.</summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>Optional start date (UTC) filter.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Optional end date (UTC) filter.</summary>
        public DateTime? ToUtc { get; set; }

        public InventoryLedgerQueueFilter Filter { get; set; } = InventoryLedgerQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public InventoryLedgerOpsSummaryVm Summary { get; set; } = new();
        public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();

        /// <summary>Current page items.</summary>
        public List<InventoryLedgerItemVm> Items { get; set; } = new();

        /// <summary>1-based page number.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Items per page.</summary>
        public int PageSize { get; set; } = 20;

        /// <summary>Total number of matching rows.</summary>
        public int Total { get; set; }

        /// <summary>Drop-down items for page size selection.</summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    public sealed class InventoryLedgerOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int InboundCount { get; set; }
        public int OutboundCount { get; set; }
        public int ReservationCount { get; set; }
    }

    public sealed class InventoryOpsPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary snapshot for a product variant stock, used in Admin for quick inspection.
    /// </summary>
    public sealed class VariantStockSummaryVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>On-hand stock (physically available on shelf).</summary>
        public int StockOnHand { get; set; }

        /// <summary>Reserved stock (blocked for orders not yet shipped).</summary>
        public int StockReserved { get; set; }

        /// <summary>Computed availability (on-hand minus reserved).</summary>
        public int Available => StockOnHand - StockReserved;

        /// <summary>Concurrency token for optimistic concurrency when exposing inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Form view model for manual stock adjustments (increase/decrease on-hand).
    /// Aligns with InventoryAdjustDto in the Application layer.
    /// </summary>
    public sealed class InventoryAdjustVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Delta to apply to on-hand stock (positive or negative).</summary>
        public int QuantityDelta { get; set; }

        /// <summary>Reason label (e.g., "ManualAdjustment", "StockCount", "SupplierReceipt").</summary>
        public string Reason { get; set; } = "ManualAdjustment";

        /// <summary>Optional correlation id (e.g., a count-session id) to make the operation idempotent.</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>
    /// Form view model for reserving stock (does not change on-hand).
    /// Aligns with InventoryReserveDto in the Application layer.
    /// </summary>
    public sealed class InventoryReserveVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Quantity to reserve (must be positive).</summary>
        public int Quantity { get; set; }

        /// <summary>Reason label (e.g., "ManualReserve", "OrderPaid-Reserve").</summary>
        public string Reason { get; set; } = "ManualReserve";

        /// <summary>Optional correlation id to avoid duplicate reservations.</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>
    /// Form view model for releasing a reservation (does not change on-hand).
    /// Aligns with InventoryReleaseReservationDto in the Application layer.
    /// </summary>
    public sealed class InventoryReleaseReservationVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Quantity to release (must be positive).</summary>
        public int Quantity { get; set; }

        /// <summary>Reason label (e.g., "ManualRelease", "OrderCancelled-Release").</summary>
        public string Reason { get; set; } = "ManualRelease";

        /// <summary>Optional correlation id to avoid duplicate releases.</summary>
        public Guid? ReferenceId { get; set; }
    }

    public sealed class WarehousesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public WarehouseQueueFilter Filter { get; set; } = WarehouseQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public WarehouseOpsSummaryVm Summary { get; set; } = new();
        public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<WarehouseListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class WarehouseListItemVm
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

    public sealed class WarehouseOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DefaultCount { get; set; }
        public int NoStockLevelsCount { get; set; }
    }

    public sealed class WarehouseEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        public bool IsDefault { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
    }

    public sealed class SuppliersListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public SupplierQueueFilter Filter { get; set; } = SupplierQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public SupplierOpsSummaryVm Summary { get; set; } = new();
        public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SupplierListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class SupplierListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Status { get; set; } = "Active";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PreferredCurrency { get; set; }
        public int? PaymentTermDays { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Website { get; set; }
        public string? TaxRegistrationNumber { get; set; }
        public int PurchaseOrderCount { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class SupplierOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int MissingAddressCount { get; set; }
        public int HasPurchaseOrdersCount { get; set; }
        public int InactiveCount { get; set; }
        public int BlockedCount { get; set; }
    }

    public sealed class SupplierEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Code { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string? PreferredCurrency { get; set; }

        [Range(0, 3650)]
        public int? PaymentTermDays { get; set; }

        [Range(0, 3650)]
        public int? LeadTimeDays { get; set; }

        [StringLength(500)]
        public string? Website { get; set; }

        [StringLength(100)]
        public string? TaxRegistrationNumber { get; set; }

        [StringLength(2000)]
        public string? ExternalNotes { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
    }

    public sealed class StockLevelsListVm
    {
        public Guid? WarehouseId { get; set; }
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public StockLevelQueueFilter Filter { get; set; } = StockLevelQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<StockLevelListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class StockLevelListItemVm
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

    public sealed class StockLevelEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid WarehouseId { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        [Range(0, int.MaxValue)]
        public int AvailableQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReservedQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderPoint { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int InTransitQuantity { get; set; }

        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
    }

    public class InventoryStockActionVm
    {
        public Guid StockLevelId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public Guid? ReferenceId { get; set; }
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
    }

    public sealed class InventoryAdjustActionVm : InventoryStockActionVm
    {
        [Range(-1000000, 1000000)]
        public int QuantityDelta { get; set; }

        [Required]
        [StringLength(120)]
        public string Reason { get; set; } = "ManualAdjustment";
    }

    public sealed class InventoryReserveActionVm : InventoryStockActionVm
    {
        [Range(1, 1000000)]
        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(120)]
        public string Reason { get; set; } = "ManualReserve";
    }

    public sealed class InventoryReleaseReservationActionVm : InventoryStockActionVm
    {
        [Range(1, 1000000)]
        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(120)]
        public string Reason { get; set; } = "ManualRelease";
    }

    public sealed class InventoryReturnReceiptActionVm : InventoryStockActionVm
    {
        [Range(1, 1000000)]
        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(120)]
        public string Reason { get; set; } = "ReturnReceipt";
    }

    public sealed class StockTransfersListVm
    {
        public Guid? BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Query { get; set; } = string.Empty;
        public StockTransferQueueFilter Filter { get; set; } = StockTransferQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public StockTransferOpsSummaryVm Summary { get; set; } = new();
        public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<StockTransferListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class StockTransferListItemVm
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

    public sealed class StockTransferOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int InTransitCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int StaleInTransitCount { get; set; }
    }

    public sealed class StockTransferLineVm
    {
        [Required]
        public Guid ProductVariantId { get; set; }

        [StringLength(100)]
        public string? SupplierSku { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0, int.MaxValue)]
        public int ReceivedQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int CancelledQuantity { get; set; }
    }

    public sealed class StockTransferEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid FromWarehouseId { get; set; }

        [Required]
        public Guid ToWarehouseId { get; set; }

        [Required]
        public string Status { get; set; } = "Draft";

        public List<StockTransferLineVm> Lines { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
    }

    public sealed class PurchaseOrdersListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public PurchaseOrderQueueFilter Filter { get; set; } = PurchaseOrderQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public PurchaseOrderOpsSummaryVm Summary { get; set; } = new();
        public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<PurchaseOrderListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class PurchaseOrderListItemVm
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

    public sealed class PurchaseOrderOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int IssuedCount { get; set; }
        public int ReceivedCount { get; set; }
        public int CancelledCount { get; set; }
        public int StaleIssuedCount { get; set; }
        public int PartiallyReceivedCount { get; set; }
    }

    public sealed class PurchaseOrderLineVm
    {
        [Required]
        public Guid ProductVariantId { get; set; }

        [StringLength(100)]
        public string? SupplierSku { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0, int.MaxValue)]
        public int ReceivedQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int CancelledQuantity { get; set; }

        [Range(0, long.MaxValue)]
        public long UnitCostMinor { get; set; }

        [Range(0, long.MaxValue)]
        public long TotalCostMinor { get; set; }
    }

    public sealed class PurchaseOrderEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid SupplierId { get; set; }

        [Required]
        public Guid BusinessId { get; set; }

        [StringLength(64)]
        public string? OrderNumber { get; set; }

        public DateTime OrderedAtUtc { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        [Required]
        public string Status { get; set; } = "Draft";

        [StringLength(4000)]
        public string? InternalNotes { get; set; }

        public List<PurchaseOrderLineVm> Lines { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> SupplierOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
    }

    public sealed class GoodsReceiptsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public GoodsReceiptQueueFilter Filter { get; set; } = GoodsReceiptQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public GoodsReceiptOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> PurchaseOrderOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<GoodsReceiptListItemVm> Items { get; set; } = new();
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string? InternalNotes { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class GoodsReceiptListItemVm
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

    public sealed class GoodsReceiptOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int ReceivedCount { get; set; }
        public int InspectedCount { get; set; }
        public int PostedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public sealed class GoodsReceiptDetailVm
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
        public List<GoodsReceiptLineVm> Lines { get; set; } = new();
    }

    public sealed class GoodsReceiptLineVm
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderLineId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string? SupplierSku { get; set; }
        public string? Description { get; set; }
        public int OrderedQuantity { get; set; }
        public int PreviouslyReceivedQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        [Range(0, int.MaxValue)]
        public int ReceivedQuantity { get; set; }
        [Range(0, int.MaxValue)]
        public int AcceptedQuantity { get; set; }
        [Range(0, int.MaxValue)]
        public int RejectedQuantity { get; set; }
        [Range(0, int.MaxValue)]
        public int DamagedQuantity { get; set; }
        public long UnitCostMinor { get; set; }
        public long TotalCostMinor { get; set; }
        public int SortOrder { get; set; }
    }
}
