namespace Darwin.Domain.Enums;

public enum CustomFieldDataType
{
    Text = 0,
    Number = 1,
    Boolean = 2,
    Date = 3,
    Json = 4
}

public enum FoundationVisibility
{
    Internal = 0,
    Staff = 1,
    Business = 2,
    Member = 3,
    Public = 4
}

public enum DocumentRecordKind
{
    General = 0,
    Attachment = 1,
    Evidence = 2,
    Contract = 3,
    InvoiceArtifact = 4,
    OrderDocument = 5,
    IdentityDocument = 6,
    StaffDocument = 7
}

public enum NumberSequenceDocumentType
{
    Order = 0,
    Invoice = 1,
    PurchaseOrder = 2,
    InventoryDocument = 3,
    FinanceDocument = 4,
    HrDocument = 5,
    SalesQuote = 6,
    DeliveryNote = 7,
    ReturnOrder = 8,
    CreditNote = 9,
    GoodsReceipt = 10,
    SupplierInvoice = 11,
    Custom = 99
}

public enum NumberSequenceResetPolicy
{
    Never = 0,
    Daily = 1,
    Monthly = 2,
    Yearly = 3
}

public enum BusinessEventSource
{
    System = 0,
    User = 1,
    Integration = 2,
    Automation = 3,
    Import = 4,
    BackgroundJob = 5
}

public enum BusinessEventSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum AuditTrailAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Restored = 3,
    StatusChanged = 4,
    Linked = 5,
    Unlinked = 6,
    Imported = 7,
    Exported = 8,
    Custom = 99
}

public enum FeatureAreaCategory
{
    Foundation = 0,
    CRM = 1,
    Sales = 2,
    Purchasing = 3,
    Inventory = 4,
    Finance = 5,
    HR = 6,
    Loyalty = 7,
    Commerce = 8,
    Documents = 9,
    Integrations = 10,
    AI = 11,
    Custom = 99
}

public enum FeatureAreaVisibilityScope
{
    Internal = 0,
    Staff = 1,
    Business = 2,
    Member = 3,
    Public = 4
}
