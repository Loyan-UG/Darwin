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

    public enum DeliveryNoteStatus
    {
        Draft = 0,
        Prepared = 1,
        Issued = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public enum ReturnOrderStatus
    {
        Requested = 0,
        Approved = 1,
        Rejected = 2,
        ReturnShipmentQueued = 3,
        Received = 4,
        Inspected = 5,
        RefundReady = 6,
        Refunded = 7,
        Closed = 8,
        Cancelled = 9
    }

    public enum ReturnInspectionDisposition
    {
        NotInspected = 0,
        AcceptedForRefund = 1,
        Rejected = 2,
        Restock = 3,
        Scrap = 4,
        Damaged = 5,
        Mixed = 6
    }

    public enum CreditNoteStatus
    {
        Draft = 0,
        Issued = 1,
        Voided = 2,
        Cancelled = 3
    }

    public enum CreditNoteReason
    {
        PostIssueCorrection = 0,
        AcceptedReturn = 1,
        CommercialCredit = 2,
        CancellationCorrection = 3
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

    /// <summary>Sales origin used for reporting and operational segmentation.</summary>
    public enum SalesChannel
    {
        Unknown = 0,
        WebStorefront = 1,
        Admin = 2,
        Import = 3,
        Api = 4
    }

    /// <summary>Internal sales quote lifecycle.</summary>
    public enum SalesQuoteStatus
    {
        Draft = 0,
        Sent = 1,
        Accepted = 2,
        Rejected = 3,
        Expired = 4,
        Converted = 5
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

    /// <summary>CRM customer lifecycle status for reporting and follow-up queues.</summary>
    public enum CustomerLifecycleStatus
    {
        Active = 0,
        Prospect = 1,
        Inactive = 2,
        Archived = 3
    }

    /// <summary>Shared CRM priority used by records that need operator triage.</summary>
    public enum CrmPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    /// <summary>Preferred customer contact channel for CRM follow-up.</summary>
    public enum PreferredContactChannel
    {
        Email = 0,
        Phone = 1,
        Sms = 2,
        Chat = 3,
        InPerson = 4
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

    /// <summary>Forecast category used for CRM pipeline reporting.</summary>
    public enum OpportunityForecastCategory
    {
        Pipeline = 0,
        BestCase = 1,
        Commit = 2,
        Omitted = 3,
        Closed = 4
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
