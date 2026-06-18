namespace Darwin.Domain.Enums;

public enum ExternalSystemKind
{
    Unknown = 0,
    Erp = 1,
    Crm = 2,
    Accounting = 3,
    Commerce = 4,
    PaymentProvider = 5,
    ShippingProvider = 6,
    IdentityProvider = 7,
    LoyaltyProvider = 8,
    Custom = 99
}

public enum ExternalReferenceKind
{
    Primary = 0,
    Alternate = 1,
    Import = 2,
    Export = 3,
    Provider = 4,
    Legacy = 5
}

public enum SourceOfTruth
{
    Unknown = 0,
    Darwin = 1,
    External = 2,
    Shared = 3
}

public enum SyncStateStatus
{
    NotSynced = 0,
    PendingOutbound = 1,
    PendingInbound = 2,
    Synced = 3,
    Failed = 4,
    Conflict = 5,
    Disabled = 6
}

public enum SyncDirection
{
    Unknown = 0,
    Inbound = 1,
    Outbound = 2,
    Bidirectional = 3
}

public enum SyncConflictStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Ignored = 3,
    Cancelled = 4
}

public enum SyncConflictResolution
{
    None = 0,
    UseDarwin = 1,
    UseExternal = 2,
    Merge = 3,
    Ignore = 4,
    Custom = 99
}
