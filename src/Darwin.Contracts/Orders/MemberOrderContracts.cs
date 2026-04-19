using Darwin.Contracts.Common;

namespace Darwin.Contracts.Orders;

/// <summary>
/// Member-facing summary of an order in order history screens.
/// </summary>
public sealed class MemberOrderSummary
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the current order status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Member-facing detailed order representation.
/// </summary>
public sealed class MemberOrderDetail
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets a value indicating whether prices include tax.</summary>
    public bool PricesIncludeTax { get; set; }

    /// <summary>Gets or sets the subtotal net amount in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the tax total in minor units.</summary>
    public long TaxTotalMinor { get; set; }

    /// <summary>Gets or sets the shipping total in minor units.</summary>
    public long ShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the selected shipping method identifier.</summary>
    public Guid? ShippingMethodId { get; set; }

    /// <summary>Gets or sets the shipping method display name snapshot.</summary>
    public string? ShippingMethodName { get; set; }

    /// <summary>Gets or sets the carrier snapshot.</summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>Gets or sets the service snapshot.</summary>
    public string? ShippingService { get; set; }

    /// <summary>Gets or sets the discount total in minor units.</summary>
    public long DiscountTotalMinor { get; set; }

    /// <summary>Gets or sets the grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the current order status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized billing address snapshot.</summary>
    public string BillingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the serialized shipping address snapshot.</summary>
    public string ShippingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the purchased line snapshots.</summary>
    public IReadOnlyList<MemberOrderLine> Lines { get; set; } = Array.Empty<MemberOrderLine>();

    /// <summary>Gets or sets the payment snapshots.</summary>
    public IReadOnlyList<MemberOrderPayment> Payments { get; set; } = Array.Empty<MemberOrderPayment>();

    /// <summary>Gets or sets the shipment snapshots.</summary>
    public IReadOnlyList<MemberOrderShipment> Shipments { get; set; } = Array.Empty<MemberOrderShipment>();

    /// <summary>Gets or sets the linked invoice snapshots.</summary>
    public IReadOnlyList<MemberOrderInvoice> Invoices { get; set; } = Array.Empty<MemberOrderInvoice>();

    /// <summary>Gets or sets the available member actions for this order.</summary>
    public MemberOrderActions Actions { get; set; } = new();
}

/// <summary>
/// Member-facing action metadata for an order detail screen.
/// </summary>
public sealed class MemberOrderActions
{
    /// <summary>Gets or sets a value indicating whether the current member may retry payment for the order.</summary>
    public bool CanRetryPayment { get; set; }

    /// <summary>Gets or sets the canonical API path for creating a payment intent, when available.</summary>
    public string? PaymentIntentPath { get; set; }

    /// <summary>Gets or sets the canonical API path for the order confirmation projection.</summary>
    public string ConfirmationPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the canonical API path for downloading an order document.</summary>
    public string DocumentPath { get; set; } = string.Empty;
}

/// <summary>
/// Member-facing order line snapshot.
/// </summary>
public sealed class MemberOrderLine
{
    /// <summary>Gets or sets the order line identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the purchased variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the display name captured at purchase time.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the purchased quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit gross amount in minor units.</summary>
    public long UnitPriceGrossMinor { get; set; }

    /// <summary>Gets or sets the line gross amount in minor units.</summary>
    public long LineGrossMinor { get; set; }
}

/// <summary>
/// Member-facing order payment snapshot.
/// </summary>
public sealed class MemberOrderPayment
{
    /// <summary>Gets or sets the payment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp for the payment attempt.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the payment provider name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional provider transaction reference.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Gets or sets the payment amount in minor units.</summary>
    public long AmountMinor { get; set; }

    /// <summary>Gets or sets the payment currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the payment status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the paid timestamp in UTC, when available.</summary>
    public DateTime? PaidAtUtc { get; set; }
}

/// <summary>
/// Member-facing order shipment snapshot.
/// </summary>
public sealed class MemberOrderShipment
{
    /// <summary>Gets or sets the shipment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the carrier name or code.</summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>Gets or sets the service level.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the shipment status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC shipped timestamp, when available.</summary>
    public DateTime? ShippedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC delivered timestamp, when available.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
}

/// <summary>
/// Member-facing invoice snapshot linked to an order.
/// </summary>
public sealed class MemberOrderInvoice
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the invoice currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the invoice total in minor units.</summary>
    public long TotalGrossMinor { get; set; }

    /// <summary>Gets or sets the invoice status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC due date.</summary>
    public DateTime DueDateUtc { get; set; }

    /// <summary>Gets or sets the UTC paid timestamp, when available.</summary>
    public DateTime? PaidAtUtc { get; set; }
}
