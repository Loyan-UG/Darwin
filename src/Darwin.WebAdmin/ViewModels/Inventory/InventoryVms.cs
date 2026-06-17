using System;
using System.Collections.Generic;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

    public sealed class WarehouseLocationsListVm
    {
        public Guid? BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Query { get; set; } = string.Empty;
        public WarehouseLocationQueueFilter Filter { get; set; } = WarehouseLocationQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public WarehouseLocationOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<WarehouseLocationListItemVm> Items { get; set; } = new();
        public List<WarehouseLocationTreeItemVm> TreeItems { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class WarehouseLocationListItemVm
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

    public sealed class WarehouseLocationOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int BlockedCount { get; set; }
        public int BinCount { get; set; }
        public int DockCount { get; set; }
        public int QualityHoldCount { get; set; }
    }

    public sealed class WarehouseLocationEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        public Guid? ParentLocationId { get; set; }

        [Required]
        [StringLength(64)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        public WarehouseLocationType LocationType { get; set; } = WarehouseLocationType.Bin;
        public WarehouseLocationStatus Status { get; set; } = WarehouseLocationStatus.Active;

        [StringLength(128)]
        public string? Barcode { get; set; }

        [Range(0, int.MaxValue)]
        public int SortOrder { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public string WarehouseName { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> ParentLocationOptions { get; set; } = new();
        public List<SelectListItem> LocationTypeOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<WarehouseLocationTreeItemVm> Children { get; set; } = new();
    }

    public sealed class WarehouseLocationTreeItemVm
    {
        public Guid Id { get; set; }
        public Guid? ParentLocationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WarehouseLocationType LocationType { get; set; } = WarehouseLocationType.Bin;
        public WarehouseLocationStatus Status { get; set; } = WarehouseLocationStatus.Active;
        public int SortOrder { get; set; }
        public List<WarehouseLocationTreeItemVm> Children { get; set; } = new();
    }

    public sealed class ProductTrackingPoliciesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public ProductTrackingPolicyQueueFilter Filter { get; set; } = ProductTrackingPolicyQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public ProductTrackingPolicyOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<ProductTrackingPolicyListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class ProductTrackingPolicyListItemVm
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

    public sealed class ProductTrackingPolicyOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int TrackedCount { get; set; }
        public int RequiresExpiryCount { get; set; }
        public int RequiresHandlingUnitCount { get; set; }
    }

    public sealed class ProductTrackingPolicyEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        public string VariantSku { get; set; } = string.Empty;
        public ProductTrackingMode TrackingMode { get; set; } = ProductTrackingMode.Untracked;
        public ProductTrackingPolicyStatus Status { get; set; } = ProductTrackingPolicyStatus.Active;
        public bool RequiresSupplierLot { get; set; }
        public bool RequiresExpiryDate { get; set; }
        public bool RequiresHandlingUnit { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
        public List<SelectListItem> TrackingModeOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }

    public sealed class InventoryLotsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public InventoryLotQueueFilter Filter { get; set; } = InventoryLotQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public InventoryLotOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<InventoryLotListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class InventoryLotListItemVm
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

    public sealed class InventoryLotOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int QuarantinedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int RecalledCount { get; set; }
    }

    public sealed class InventoryLotEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        public string VariantSku { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LotCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SupplierLotCode { get; set; }

        public DateTime? ManufactureDateUtc { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public InventoryLotStatus Status { get; set; } = InventoryLotStatus.Draft;

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }

    public sealed class InventorySerialUnitsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public InventorySerialUnitQueueFilter Filter { get; set; } = InventorySerialUnitQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public InventorySerialUnitOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<InventorySerialUnitListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class InventorySerialUnitListItemVm
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

    public sealed class InventorySerialUnitOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int AvailableCount { get; set; }
        public int ReservedCount { get; set; }
        public int QuarantinedCount { get; set; }
        public int ScrappedCount { get; set; }
    }

    public sealed class InventorySerialUnitEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        public string VariantSku { get; set; } = string.Empty;
        public Guid? InventoryLotId { get; set; }
        public string? LotCode { get; set; }

        [Required]
        [StringLength(128)]
        public string SerialNumber { get; set; } = string.Empty;

        public DateTime? ManufactureDateUtc { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public InventorySerialUnitStatus Status { get; set; } = InventorySerialUnitStatus.Received;

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
        public List<SelectListItem> LotOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }

    public sealed class HandlingUnitsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public HandlingUnitQueueFilter Filter { get; set; } = HandlingUnitQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public HandlingUnitOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<HandlingUnitListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class HandlingUnitListItemVm
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

    public sealed class HandlingUnitOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int ClosedCount { get; set; }
        public int InTransitCount { get; set; }
        public int ReceivedCount { get; set; }
    }

    public sealed class HandlingUnitEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        public Guid? WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? ParentHandlingUnitId { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(128)]
        public string? Barcode { get; set; }

        public HandlingUnitType HandlingUnitType { get; set; } = HandlingUnitType.Pallet;
        public HandlingUnitStatus Status { get; set; } = HandlingUnitStatus.Open;

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public string? WarehouseName { get; set; }
        public string? LocationCode { get; set; }
        public string? ParentCode { get; set; }
        public List<HandlingUnitContentVm> Contents { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ParentHandlingUnitOptions { get; set; } = new();
        public List<SelectListItem> TypeOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
        public List<SelectListItem> LotOptions { get; set; } = new();
        public List<SelectListItem> SerialOptions { get; set; } = new();
    }

    public sealed class HandlingUnitContentVm
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }

        [StringLength(100)]
        public string? SkuSnapshot { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        public int SortOrder { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }
    }

    public sealed class WarehouseLabelTemplatesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public WarehouseLabelTemplateQueueFilter Filter { get; set; } = WarehouseLabelTemplateQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public WarehouseLabelTemplateOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<WarehouseLabelTemplateListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class WarehouseLabelTemplateListItemVm
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

    public sealed class WarehouseLabelTemplateOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int DefaultCount { get; set; }
    }

    public sealed class WarehouseLabelTemplateEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TemplateKey { get; set; } = string.Empty;

        public WarehouseLabelTemplateStatus Status { get; set; } = WarehouseLabelTemplateStatus.Active;
        public WarehouseLabelTemplateFormat Format { get; set; } = WarehouseLabelTemplateFormat.Html;
        public bool IsDefault { get; set; }

        [Range(10, 300)]
        public int WidthMm { get; set; } = 70;

        [Range(10, 300)]
        public int HeightMm { get; set; } = 35;

        [Required]
        [StringLength(8000)]
        public string ContentTemplate { get; set; } = "<strong>{Code}</strong><br>{DisplayName}<br>{Barcode}";

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> FormatOptions { get; set; } = new();
    }

    public sealed class WarehouseLocationLabelPrintVm
    {
        public Guid BusinessId { get; set; }
        public Guid TemplateId { get; set; }
        public List<SelectListItem> TemplateOptions { get; set; } = new();
        public List<WarehouseLocationListItemVm> Locations { get; set; } = new();
        public List<Guid> LocationIds { get; set; } = new();
        public WarehouseLocationLabelRenderVm? Render { get; set; }
    }

    public sealed class WarehouseLocationLabelRenderVm
    {
        public WarehouseLabelTemplateFormat Format { get; set; }
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }
        public List<WarehouseLocationLabelItemVm> Labels { get; set; } = new();
    }

    public sealed class WarehouseLocationLabelItemVm
    {
        public Guid LocationId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string RenderedContent { get; set; } = string.Empty;
    }

    public sealed class WarehouseTasksListVm
    {
        public Guid? BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Query { get; set; } = string.Empty;
        public WarehouseTaskQueueFilter Filter { get; set; } = WarehouseTaskQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public WarehouseTaskOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public WarehousePickingTaskCreateVm PickingTask { get; set; } = new();
        public List<WarehouseTaskListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class WarehousePickingTaskCreateVm
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid OrderId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public DateTime? DueAtUtc { get; set; }
        public string? InternalNotes { get; set; }
    }

    public sealed class WarehouseTaskListItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? FromLocationCode { get; set; }
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

    public sealed class WarehouseTaskOpsSummaryVm
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

    public sealed class WarehouseTaskEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? TaskNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public WarehouseTaskType TaskType { get; set; } = WarehouseTaskType.General;
        public WarehouseTaskStatus Status { get; set; } = WarehouseTaskStatus.Draft;
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public WarehouseTaskSourceType SourceType { get; set; } = WarehouseTaskSourceType.Manual;
        public Guid? SourceEntityId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public DateTime? ReadyAtUtc { get; set; }
        public DateTime? AssignedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }

        [StringLength(4000)]
        public string? InternalNotes { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }

        public string WarehouseName { get; set; } = string.Empty;
        public string? FromLocationCode { get; set; }
        public string? ToLocationCode { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public List<WarehouseTaskLineVm> Lines { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> TaskTypeOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
        public List<SelectListItem> SourceTypeOptions { get; set; } = new();
    }

    public sealed class WarehouseTaskLineVm
    {
        public Guid Id { get; set; }
        public Guid? ProductVariantId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }

        [StringLength(100)]
        public string? SkuSnapshot { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int RequestedQuantity { get; set; } = 1;

        [Range(0, int.MaxValue)]
        public int CompletedQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ShortQuantity { get; set; }

        [StringLength(1000)]
        public string? ShortReason { get; set; }

        public int SortOrder { get; set; }

        [StringLength(100)]
        public string? SourceLineType { get; set; }

        public Guid? SourceLineId { get; set; }

        [StringLength(8000)]
        public string? MetadataJson { get; set; }
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
        [ValidateNever]
        public List<SupplierContactVm> Contacts { get; set; } = new();

        [ValidateNever]
        public SupplierContactVm NewContact { get; set; } = new();

        [ValidateNever]
        public List<SupplierDocumentVm> Documents { get; set; } = new();

        [ValidateNever]
        public SupplierDocumentRegisterVm NewDocument { get; set; } = new();
    }

    public sealed class SupplierContactVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public SupplierContactRole Role { get; set; } = SupplierContactRole.General;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? JobTitle { get; set; }

        [EmailAddress]
        [StringLength(320)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(16)]
        public string? LanguageCode { get; set; }

        public bool IsPrimary { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public sealed class SupplierDocumentVm
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

    public sealed class SupplierDocumentRegisterVm
    {
        public Guid SupplierId { get; set; }
        public Guid BusinessId { get; set; }
        public DocumentRecordKind DocumentKind { get; set; } = DocumentRecordKind.Attachment;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(128)]
        public string ContentType { get; set; } = "application/octet-stream";

        [Range(0, long.MaxValue)]
        public long? SizeBytes { get; set; }

        [StringLength(256)]
        public string? ContentHash { get; set; }

        [Required]
        [StringLength(128)]
        public string StorageProvider { get; set; } = "External";

        [Required]
        [StringLength(128)]
        public string StorageContainer { get; set; } = "supplier-documents";

        [Required]
        [StringLength(1024)]
        public string StorageKey { get; set; } = string.Empty;

        public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;

        [StringLength(4000)]
        public string MetadataJson { get; set; } = "{}";
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

    public sealed class StockCountsListVm
    {
        public Guid? BusinessId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Query { get; set; } = string.Empty;
        public StockCountQueueFilter Filter { get; set; } = StockCountQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public StockCountOpsSummaryVm Summary { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<StockCountListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class StockCountListItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
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

    public sealed class StockCountOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int InProgressCount { get; set; }
        public int ReviewPendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PostedCount { get; set; }
        public int VarianceCount { get; set; }
    }

    public sealed class StockCountEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? CountNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public StockCountType CountType { get; set; } = StockCountType.Cycle;
        public StockCountSessionStatus Status { get; set; } = StockCountSessionStatus.Draft;
        public DateTime? CountWindowStartUtc { get; set; }
        public DateTime? CountWindowEndUtc { get; set; }
        public DateTime? PreparedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CountedAtUtc { get; set; }
        public DateTime? ReviewRequestedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? LocationCode { get; set; }

        [StringLength(4000)]
        public string? ReviewNotes { get; set; }

        [StringLength(4000)]
        public string? InternalNotes { get; set; }

        public string? MetadataJson { get; set; }
        public List<StockCountLineVm> Lines { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> CountTypeOptions { get; set; } = new();
        public List<SelectListItem> ReviewStatusOptions { get; set; } = new();
    }

    public sealed class StockCountLineVm
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        public Guid? LocationId { get; set; }

        [StringLength(100)]
        public string? SkuSnapshot { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int ExpectedQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int CountedQuantity { get; set; }

        public int VarianceQuantity { get; set; }
        public StockCountLineReviewStatus ReviewStatus { get; set; } = StockCountLineReviewStatus.Pending;
        public bool AdjustmentPosted { get; set; }

        [StringLength(1000)]
        public string? ReviewNotes { get; set; }

        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
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
        public GoodsReceiptWarehouseTaskVm WarehouseTaskAction { get; set; } = new();
        public List<SelectListItem> PutawayLocationOptions { get; set; } = new();
        public List<GoodsReceiptLineVm> Lines { get; set; } = new();
    }

    public sealed class GoodsReceiptWarehouseTaskVm
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ToLocationId { get; set; }
        public WarehouseTaskPriority Priority { get; set; } = WarehouseTaskPriority.Normal;
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public string? InternalNotes { get; set; }
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
