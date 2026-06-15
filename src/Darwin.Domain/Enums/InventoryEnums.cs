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
    /// Represents the operational purchasing status of a supplier.
    /// </summary>
    public enum SupplierStatus : short
    {
        Active = 0,
        Inactive = 1,
        Blocked = 2
    }
}
