using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Orders
{
    /// <summary>Shipment record for an order.</summary>
    public sealed class Shipment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid? MethodId { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public int? TotalWeight { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public string? LastCarrierEventKey { get; set; }
        public List<ShipmentLine> Lines { get; set; } = new();
        public List<ShipmentCarrierEvent> CarrierEvents { get; set; } = new();
    }

    /// <summary>Carrier-originated or provider-synthesized lifecycle event for a shipment.</summary>
    public sealed class ShipmentCarrierEvent : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string ProviderShipmentReference { get; set; } = string.Empty;
        public string CarrierEventKey { get; set; } = string.Empty;
        public string? ProviderStatus { get; set; }
        public string? ExceptionCode { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public string? Service { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }

    /// <summary>Join entity mapping a variant and quantity to a shipment.</summary>
    public sealed class ShipmentLine : BaseEntity
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderLineId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>Refund record referencing a payment.</summary>
    public sealed class Refund : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; } = RefundStatus.Pending;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderRefundReference { get; set; }
        public string? ProviderPaymentReference { get; set; }
        public string? ProviderStatus { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? RequestedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
