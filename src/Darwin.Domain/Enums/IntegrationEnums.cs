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
