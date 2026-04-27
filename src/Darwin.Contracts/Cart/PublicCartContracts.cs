using Darwin.Contracts.Common;

namespace Darwin.Contracts.Cart;

/// <summary>
/// Request payload for adding or increasing a storefront cart line.
/// </summary>
public sealed class PublicCartAddItemRequest
{
    /// <summary>Gets or sets the anonymous storefront cart identifier when no member token exists.</summary>
    public string? AnonymousId { get; set; }

    /// <summary>Gets or sets the product variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the desired quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the optional client-side unit price snapshot in minor units.</summary>
    public long? UnitPriceNetMinor { get; set; }

    /// <summary>Gets or sets the optional client-side VAT snapshot.</summary>
    public decimal? VatRate { get; set; }

    /// <summary>Gets or sets the optional ISO currency code.</summary>
    public string? Currency { get; set; }

    /// <summary>Gets or sets the selected add-on identifiers.</summary>
    public IReadOnlyList<Guid> SelectedAddOnValueIds { get; set; } = Array.Empty<Guid>();
}

/// <summary>
/// Request payload for changing a storefront cart line quantity.
/// </summary>
public sealed class PublicCartUpdateItemRequest
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the product variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the optional serialized add-on selection key.</summary>
    public string? SelectedAddOnValueIdsJson { get; set; }

    /// <summary>Gets or sets the desired quantity. Zero removes the line.</summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Request payload for removing a storefront cart line.
/// </summary>
public sealed class PublicCartRemoveItemRequest
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the product variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the optional serialized add-on selection key.</summary>
    public string? SelectedAddOnValueIdsJson { get; set; }
}

/// <summary>
/// Request payload for applying or clearing a storefront coupon code.
/// </summary>
public sealed class PublicCartApplyCouponRequest
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the coupon code. Null or empty clears the current coupon.</summary>
    public string? CouponCode { get; set; }
}

/// <summary>
/// Storefront cart summary returned to public and member-facing front-office clients.
/// </summary>
public sealed class PublicCartSummary
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the cart currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the cart lines.</summary>
    public IReadOnlyList<PublicCartItemRow> Items { get; set; } = Array.Empty<PublicCartItemRow>();

    /// <summary>Gets or sets the cart subtotal net amount in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the cart VAT total in minor units.</summary>
    public long VatTotalMinor { get; set; }

    /// <summary>Gets or sets the cart grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the active coupon code, if any.</summary>
    public string? CouponCode { get; set; }
}

/// <summary>
/// Storefront cart line summary.
/// </summary>
public sealed class PublicCartItemRow
{
    /// <summary>Gets or sets the product variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price net in minor units.</summary>
    public long UnitPriceNetMinor { get; set; }

    /// <summary>Gets or sets the add-on delta in minor units.</summary>
    public long AddOnPriceDeltaMinor { get; set; }

    /// <summary>Gets or sets the VAT rate snapshot.</summary>
    public decimal VatRate { get; set; }

    /// <summary>Gets or sets the line net amount in minor units.</summary>
    public long LineNetMinor { get; set; }

    /// <summary>Gets or sets the line VAT amount in minor units.</summary>
    public long LineVatMinor { get; set; }

    /// <summary>Gets or sets the line gross amount in minor units.</summary>
    public long LineGrossMinor { get; set; }

    /// <summary>Gets or sets the serialized add-on selection key.</summary>
    public string SelectedAddOnValueIdsJson { get; set; } = "[]";

    /// <summary>Gets or sets localized selected add-on labels for display.</summary>
    public IReadOnlyList<PublicCartSelectedAddOn> SelectedAddOns { get; set; } = Array.Empty<PublicCartSelectedAddOn>();
}

/// <summary>
/// Localized selected add-on display row for a cart line.
/// </summary>
public sealed class PublicCartSelectedAddOn
{
    public Guid ValueId { get; set; }
    public Guid OptionId { get; set; }
    public string OptionLabel { get; set; } = string.Empty;
    public string ValueLabel { get; set; } = string.Empty;
    public long PriceDeltaMinor { get; set; }
}
