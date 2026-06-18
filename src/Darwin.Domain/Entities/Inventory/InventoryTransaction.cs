using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Inventory
{
    /// <summary>
    /// Represents a warehouse that stores stock for a specific business tenant.
    /// </summary>
    public sealed class Warehouse : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional operator-facing description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the optional location description or address summary.
        /// Do not use this as a trusted geocoding source without validation.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this warehouse is the business default.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets the stock levels tracked in this warehouse.
        /// </summary>
        public List<StockLevel> StockLevels { get; set; } = new();

        /// <summary>
        /// Gets or sets structured zones, bins, docks, and other warehouse locations.
        /// </summary>
        public List<WarehouseLocation> Locations { get; set; } = new();
    }

    /// <summary>
    /// Represents a structured warehouse location or bin in a single hierarchy.
    /// </summary>
    public sealed class WarehouseLocation : BaseEntity
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
        public List<WarehouseLocation> Children { get; set; } = new();
    }

    /// <summary>
    /// Inventory-owned tracking policy for a product variant.
    /// </summary>
    public sealed class ProductTrackingPolicy : BaseEntity
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

    /// <summary>
    /// Reusable lot identity for quantity-tracked stock.
    /// </summary>
    public sealed class InventoryLot : BaseEntity
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

    /// <summary>
    /// Unique serial identity for exact-unit stock.
    /// </summary>
    public sealed class InventorySerialUnit : BaseEntity
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

    /// <summary>
    /// Warehouse execution identity for grouped stock such as pallets, cartons, totes, or cases.
    /// </summary>
    public sealed class HandlingUnit : BaseEntity
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
        public List<HandlingUnit> Children { get; set; } = new();
        public List<HandlingUnitContent> Contents { get; set; } = new();
    }

    /// <summary>
    /// Product quantity or exact-unit content inside a handling unit.
    /// </summary>
    public sealed class HandlingUnitContent : BaseEntity
    {
        public Guid HandlingUnitId { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public int Quantity { get; set; }
        public string? SkuSnapshot { get; set; }
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Provider-neutral label template for warehouse locations and bins.
    /// </summary>
    public sealed class WarehouseLabelTemplate : BaseEntity
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

    /// <summary>
    /// Formal internal warehouse execution task for review and later PWA execution.
    /// </summary>
    public sealed class WarehouseTask : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? TaskNumber { get; set; }
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
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<WarehouseTaskLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Product, location, quantity, and source-line evidence for a warehouse task.
    /// </summary>
    public sealed class WarehouseTaskLine : BaseEntity
    {
        public Guid WarehouseTaskId { get; set; }
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
        public List<WarehouseTaskLineIdentity> Identities { get; set; } = new();
    }

    /// <summary>
    /// Lot, serial, and handling-unit evidence captured while executing a warehouse task line.
    /// </summary>
    public sealed class WarehouseTaskLineIdentity : BaseEntity
    {
        public Guid WarehouseTaskLineId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public Guid? HandlingUnitId { get; set; }
        public int Quantity { get; set; }
        public string? LotCodeSnapshot { get; set; }
        public string? SupplierLotCodeSnapshot { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public string? SerialNumberSnapshot { get; set; }
        public string? HandlingUnitCodeSnapshot { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Formal internal stock count session for count evidence, variance review, and adjustment posting.
    /// </summary>
    public sealed class StockCountSession : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? CountNumber { get; set; }
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
        public string? ReviewNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<StockCountLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Product and location count evidence for a formal stock count session.
    /// </summary>
    public sealed class StockCountLine : BaseEntity
    {
        public Guid StockCountSessionId { get; set; }
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
        public List<StockCountLineIdentity> Identities { get; set; } = new();
    }

    /// <summary>
    /// Lot, serial, and handling-unit evidence captured for a stock count line.
    /// </summary>
    public sealed class StockCountLineIdentity : BaseEntity
    {
        public Guid StockCountLineId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public Guid? HandlingUnitId { get; set; }
        public int Quantity { get; set; }
        public string? LotCodeSnapshot { get; set; }
        public string? SupplierLotCodeSnapshot { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public string? SerialNumberSnapshot { get; set; }
        public string? HandlingUnitCodeSnapshot { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Represents the stock state of a single product variant in a warehouse.
    /// </summary>
    public sealed class StockLevel : BaseEntity
    {
        /// <summary>
        /// Gets or sets the warehouse id.
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the product variant id.
        /// </summary>
        public Guid ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the currently available quantity.
        /// </summary>
        public int AvailableQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity reserved by pending carts or orders.
        /// </summary>
        public int ReservedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the reorder threshold.
        /// </summary>
        public int ReorderPoint { get; set; }

        /// <summary>
        /// Gets or sets the recommended replenishment quantity.
        /// </summary>
        public int ReorderQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity currently in transit between warehouses or from suppliers.
        /// </summary>
        public int InTransitQuantity { get; set; }
    }

    /// <summary>
    /// Represents a stock transfer between warehouses.
    /// </summary>
    public sealed class StockTransfer : BaseEntity
    {
        /// <summary>
        /// Gets or sets the source warehouse id.
        /// </summary>
        public Guid FromWarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the destination warehouse id.
        /// </summary>
        public Guid ToWarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the transfer lifecycle status.
        /// </summary>
        public TransferStatus Status { get; set; } = TransferStatus.Draft;

        /// <summary>
        /// Gets or sets the transfer lines.
        /// </summary>
        public List<StockTransferLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents a single product variant line in a stock transfer.
    /// </summary>
    public sealed class StockTransferLine : BaseEntity
    {
        /// <summary>
        /// Gets or sets the stock transfer id.
        /// </summary>
        public Guid StockTransferId { get; set; }

        /// <summary>
        /// Gets or sets the product variant id.
        /// </summary>
        public Guid ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the transfer quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the lot, serial, and handling-unit evidence dispatched and received with this transfer line.
        /// </summary>
        public List<StockTransferLineIdentity> Identities { get; set; } = new();
    }

    /// <summary>
    /// Lot, serial, and handling-unit evidence captured for a stock transfer line.
    /// </summary>
    public sealed class StockTransferLineIdentity : BaseEntity
    {
        public Guid StockTransferLineId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public Guid? HandlingUnitId { get; set; }
        public int Quantity { get; set; }
        public string? LotCodeSnapshot { get; set; }
        public string? SupplierLotCodeSnapshot { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public string? SerialNumberSnapshot { get; set; }
        public string? HandlingUnitCodeSnapshot { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Represents a supplier used for replenishment and purchasing workflows.
    /// </summary>
    public sealed class Supplier : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the supplier name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional business-scoped supplier code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the supplier status.
        /// </summary>
        public SupplierStatus Status { get; set; } = SupplierStatus.Active;

        /// <summary>
        /// Gets or sets the supplier email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the supplier phone number.
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional supplier address summary.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets optional internal notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the preferred purchase currency code.
        /// </summary>
        public string? PreferredCurrency { get; set; }

        /// <summary>
        /// Gets or sets the default payment term in days.
        /// </summary>
        public int? PaymentTermDays { get; set; }

        /// <summary>
        /// Gets or sets the default lead time in days.
        /// </summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// Gets or sets the supplier website.
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Gets or sets the supplier tax registration number.
        /// </summary>
        public string? TaxRegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets optional external-facing supplier notes.
        /// </summary>
        public string? ExternalNotes { get; set; }

        /// <summary>
        /// Gets or sets purchase orders issued to this supplier.
        /// </summary>
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();

        /// <summary>
        /// Gets or sets structured contacts for purchasing, payables, logistics, and quality workflows.
        /// </summary>
        public List<SupplierContact> Contacts { get; set; } = new();
    }

    /// <summary>
    /// Represents a structured supplier contact owned by the supplier master record.
    /// </summary>
    public sealed class SupplierContact : BaseEntity
    {
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

    /// <summary>
    /// Represents a purchase order issued to a supplier.
    /// </summary>
    public sealed class PurchaseOrder : BaseEntity
    {
        /// <summary>
        /// Gets or sets the supplier id.
        /// </summary>
        public Guid SupplierId { get; set; }

        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the business-facing purchase order number.
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order timestamp in UTC.
        /// </summary>
        public DateTime OrderedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the purchase order currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Gets or sets the expected delivery date in UTC.
        /// </summary>
        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the issued timestamp in UTC.
        /// </summary>
        public DateTime? IssuedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the received timestamp in UTC.
        /// </summary>
        public DateTime? ReceivedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the cancelled timestamp in UTC.
        /// </summary>
        public DateTime? CancelledAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the purchase order status.
        /// </summary>
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        /// <summary>
        /// Gets or sets optional internal notes.
        /// </summary>
        public string? InternalNotes { get; set; }

        /// <summary>
        /// Gets or sets the purchase order lines.
        /// </summary>
        public List<PurchaseOrderLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents a single line on a purchase order.
    /// </summary>
    public sealed class PurchaseOrderLine : BaseEntity
    {
        /// <summary>
        /// Gets or sets the purchase order id.
        /// </summary>
        public Guid PurchaseOrderId { get; set; }

        /// <summary>
        /// Gets or sets the product variant id being procured.
        /// </summary>
        public Guid ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the supplier-facing SKU snapshot.
        /// </summary>
        public string? SupplierSku { get; set; }

        /// <summary>
        /// Gets or sets the purchase line description snapshot.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the ordered quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity received against this purchase order line.
        /// </summary>
        public int ReceivedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity cancelled against this purchase order line.
        /// </summary>
        public int CancelledQuantity { get; set; }

        /// <summary>
        /// Gets or sets the unit cost in minor units.
        /// </summary>
        public long UnitCostMinor { get; set; }

        /// <summary>
        /// Gets or sets the total line cost in minor units.
        /// </summary>
        public long TotalCostMinor { get; set; }
    }

    /// <summary>
    /// Represents a formal receipt of supplier goods against a purchase order.
    /// </summary>
    public sealed class GoodsReceipt : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string? GoodsReceiptNumber { get; set; }
        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
        public DateTime? ReceivedAtUtc { get; set; }
        public DateTime? InspectedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<GoodsReceiptLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents a received purchase order line snapshot and inspection outcome.
    /// </summary>
    public sealed class GoodsReceiptLine : BaseEntity
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid PurchaseOrderLineId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string? SupplierSku { get; set; }
        public string? Description { get; set; }
        public int OrderedQuantity { get; set; }
        public int PreviouslyReceivedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int AcceptedQuantity { get; set; }
        public int RejectedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
        public long UnitCostMinor { get; set; }
        public long TotalCostMinor { get; set; }
        public int SortOrder { get; set; }
        public List<GoodsReceiptLineIdentity> Identities { get; set; } = new();
    }

    /// <summary>
    /// Represents structured lot, serial, and handling-unit evidence captured for a goods receipt line.
    /// </summary>
    public sealed class GoodsReceiptLineIdentity : BaseEntity
    {
        public Guid GoodsReceiptLineId { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid? InventoryLotId { get; set; }
        public Guid? InventorySerialUnitId { get; set; }
        public Guid? HandlingUnitId { get; set; }
        public int Quantity { get; set; }
        public string? LotCodeSnapshot { get; set; }
        public string? SupplierLotCodeSnapshot { get; set; }
        public string? SerialNumberSnapshot { get; set; }
        public string? HandlingUnitCodeSnapshot { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Represents a stock movement event for inventory history and reconciliation.
    /// </summary>
    public sealed class InventoryTransaction : BaseEntity
    {
        /// <summary>
        /// Gets or sets the related warehouse id.
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the related product variant id whose stock changed.
        /// </summary>
        public Guid ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the signed quantity delta applied to available stock.
        /// </summary>
        public int QuantityDelta { get; set; }

        /// <summary>
        /// Gets or sets the reason or system code.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional reference id such as an order, transfer, or purchase order id.
        /// </summary>
        public Guid? ReferenceId { get; set; }
    }
}
