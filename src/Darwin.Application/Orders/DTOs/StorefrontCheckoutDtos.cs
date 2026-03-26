using Darwin.Domain.Enums;

namespace Darwin.Application.Orders.DTOs;

/// <summary>
/// Input used to preview storefront checkout totals, shipping options, and address readiness.
/// </summary>
public sealed class CreateStorefrontCheckoutIntentDto
{
    /// <summary>Gets or sets the cart identifier being checked out.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the optional current member identifier.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the optional saved shipping address identifier.</summary>
    public Guid? ShippingAddressId { get; set; }

    /// <summary>Gets or sets the optional inline shipping address snapshot.</summary>
    public CheckoutAddressDto? ShippingAddress { get; set; }

    /// <summary>Gets or sets the optional shipping method selected by the storefront.</summary>
    public Guid? SelectedShippingMethodId { get; set; }
}

/// <summary>
/// Storefront checkout preview containing the authoritative cart summary and shipping choices.
/// </summary>
public sealed class StorefrontCheckoutIntentResultDto
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the cart currency.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the subtotal net amount in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the VAT total in minor units.</summary>
    public long VatTotalMinor { get; set; }

    /// <summary>Gets or sets the cart grand total before shipping in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the computed physical shipment mass in grams.</summary>
    public int ShipmentMass { get; set; }

    /// <summary>Gets or sets a value indicating whether at least one physical line requires shipping.</summary>
    public bool RequiresShipping { get; set; }

    /// <summary>Gets or sets the ISO country code used to rate shipping.</summary>
    public string? ShippingCountryCode { get; set; }

    /// <summary>Gets or sets the selected shipping method identifier.</summary>
    public Guid? SelectedShippingMethodId { get; set; }

    /// <summary>Gets or sets the selected shipping price in minor units.</summary>
    public long SelectedShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the available storefront shipping options.</summary>
    public List<StorefrontShippingOptionDto> ShippingOptions { get; set; } = new();
}

/// <summary>
/// Storefront-facing shipping option returned by checkout intent.
/// </summary>
public sealed class StorefrontShippingOptionDto
{
    /// <summary>Gets or sets the shipping method identifier.</summary>
    public Guid MethodId { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the shipping price in minor units.</summary>
    public long PriceMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the carrier name or code.</summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>Gets or sets the service level.</summary>
    public string Service { get; set; } = string.Empty;
}

/// <summary>
/// Input used to create or reuse a storefront payment intent for an existing order.
/// </summary>
public sealed class CreateStorefrontPaymentIntentDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the optional current member identifier.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the order number required for anonymous confirmation/payment flows.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the requested storefront payment provider label.</summary>
    public string Provider { get; set; } = "DarwinCheckout";
}

/// <summary>
/// Result returned after a storefront payment intent is created or reused.
/// </summary>
public sealed class StorefrontPaymentIntentResultDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the payment identifier.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>Gets or sets the storefront payment provider label.</summary>
    public string Provider { get; set; } = "DarwinCheckout";

    /// <summary>Gets or sets the provider/session reference associated with the payment attempt.</summary>
    public string ProviderReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount in minor units.</summary>
    public long AmountMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the payment status.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Gets or sets the expiration timestamp for the intent.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Query input used to resolve a storefront order-confirmation view.
/// </summary>
public sealed class GetStorefrontOrderConfirmationDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the optional current member identifier.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the order number required for anonymous order confirmations.</summary>
    public string? OrderNumber { get; set; }
}

/// <summary>
/// Storefront confirmation projection returned after order placement and payment initiation.
/// </summary>
public sealed class StorefrontOrderConfirmationDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the order subtotal net in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the order tax total in minor units.</summary>
    public long TaxTotalMinor { get; set; }

    /// <summary>Gets or sets the order shipping total in minor units.</summary>
    public long ShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the order discount total in minor units.</summary>
    public long DiscountTotalMinor { get; set; }

    /// <summary>Gets or sets the grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the order lifecycle status.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Created;

    /// <summary>Gets or sets the serialized billing address snapshot.</summary>
    public string BillingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the serialized shipping address snapshot.</summary>
    public string ShippingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the storefront order lines.</summary>
    public List<StorefrontOrderConfirmationLineDto> Lines { get; set; } = new();

    /// <summary>Gets or sets the associated payment attempts.</summary>
    public List<StorefrontOrderConfirmationPaymentDto> Payments { get; set; } = new();
}

/// <summary>
/// Storefront-facing order line confirmation snapshot.
/// </summary>
public sealed class StorefrontOrderConfirmationLineDto
{
    /// <summary>Gets or sets the line identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the captured line name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit gross amount in minor units.</summary>
    public long UnitPriceGrossMinor { get; set; }

    /// <summary>Gets or sets the line gross amount in minor units.</summary>
    public long LineGrossMinor { get; set; }
}

/// <summary>
/// Storefront-facing payment confirmation snapshot.
/// </summary>
public sealed class StorefrontOrderConfirmationPaymentDto
{
    /// <summary>Gets or sets the payment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the provider name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider reference.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Gets or sets the payment amount in minor units.</summary>
    public long AmountMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the payment status.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Gets or sets the paid/captured timestamp in UTC.</summary>
    public DateTime? PaidAtUtc { get; set; }
}
