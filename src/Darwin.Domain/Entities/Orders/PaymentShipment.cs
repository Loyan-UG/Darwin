using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Orders
{
    /// <summary>Payment transaction record for an order.</summary>
    public sealed class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>Provider metadata for reconciliation (e.g., gateway payload identifiers).</summary>
        public string? ProviderMetadataJson { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }

    /// <summary>Shipment record for an order.</summary>
    public sealed class Shipment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid? MethodId { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public List<ShipmentLine> Lines { get; set; } = new();
    }

    /// <summary>Join entity mapping a variant and quantity to a shipment.</summary>
    public sealed class ShipmentLine : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>Refund record referencing a payment.</summary>
    public sealed class Refund : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; } = RefundStatus.Pending;
        public DateTime? CompletedAtUtc { get; set; }
    }
}
