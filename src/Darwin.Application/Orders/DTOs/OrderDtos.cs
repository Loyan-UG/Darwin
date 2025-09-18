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
    /// Summary for admin listing.
    /// </summary>
    public sealed class OrderListItemDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
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

    public sealed class PaymentCreateDto
    {
        public Guid OrderId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderReference { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
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
}
