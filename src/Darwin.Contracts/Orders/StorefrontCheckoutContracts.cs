namespace Darwin.Contracts.Orders;

/// <summary>
/// Checkout address snapshot supplied by the storefront when placing an order.
/// </summary>
public sealed class CheckoutAddress
{
    /// <summary>Gets or sets the recipient full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the first street line.</summary>
    public string Street1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second street line.</summary>
    public string? Street2 { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional state or region.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the ISO country code.</summary>
    public string CountryCode { get; set; } = "DE";

    /// <summary>Gets or sets the optional phone number in E.164 format.</summary>
    public string? PhoneE164 { get; set; }
}

/// <summary>
/// Request payload for placing an order from the current storefront cart.
/// </summary>
public sealed class PlaceOrderFromCartRequest
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the optional saved billing address identifier.</summary>
    public Guid? BillingAddressId { get; set; }

    /// <summary>Gets or sets the optional saved shipping address identifier.</summary>
    public Guid? ShippingAddressId { get; set; }

    /// <summary>Gets or sets the optional selected shipping method identifier.</summary>
    public Guid? SelectedShippingMethodId { get; set; }

    /// <summary>Gets or sets the inline billing address when no saved address is selected.</summary>
    public CheckoutAddress? BillingAddress { get; set; }

    /// <summary>Gets or sets the inline shipping address when no saved address is selected.</summary>
    public CheckoutAddress? ShippingAddress { get; set; }

    /// <summary>Gets or sets the shipping total in minor units selected by the client.</summary>
    public long ShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the culture used for localized product snapshots.</summary>
    public string? Culture { get; set; }
}

/// <summary>
/// Result payload returned after a storefront cart is placed as an order.
/// </summary>
public sealed class PlaceOrderFromCartResponse
{
    /// <summary>Gets or sets the created order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the order currency.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the initial order status.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for building a storefront checkout intent from the current cart and shipping address.
/// </summary>
public sealed class CreateCheckoutIntentRequest
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the optional saved shipping address identifier.</summary>
    public Guid? ShippingAddressId { get; set; }

    /// <summary>Gets or sets the optional inline shipping address snapshot.</summary>
    public CheckoutAddress? ShippingAddress { get; set; }

    /// <summary>Gets or sets the optional selected shipping method identifier.</summary>
    public Guid? SelectedShippingMethodId { get; set; }
}

/// <summary>
/// Response payload representing the authoritative storefront checkout preview.
/// </summary>
public sealed class CreateCheckoutIntentResponse
{
    /// <summary>Gets or sets the cart identifier.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the cart currency.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the cart subtotal net in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the cart VAT total in minor units.</summary>
    public long VatTotalMinor { get; set; }

    /// <summary>Gets or sets the cart grand total before shipping in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the computed shipment mass in grams.</summary>
    public int ShipmentMass { get; set; }

    /// <summary>Gets or sets a value indicating whether the checkout requires shipping.</summary>
    public bool RequiresShipping { get; set; }

    /// <summary>Gets or sets the ISO shipping country code used for rating.</summary>
    public string? ShippingCountryCode { get; set; }

    /// <summary>Gets or sets the selected shipping method identifier.</summary>
    public Guid? SelectedShippingMethodId { get; set; }

    /// <summary>Gets or sets the selected shipping total in minor units.</summary>
    public long SelectedShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the available shipping options.</summary>
    public IReadOnlyList<Darwin.Contracts.Shipping.PublicShippingOption> ShippingOptions { get; set; } = Array.Empty<Darwin.Contracts.Shipping.PublicShippingOption>();
}

/// <summary>
/// Request payload for creating or reusing a storefront payment intent for an order.
/// </summary>
public sealed class CreateStorefrontPaymentIntentRequest
{
    /// <summary>Gets or sets the order number required for anonymous storefront flows.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the desired payment provider label.</summary>
    public string? Provider { get; set; }
}

/// <summary>
/// Response payload returned after a storefront payment intent is created or reused.
/// </summary>
public sealed class CreateStorefrontPaymentIntentResponse
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the payment identifier.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>Gets or sets the payment provider label.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider/session reference associated with the intent.</summary>
    public string ProviderReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount in minor units.</summary>
    public long AmountMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the payment status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the intent expiration timestamp.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Storefront confirmation response returned after checkout or payment initiation.
/// </summary>
public sealed class StorefrontOrderConfirmationResponse
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the order subtotal net in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the order VAT total in minor units.</summary>
    public long TaxTotalMinor { get; set; }

    /// <summary>Gets or sets the order shipping total in minor units.</summary>
    public long ShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the selected shipping method identifier.</summary>
    public Guid? ShippingMethodId { get; set; }

    /// <summary>Gets or sets the shipping method display name snapshot.</summary>
    public string? ShippingMethodName { get; set; }

    /// <summary>Gets or sets the carrier snapshot.</summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>Gets or sets the service snapshot.</summary>
    public string? ShippingService { get; set; }

    /// <summary>Gets or sets the order discount total in minor units.</summary>
    public long DiscountTotalMinor { get; set; }

    /// <summary>Gets or sets the order grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the order status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized billing address snapshot.</summary>
    public string BillingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the serialized shipping address snapshot.</summary>
    public string ShippingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the order creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the storefront order lines.</summary>
    public IReadOnlyList<StorefrontOrderConfirmationLine> Lines { get; set; } = Array.Empty<StorefrontOrderConfirmationLine>();

    /// <summary>Gets or sets the associated payment attempts.</summary>
    public IReadOnlyList<StorefrontOrderConfirmationPayment> Payments { get; set; } = Array.Empty<StorefrontOrderConfirmationPayment>();
}

/// <summary>
/// Storefront-facing order line projection.
/// </summary>
public sealed class StorefrontOrderConfirmationLine
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
/// Storefront-facing payment projection shown on the confirmation screen.
/// </summary>
public sealed class StorefrontOrderConfirmationPayment
{
    /// <summary>Gets or sets the payment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the provider label.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider/session reference.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Gets or sets the amount in minor units.</summary>
    public long AmountMinor { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the payment status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the paid/captured timestamp in UTC.</summary>
    public DateTime? PaidAtUtc { get; set; }
}
