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

        // Legacy compatibility fields.
        public string Provider { get; set; } = string.Empty;
        public string ProviderReference { get; set; } = string.Empty;
        public DateTime? CapturedAtUtc
        {
            get => CompletedAtUtc;
            set => CompletedAtUtc = value;
        }
        public string? FailureReason { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }

    /// <summary>Shipment record for an order.</summary>
    public sealed class Shipment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid? MethodId { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public int? TotalWeight { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public List<ShipmentLine> Lines { get; set; } = new();
    }

    /// <summary>Join entity mapping a variant and quantity to a shipment.</summary>
    public sealed class ShipmentLine : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid VariantId { get; set; }
        public Guid OrderLineId
        {
            get => VariantId;
            set => VariantId = value;
        }
        public int Quantity { get; set; }
    }

    /// <summary>Refund record referencing a payment.</summary>
    public sealed class Refund : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; } = RefundStatus.Pending;
        public DateTime? CompletedAtUtc { get; set; }
    }
}
