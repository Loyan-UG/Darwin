namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle state of a warehouse-to-warehouse transfer.
    /// </summary>
    public enum TransferStatus : short
    {
        Draft = 0,
        InTransit = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Represents the lifecycle state of a purchase order.
    /// </summary>
    public enum PurchaseOrderStatus : short
    {
        Draft = 0,
        Issued = 1,
        Received = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Represents the lifecycle state of a formal supplier goods receipt.
    /// </summary>
    public enum GoodsReceiptStatus : short
    {
        Draft = 0,
        Received = 1,
        Inspected = 2,
        Posted = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Represents the operational type of a structured warehouse location.
    /// </summary>
    public enum WarehouseLocationType : short
    {
        Zone = 0,
        Aisle = 1,
        Rack = 2,
        Shelf = 3,
        Bin = 4,
        Staging = 5,
        Dock = 6,
        QualityHold = 7
    }

    /// <summary>
    /// Represents whether a structured warehouse location is usable for operations.
    /// </summary>
    public enum WarehouseLocationStatus : short
    {
        Active = 0,
        Inactive = 1,
        Blocked = 2
    }

    /// <summary>
    /// Represents how a product variant must be identified during inventory execution.
    /// </summary>
    public enum ProductTrackingMode : short
    {
        Untracked = 0,
        LotTracked = 1,
        SerialTracked = 2,
        LotAndExpiryTracked = 3,
        SerialAndExpiryTracked = 4
    }

    /// <summary>
    /// Represents whether an inventory tracking policy can be enforced by future movement handlers.
    /// </summary>
    public enum ProductTrackingPolicyStatus : short
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>
    /// Represents lifecycle state for reusable inventory lot identity.
    /// </summary>
    public enum InventoryLotStatus : short
    {
        Draft = 0,
        Active = 1,
        Quarantined = 2,
        Expired = 3,
        Recalled = 4,
        Closed = 5
    }

    /// <summary>
    /// Represents lifecycle state for unique inventory serial identity.
    /// </summary>
    public enum InventorySerialUnitStatus : short
    {
        Received = 0,
        Available = 1,
        Reserved = 2,
        Picked = 3,
        Shipped = 4,
        Quarantined = 5,
        Scrapped = 6
    }

    /// <summary>
    /// Represents warehouse execution type for a handling unit.
    /// </summary>
    public enum HandlingUnitType : short
    {
        Pallet = 0,
        Carton = 1,
        Tote = 2,
        Case = 3,
        Container = 4,
        Other = 5
    }

    /// <summary>
    /// Represents lifecycle state for grouped-stock handling units.
    /// </summary>
    public enum HandlingUnitStatus : short
    {
        Open = 0,
        Closed = 1,
        InTransit = 2,
        Received = 3,
        BrokenDown = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Represents whether a warehouse label template can be used for new prints/downloads.
    /// </summary>
    public enum WarehouseLabelTemplateStatus : short
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>
    /// Represents the provider-neutral rendering format of a warehouse label template.
    /// </summary>
    public enum WarehouseLabelTemplateFormat : short
    {
        Html = 0,
        Text = 1,
        Json = 2
    }

    /// <summary>
    /// Represents the operational kind of a warehouse execution task.
    /// </summary>
    public enum WarehouseTaskType : short
    {
        Receiving = 0,
        Putaway = 1,
        Picking = 2,
        Packing = 3,
        TransferPick = 4,
        TransferPutaway = 5,
        StockCount = 6,
        Replenishment = 7,
        General = 8
    }

    /// <summary>
    /// Represents the lifecycle state of a warehouse task.
    /// </summary>
    public enum WarehouseTaskStatus : short
    {
        Draft = 0,
        Ready = 1,
        Assigned = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Represents operator priority for warehouse task queues.
    /// </summary>
    public enum WarehouseTaskPriority : short
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    /// <summary>
    /// Identifies the source aggregate type that created or owns task demand.
    /// </summary>
    public enum WarehouseTaskSourceType : short
    {
        Manual = 0,
        GoodsReceipt = 1,
        StockTransfer = 2,
        Order = 3,
        ReturnOrder = 4,
        StockCount = 5
    }

    /// <summary>
    /// Represents the operational type of a stock count session.
    /// </summary>
    public enum StockCountType : short
    {
        Full = 0,
        Cycle = 1,
        Location = 2,
        Product = 3
    }

    /// <summary>
    /// Represents the lifecycle state of a stock count session.
    /// </summary>
    public enum StockCountSessionStatus : short
    {
        Draft = 0,
        Prepared = 1,
        InProgress = 2,
        Counted = 3,
        ReviewPending = 4,
        Approved = 5,
        Posted = 6,
        Rejected = 7,
        Cancelled = 8
    }

    /// <summary>
    /// Represents review state for an individual stock count line.
    /// </summary>
    public enum StockCountLineReviewStatus : short
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2
    }

    /// <summary>
    /// Represents the operational purchasing status of a supplier.
    /// </summary>
    public enum SupplierStatus : short
    {
        Active = 0,
        Inactive = 1,
        Blocked = 2
    }

    /// <summary>
    /// Represents the role of a structured supplier contact.
    /// </summary>
    public enum SupplierContactRole : short
    {
        General = 0,
        Purchasing = 1,
        AccountsPayable = 2,
        Logistics = 3,
        Quality = 4,
        Technical = 5
    }
}
