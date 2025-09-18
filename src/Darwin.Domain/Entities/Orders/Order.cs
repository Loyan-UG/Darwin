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
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool PricesIncludeTax { get; set; } = false;

        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Created;

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLine> Lines { get; set; } = new();

        // When Infrastructure/fulfillment/payments are ready, these can be used.
        public List<Payment> Payments { get; set; } = new();
        public List<Shipment> Shipments { get; set; } = new();

        public string? InternalNotes { get; set; }
    }

    /// <summary>
    /// Order line snapshot of a variant with pricing/tax details and add-on selections at purchase time.
    /// </summary>
    public sealed class OrderLine : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid VariantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public long UnitPriceGrossMinor { get; set; }

        public long LineTaxMinor { get; set; }
        public long LineGrossMinor { get; set; }

        /// <summary>
        /// Snapshot of add-on selections as JSON array of AddOnOptionValue IDs.
        /// </summary>
        public string AddOnValueIdsJson { get; set; } = "[]";

        /// <summary>Snapshot of total add-on delta (minor units, net) for this line.</summary>
        public long AddOnPriceDeltaMinor { get; set; } = 0;
    }

    // Payments/Shipment/ShipmentLine/Refund remain as you provided earlier.
}
