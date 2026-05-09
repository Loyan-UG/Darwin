namespace Darwin.Domain.Enums
{
    /// <summary>Publication status for CMS pages.</summary>
    public enum PageStatus
    {
        Draft = 0,
        Published = 1
    }

    /// <summary>Payment processing state used across order and invoice payments.</summary>
    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 1,
        Captured = 2,
        Completed = 3,
        Failed = 4,
        Refunded = 5,
        Voided = 6
    }

    /// <summary>Shipment lifecycle state.</summary>
    public enum ShipmentStatus
    {
        Pending = 0,
        Packed = 1,
        Shipped = 2,
        Delivered = 3,
        Returned = 4
    }

    /// <summary>Promotion reward type.</summary>
    public enum PromotionType
    {
        Percent = 0,
        Amount = 1,

        // Legacy alias.
        Percentage = Percent
    }

    /// <summary>Product kind.</summary>
    public enum ProductKind
    {
        Simple = 0,
        Variant = 1,
        Bundle = 2,
        Digital = 3,
        Service = 4
    }

    /// <summary>Order lifecycle.</summary>
    public enum OrderStatus
    {
        Created = 0,
        Confirmed = 1,
        Paid = 2,
        PartiallyShipped = 3,
        Shipped = 4,
        Delivered = 5,
        Cancelled = 6,
        Refunded = 7,
        PartiallyRefunded = 8,
        Completed = 9
    }

    /// <summary>Selection mode enum for a catalog add-on group.</summary>
    public enum AddOnSelectionMode
    {
        Single = 0,
        Multiple = 1
    }

    /// <summary>Interaction type in CRM activity log.</summary>
    public enum InteractionType
    {
        Email = 0,
        Call = 1,
        Meeting = 2,
        Order = 3,
        Support = 4
    }

    /// <summary>Interaction channel in CRM activity log.</summary>
    public enum InteractionChannel
    {
        Email = 0,
        Phone = 1,
        Chat = 2,
        InPerson = 3
    }

    /// <summary>Consent categories for GDPR and preferences.</summary>
    public enum ConsentType
    {
        MarketingEmail = 0,
        Sms = 1,
        TermsOfService = 2
    }

    /// <summary>CRM lead lifecycle status.</summary>
    public enum LeadStatus
    {
        New = 0,
        Qualified = 1,
        Disqualified = 2,
        Converted = 3
    }

    /// <summary>CRM opportunity progression stage.</summary>
    public enum OpportunityStage
    {
        Qualification = 0,
        Proposal = 1,
        Negotiation = 2,
        ClosedWon = 3,
        ClosedLost = 4
    }

    /// <summary>CRM customer tax profile used for B2C/B2B support and invoice context.</summary>
    public enum CustomerTaxProfileType
    {
        Consumer = 0,
        Business = 1
    }

    /// <summary>Operator or provider-backed VAT validation state for business customers.</summary>
    public enum CustomerVatValidationStatus
    {
        Unknown = 0,
        Valid = 1,
        Invalid = 2,
        NotApplicable = 3
    }

    /// <summary>Invoice lifecycle status.</summary>
    public enum InvoiceStatus
    {
        Draft = 0,
        Open = 1,
        Paid = 2,
        Cancelled = 3
    }

    /// <summary>Refund lifecycle status.</summary>
    public enum RefundStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }

    /// <summary>Loyalty redemption status.</summary>
    public enum RedemptionStatus
    {
        Pending = 0,
        Completed = 1,
        Cancelled = 2
    }
}
