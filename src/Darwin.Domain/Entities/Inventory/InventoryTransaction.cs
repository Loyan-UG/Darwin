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
        /// Gets or sets purchase orders issued to this supplier.
        /// </summary>
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
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
        /// Gets or sets the purchase order status.
        /// </summary>
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

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
        /// Gets or sets the ordered quantity.
        /// </summary>
        public int Quantity { get; set; }

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
        /// Gets or sets the related product variant id using the legacy inventory naming convention.
        /// This alias is kept temporarily so existing inventory handlers can move to
        /// <see cref="ProductVariantId"/> incrementally without blocking the broader domain refactor.
        /// </summary>
        public Guid VariantId
        {
            get => ProductVariantId;
            set => ProductVariantId = value;
        }

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
