using Darwin.Domain.Enums;

namespace Darwin.Application.Orders.DTOs;

/// <summary>
/// Summary projection used by member-facing order history screens.
/// </summary>
public sealed class MemberOrderSummaryDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the order grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the current order lifecycle status.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Created;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Detailed member-facing order projection used by order detail screens.
/// </summary>
public sealed class MemberOrderDetailDto
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-friendly order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets a value indicating whether prices include tax.</summary>
    public bool PricesIncludeTax { get; set; }

    /// <summary>Gets or sets the order subtotal net amount in minor units.</summary>
    public long SubtotalNetMinor { get; set; }

    /// <summary>Gets or sets the tax total in minor units.</summary>
    public long TaxTotalMinor { get; set; }

    /// <summary>Gets or sets the shipping total in minor units.</summary>
    public long ShippingTotalMinor { get; set; }

    /// <summary>Gets or sets the optional selected shipping method identifier.</summary>
    public Guid? ShippingMethodId { get; set; }

    /// <summary>Gets or sets the shipping method display name snapshot.</summary>
    public string? ShippingMethodName { get; set; }

    /// <summary>Gets or sets the shipping carrier snapshot.</summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>Gets or sets the shipping service snapshot.</summary>
    public string? ShippingService { get; set; }

    /// <summary>Gets or sets the discount total in minor units.</summary>
    public long DiscountTotalMinor { get; set; }

    /// <summary>Gets or sets the grand total in minor units.</summary>
    public long GrandTotalGrossMinor { get; set; }

    /// <summary>Gets or sets the current order lifecycle status.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Created;

    /// <summary>Gets or sets the serialized billing address snapshot.</summary>
    public string BillingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the serialized shipping address snapshot.</summary>
    public string ShippingAddressJson { get; set; } = "{}";

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the order line snapshots.</summary>
    public List<MemberOrderLineDto> Lines { get; set; } = new();

    /// <summary>Gets or sets the payment snapshots associated with the order.</summary>
    public List<MemberOrderPaymentDto> Payments { get; set; } = new();

    /// <summary>Gets or sets the shipment snapshots associated with the order.</summary>
    public List<MemberOrderShipmentDto> Shipments { get; set; } = new();

    /// <summary>Gets or sets the invoice snapshots associated with the order.</summary>
    public List<MemberOrderInvoiceDto> Invoices { get; set; } = new();
}

/// <summary>
/// Member-facing snapshot of a purchased order line.
/// </summary>
public sealed class MemberOrderLineDto
{
    /// <summary>Gets or sets the order line identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the purchased variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets or sets the display name captured at purchase time.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured SKU value.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the purchased quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit gross amount in minor units.</summary>
    public long UnitPriceGrossMinor { get; set; }

    /// <summary>Gets or sets the line gross amount in minor units.</summary>
    public long LineGrossMinor { get; set; }
}

/// <summary>
/// Member-facing snapshot of an order payment.
/// </summary>
public sealed class MemberOrderPaymentDto
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

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the payment status.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Gets or sets the UTC timestamp when the payment was marked paid/captured.</summary>
    public DateTime? PaidAtUtc { get; set; }
}

/// <summary>
/// Member-facing snapshot of an order shipment.
/// </summary>
public sealed class MemberOrderShipmentDto
{
    /// <summary>Gets or sets the shipment identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the carrier name or code.</summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>Gets or sets the service level.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the shipment lifecycle status.</summary>
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

    /// <summary>Gets or sets the UTC shipped timestamp, when available.</summary>
    public DateTime? ShippedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC delivered timestamp, when available.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
}

/// <summary>
/// Member-facing snapshot of an invoice linked to an order.
/// </summary>
public sealed class MemberOrderInvoiceDto
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the invoice currency.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the invoice total in minor units.</summary>
    public long TotalGrossMinor { get; set; }

    /// <summary>Gets or sets the invoice lifecycle status.</summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>Gets or sets the invoice due date in UTC.</summary>
    public DateTime DueDateUtc { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the invoice was paid.</summary>
    public DateTime? PaidAtUtc { get; set; }
}
