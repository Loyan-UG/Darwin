using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Shipping;
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
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
        public bool PricesIncludeTax { get; set; }

        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }

        /// <summary>
        /// Optional selected shipping method identifier used during checkout.
        /// The related shipping method may later change or be deleted, so the order also stores snapshot fields below.
        /// </summary>
        public Guid? ShippingMethodId { get; set; }

        /// <summary>
        /// Snapshot of the shipping method display name chosen at checkout.
        /// </summary>
        public string? ShippingMethodName { get; set; }

        /// <summary>
        /// Snapshot of the carrier label chosen at checkout.
        /// </summary>
        public string? ShippingCarrier { get; set; }

        /// <summary>
        /// Snapshot of the carrier service level chosen at checkout.
        /// </summary>
        public string? ShippingService { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Created;

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLine> Lines { get; set; } = new();
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
        /// <summary>
        /// Optional fulfillment warehouse chosen for this line.
        /// Keeping it on the order snapshot prevents later status transitions from
        /// re-resolving stock against a different warehouse.
        /// </summary>
        public Guid? WarehouseId { get; set; }
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
        public long AddOnPriceDeltaMinor { get; set; }
    }
}
