namespace Darwin.Domain.Enums
{
    /// <summary>Publication status for CMS pages.</summary>
    public enum PageStatus { Draft = 0, Published = 1 }


    /// <summary>Payment processing state.</summary>
    public enum PaymentStatus { Initiated = 0, Authorized = 1, Captured = 2, Failed = 3, Refunded = 4, PartiallyRefunded = 5 }


    /// <summary>Shipment lifecycle state.</summary>
    public enum ShipmentStatus { Pending = 0, Packed = 1, Shipped = 2, Delivered = 3, Returned = 4 }


    /// <summary>Promotion reward type.</summary>
    public enum PromotionType { Percentage = 0, Amount = 1 }


    /// <summary>Product kind; Phase 1 uses Simple/Variant.</summary>
    public enum ProductKind { Simple = 0, Variant = 1, Bundle = 2, Digital = 3, Service = 4 }

    /// <summary>
    /// Order lifecycle. Designed to cover common e-commerce flows and partial operations.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Order exists but not yet confirmed by business rules (stock/fraud/etc.).</summary>
        Created = 0,

        /// <summary>Confirmed/accepted by the system or staff; awaiting payment.</summary>
        Confirmed = 1,

        /// <summary>Payment captured/settled sufficiently to proceed with fulfillment.</summary>
        Paid = 2,

        /// <summary>Some, but not all, lines or quantities have been shipped.</summary>
        PartiallyShipped = 3,

        /// <summary>All items shipped.</summary>
        Shipped = 4,

        /// <summary>Delivered to customer (all shipped items delivered).</summary>
        Delivered = 5,

        /// <summary>Business cancellation (before shipment) or voided.</summary>
        Cancelled = 6,

        /// <summary>Full order refunded (financially settled as refund).</summary>
        Refunded = 7,

        /// <summary>One or more lines/quantities refunded but not the entire order.</summary>
        PartiallyRefunded = 8
    }
}