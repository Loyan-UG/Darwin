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
}
