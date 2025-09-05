using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;


namespace Darwin.Domain.Entities.Orders
{
    /// <summary>
    /// Order aggregate capturing financial and fulfillment details.
    /// </summary>
    public sealed class Order : BaseEntity
    {
        /// <summary>Human-friendly order number (sequential/unique).</summary>
        public string OrderNumber { get; set; } = string.Empty;
        /// <summary>Optional user who placed the order; guest checkout otherwise.</summary>
        public Guid? UserId { get; set; }
        /// <summary>ISO currency code; phase 1 fixed to EUR.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Whether the catalog prices include tax at the time of order (snapshot for accurate totals).</summary>
        public bool PricesIncludeTax { get; set; } = false;
        /// <summary>Aggregate amounts (minor units): subtotal net, tax total, shipping, discount, grand total gross.</summary>
        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }
        /// <summary>Order status lifecycle.</summary>
        public OrderStatus Status { get; set; } = OrderStatus.Created;
        /// <summary>Billing address serialized (normalized) for snapshotting.</summary>
        public string BillingAddressJson { get; set; } = "{}";
        /// <summary>Shipping address serialized (normalized) for snapshotting.</summary>
        public string ShippingAddressJson { get; set; } = "{}";
        /// <summary>Order lines.</summary>
        public List<OrderLine> Lines { get; set; } = new();
        /// <summary>List of payment transactions related to this order.</summary>
        public List<Payment> Payments { get; set; } = new();
        /// <summary>List of shipments fulfilling this order.</summary>
        public List<Shipment> Shipments { get; set; } = new();
        /// <summary>Optional notes for internal staff.</summary>
        public string? InternalNotes { get; set; }
    }

    /// <summary>
    /// Order line snapshot of a variant with pricing and tax details at the time of purchase.
    /// </summary>
    public sealed class OrderLine : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid VariantId { get; set; }
        /// <summary>Display name at purchase time (copied from translation to preserve history).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>SKU at purchase time.</summary>
        public string Sku { get; set; } = string.Empty;
        /// <summary>Quantity purchased.</summary>
        public int Quantity { get; set; }
        /// <summary>Unit price net in minor units.</summary>
        public long UnitPriceNetMinor { get; set; }
        /// <summary>VAT rate applied (e.g., 0.19 for 19%).</summary>
        public decimal VatRate { get; set; }
        /// <summary>Computed unit price gross (net + tax) in minor units for convenience.</summary>
        public long UnitPriceGrossMinor { get; set; }
        /// <summary>Total tax for the line in minor units.</summary>
        public long LineTaxMinor { get; set; }
        /// <summary>Total gross for the line in minor units.</summary>
        public long LineGrossMinor { get; set; }
    }


    /// <summary>
    /// Payment transaction record for an order. Each attempt/authorization/capture is represented.
    /// </summary>
    public sealed class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        /// <summary>Logical provider name (e.g., PayPal, Stripe, SEPA). Used to route to the correct adapter.</summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>Provider-specific reference id (e.g., PayPal capture id) for reconciliation.</summary>
        public string ProviderReference { get; set; } = string.Empty;
        /// <summary>Amount in minor units. Usually equals the authorized/captured total.</summary>
        public long AmountMinor { get; set; }
        /// <summary>ISO currency code, e.g., EUR.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Processing status of this payment record.</summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
        /// <summary>Optional timestamp of capture completion.</summary>
        public DateTime? CapturedAtUtc { get; set; }
        /// <summary>Optional human-readable failure reason captured from provider.</summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Shipment record for an order, with tracking and status.
    /// </summary>
    public sealed class Shipment : BaseEntity
    {
        public Guid OrderId { get; set; }
        /// <summary>Carrier code/name (e.g., DHL).</summary>
        public string Carrier { get; set; } = string.Empty;
        /// <summary>Service level (e.g., Standard, Express).</summary>
        public string Service { get; set; } = string.Empty;
        /// <summary>Tracking number provided by the carrier.</summary>
        public string? TrackingNumber { get; set; }
        /// <summary>Total package weight in grams for this shipment.</summary>
        public int? TotalWeight { get; set; }
        /// <summary>Shipment lifecycle status.</summary>
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        /// <summary>UTC timestamps for shipped and delivered milestones.</summary>
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        /// <summary>Lines included in this shipment (quantities may be partial).</summary>
        public System.Collections.Generic.List<ShipmentLine> Lines { get; set; } = new();
    }


    /// <summary>
    /// Join entity mapping an order line into a shipment with a shipped quantity.
    /// </summary>
    public sealed class ShipmentLine : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderLineId { get; set; }
        /// <summary>Quantity of the order line shipped in this package.</summary>
        public int Quantity { get; set; }
    }


    /// <summary>
    /// Refund record referencing an order and optionally a specific payment.
    /// </summary>
    public sealed class Refund : BaseEntity
    {
        public Guid OrderId { get; set; }
        /// <summary>Optional payment this refund is tied to.</summary>
        public Guid? PaymentId { get; set; }
        /// <summary>Amount refunded in minor units.</summary>
        public long AmountMinor { get; set; }
        /// <summary>Optional reason provided by staff or system.</summary>
        public string? Reason { get; set; }
    }
}