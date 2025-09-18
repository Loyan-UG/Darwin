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
        /// <summary>Logical provider name (e.g., PayPal, SEPA).</summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>Provider-specific reference id for reconciliation.</summary>
        public string ProviderReference { get; set; } = string.Empty;
        /// <summary>Amount in minor units.</summary>
        public long AmountMinor { get; set; }
        /// <summary>ISO currency code, e.g., EUR.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Processing status.</summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
        /// <summary>Optional timestamp of capture completion.</summary>
        public DateTime? CapturedAtUtc { get; set; }
        /// <summary>Optional human-readable failure reason captured from provider.</summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>Shipment record for an order.</summary>
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
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public List<ShipmentLine> Lines { get; set; } = new();
    }

    /// <summary>Join entity mapping an order line into a shipment with a shipped quantity.</summary>
    public sealed class ShipmentLine : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderLineId { get; set; }
        /// <summary>Quantity of the order line shipped in this package.</summary>
        public int Quantity { get; set; }
    }

    /// <summary>Refund record referencing an order and optionally a specific payment.</summary>
    public sealed class Refund : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        /// <summary>Amount refunded in minor units.</summary>
        public long AmountMinor { get; set; }
        /// <summary>Optional reason provided by staff or system.</summary>
        public string? Reason { get; set; }
    }
}
