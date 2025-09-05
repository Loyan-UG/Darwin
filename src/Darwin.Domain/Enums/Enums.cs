namespace Darwin.Domain.Enums
{
    /// <summary>Publication status for CMS pages.</summary>
    public enum PageStatus { Draft = 0, Published = 1 }


    /// <summary>Order lifecycle states.</summary>
    public enum OrderStatus { Created = 0, Confirmed = 1, Paid = 2, Shipped = 3, Cancelled = 4, Refunded = 5 }


    /// <summary>Payment processing state.</summary>
    public enum PaymentStatus { Initiated = 0, Authorized = 1, Captured = 2, Failed = 3, Refunded = 4, PartiallyRefunded = 5 }


    /// <summary>Shipment lifecycle state.</summary>
    public enum ShipmentStatus { Pending = 0, Packed = 1, Shipped = 2, Delivered = 3, Returned = 4 }


    /// <summary>Promotion reward type.</summary>
    public enum PromotionType { Percentage = 0, Amount = 1 }


    /// <summary>Product kind; Phase 1 uses Simple/Variant.</summary>
    public enum ProductKind { Simple = 0, Variant = 1, Bundle = 2, Digital = 3, Service = 4 }
}