using Darwin.Contracts.Common;

namespace Darwin.Contracts.Shipping;

/// <summary>
/// Request payload for storefront shipping-rate quotes.
/// </summary>
public sealed class PublicShippingRateRequest
{
    /// <summary>Gets or sets the ISO 3166-1 alpha-2 destination country code.</summary>
    public string Country { get; set; } = ContractDefaults.DefaultCountryCode;

    /// <summary>Gets or sets the shipment subtotal net amount in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the total shipment mass.</summary>
    public int ShipmentMass { get; set; }

    /// <summary>Gets or sets the optional ISO currency code override.</summary>
    public string? Currency { get; set; }
}

/// <summary>
/// Storefront shipping option returned by the shipping quote endpoint.
/// </summary>
public sealed class PublicShippingOption
{
    /// <summary>Gets or sets the shipping method identifier.</summary>
    public Guid MethodId { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the shipping price in minor units.</summary>
    public long PriceMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the carrier code or name.</summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>Gets or sets the service level name.</summary>
    public string Service { get; set; } = string.Empty;
}
