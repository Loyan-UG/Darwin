using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Orders.DTOs
{
    /// <summary>
    /// Input for creating an order. Totals are computed server-side from lines.
    /// </summary>
    public sealed class OrderCreateDto
    {
        public Guid? UserId { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool PricesIncludeTax { get; set; } = false;

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLineCreateDto> Lines { get; set; } = new();

        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
    }

    /// <summary>
    /// A line to be included in a new order.
    /// </summary>
    public sealed class OrderLineCreateDto
    {
        public Guid VariantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; } // 0.19m -> 19%
    }

    /// <summary>
    /// Lightweight list item used by Admin orders grid.
    /// Includes RowVersion for concurrency-sensitive actions (e.g., Delete/Cancel).
    /// </summary>
    public sealed class OrderListItemDto
    {
        /// <summary>Order id (primary key).</summary>
        public Guid Id { get; set; }

        /// <summary>Human-friendly order number (unique among non-deleted rows).</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>3-letter ISO currency code.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Grand total gross (minor units, e.g., cents).</summary>
        public long GrandTotalGrossMinor { get; set; }

        /// <summary>Current status in the order state machine.</summary>
        public OrderStatus Status { get; set; }

        /// <summary>Creation timestamp (UTC) for display/sorting.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token to enable optimistic concurrency in Admin actions.
        /// Typical usage: pass into hidden field and verify on command execution.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Detailed order view for admin.
    /// </summary>
    public sealed class OrderDetailDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool PricesIncludeTax { get; set; }

        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }

        public OrderStatus Status { get; set; }

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLineDetailDto> Lines { get; set; } = new();
        public List<PaymentDetailDto> Payments { get; set; } = new();
        public List<ShipmentDetailDto> Shipments { get; set; } = new();
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class OrderLineDetailDto
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public long UnitPriceGrossMinor { get; set; }
        public long LineTaxMinor { get; set; }
        public long LineGrossMinor { get; set; }
    }

    /// <summary>
    /// Input DTO for recording a payment attempt or result against an order.
    /// The status is captured from the payment provider callback/flow.
    /// </summary>
    public sealed class PaymentCreateDto
    {
        /// <summary>
        /// Target order identifier the payment belongs to.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Payment provider key (e.g., "Stripe", "PayPal", "Adyen").
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Provider-side reference/identifier to correlate with webhooks/callbacks.
        /// </summary>
        public string ProviderReference { get; set; } = string.Empty;

        /// <summary>
        /// Amount in minor currency units (e.g., cents).
        /// </summary>
        public long AmountMinor { get; set; }

        /// <summary>
        /// ISO 4217 currency (e.g., "EUR").
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Resulting status of the payment, as persisted in the domain.
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// Optional machine- or human-readable failure reason when Status == Failed.
        /// </summary>
        public string? FailureReason { get; set; }
    }

    public sealed class PaymentDetailDto
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderReference { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public PaymentStatus Status { get; set; }
        public DateTime? CapturedAtUtc { get; set; }
        public string? FailureReason { get; set; }
    }

    public sealed class ShipmentCreateDto
    {
        public Guid OrderId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public int? TotalWeight { get; set; }
        public List<ShipmentLineCreateDto> Lines { get; set; } = new();
    }

    public sealed class ShipmentLineCreateDto
    {
        public Guid OrderLineId { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class ShipmentDetailDto
    {
        public Guid Id { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public int? TotalWeight { get; set; }
        public Darwin.Domain.Enums.ShipmentStatus Status { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
    }

    public sealed class UpdateOrderStatusDto
    {
        public Guid OrderId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public OrderStatus NewStatus { get; set; }
    }

    /// <summary>
    /// Lightweight list item used by Admin queries to render a paged grid of payments for a single order.
    /// Includes RowVersion to support inline soft-delete or future concurrency-aware updates.
    /// </summary>
    public sealed class PaymentListItemDto
    {
        /// <summary>Primary key of the payment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Logical provider name (e.g., "PayPal", "Stripe").</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>External reference or transaction id provided by the PSP.</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Payment currency (ISO 4217), typically same as the order's currency.</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Amount in minor units (e.g., cents).</summary>
        public long AmountMinor { get; set; }

        /// <summary>Current processing state.</summary>
        public Darwin.Domain.Enums.PaymentStatus Status { get; set; }

        /// <summary>Optional failure reason provided by the gateway.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lightweight list item used by Admin queries to render a paged grid of shipments for a single order.
    /// Includes RowVersion to support inline soft-delete or future concurrency-aware updates.
    /// </summary>
    public sealed class ShipmentListItemDto
    {
        /// <summary>Primary key of the shipment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Carrier code/name (e.g., "DHL").</summary>
        public string Carrier { get; set; } = string.Empty;

        /// <summary>Service level (e.g., "Standard", "Express").</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>Optional tracking number provided by the carrier.</summary>
        public string? TrackingNumber { get; set; }

        /// <summary>Total weight of the shipment, unit decided by SiteSetting/UI.</summary>
        public int TotalWeight { get; set; }

        /// <summary>Current shipment lifecycle state.</summary>
        public Darwin.Domain.Enums.ShipmentStatus Status { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
