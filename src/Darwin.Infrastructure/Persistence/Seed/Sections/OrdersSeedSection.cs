using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds orders and related records:
    /// - Orders (10+)
    /// - OrderLines (10+)
    /// - Payments (10+)
    /// - Shipments (10+)
    /// - ShipmentLines (10+)
    /// - Refunds (10+)
    ///
    /// This implementation:
    /// - Builds coherent parent->child object graphs and adds root orders to the context.
    /// - Generates non-empty GUIDs client-side for entities that need cross-references before SaveChanges.
    /// - Collects refunds in a separate list because Order does not expose a Refunds navigation property.
    /// </summary>
    public sealed class OrdersSeedSection
    {
        private readonly ILogger<OrdersSeedSection> _logger;

        public OrdersSeedSection(ILogger<OrdersSeedSection> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Idempotent seeding of orders and related entities.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Orders (orders/payments/shipments) ...");

            // If any orders already exist, don't reseed.
            if (await db.Orders.AnyAsync(ct))
            {
                _logger.LogInformation("Orders already present. Skipping.");
                return;
            }

            // Ensure product variants are available — orders are seeded only if variants exist.
            var variants = await db.ProductVariants
                .OrderBy(v => v.Sku)
                .ToListAsync(ct);

            if (variants.Count == 0)
            {
                _logger.LogWarning("Skipping order seeding because no ProductVariants exist.");
                return;
            }

            var orders = new List<Order>();
            var refunds = new List<Refund>(); // Refunds are collected separately because Order has no Refunds nav.

            // Build sample orders with full child graphs.
            for (var i = 0; i < 10; i++)
            {
                var variant = variants[i % variants.Count];
                var qty = (i % 3) + 1;

                var unitNet = variant.BasePriceNetMinor;
                var vatRate = 0.19m;
                var unitGross = (long)Math.Round(unitNet * (1 + vatRate));

                var lineTax = (long)Math.Round(unitNet * qty * vatRate);
                var lineGross = (unitGross * qty);

                var shipping = 590;
                var subtotalNet = unitNet * qty;
                var taxTotal = lineTax;
                var grandTotal = subtotalNet + taxTotal + shipping;

                // Create root order. Generate Id client-side to allow safe references.
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"DE-2026-{i + 1:D4}",
                    Currency = "EUR",
                    PricesIncludeTax = false,
                    SubtotalNetMinor = subtotalNet,
                    TaxTotalMinor = taxTotal,
                    ShippingTotalMinor = shipping,
                    DiscountTotalMinor = 0,
                    GrandTotalGrossMinor = grandTotal,
                    Status = i % 2 == 0 ? OrderStatus.Paid : OrderStatus.Confirmed,
                    BillingAddressJson = "{\"name\":\"Max Mustermann\",\"street\":\"Hauptstraße 1\",\"city\":\"Berlin\",\"zip\":\"10115\"}",
                    ShippingAddressJson = "{\"name\":\"Max Mustermann\",\"street\":\"Hauptstraße 1\",\"city\":\"Berlin\",\"zip\":\"10115\"}",
                    InternalNotes = "Seeded order for backend testing."
                };

                // Create order line with explicit Id so other children (e.g., ShipmentLine) can reference it.
                var line = new OrderLine
                {
                    Id = Guid.NewGuid(),
                    VariantId = variant.Id,
                    Name = $"Artikel {variant.Sku}",
                    Sku = variant.Sku,
                    Quantity = qty,
                    UnitPriceNetMinor = unitNet,
                    VatRate = vatRate,
                    UnitPriceGrossMinor = unitGross,
                    LineTaxMinor = lineTax,
                    LineGrossMinor = lineGross,
                    AddOnValueIdsJson = "[]",
                    AddOnPriceDeltaMinor = 0
                };

                // Attach via navigation property so EF will wire OrderId automatically.
                order.Lines.Add(line);

                // Create payment (attach via navigation).
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id, // explicit is OK; navigation also set below
                    Provider = "PayPal",
                    ProviderReference = $"PAY-{Guid.NewGuid():N}",
                    AmountMinor = grandTotal,
                    Currency = "EUR",
                    Status = PaymentStatus.Captured,
                    CapturedAtUtc = DateTime.UtcNow.AddDays(-i)
                };
                order.Payments.Add(payment);

                // Create shipment and shipment line (shipment line references OrderLine.Id).
                var shipment = new Shipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Carrier = "DHL",
                    Service = "Standard",
                    TrackingNumber = $"DHL{i + 1000000}",
                    TotalWeight = 1200,
                    Status = ShipmentStatus.Shipped,
                    ShippedAtUtc = DateTime.UtcNow.AddDays(-i)
                };

                var shipmentLine = new ShipmentLine
                {
                    Id = Guid.NewGuid(),
                    OrderLineId = line.Id, // safe because line.Id was generated above
                    Quantity = qty
                };

                // Add shipment line to shipment navigation, then add shipment to order.
                shipment.Lines.Add(shipmentLine);
                order.Shipments.Add(shipment);

                // Optionally create a refund record (some orders only).
                if (i % 3 == 0)
                {
                    var refund = new Refund
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PaymentId = payment.Id,
                        AmountMinor = 500,
                        Reason = "Teilrückerstattung (Test)"
                    };
                    // Collect refunds separately because Order has no Refunds navigation.
                    refunds.Add(refund);
                }

                orders.Add(order);
            }

            // Add full graph of orders; EF will persist children as well.
            db.AddRange(orders);

            // Add refunds separately (because Order has no Refunds navigation property).
            if (refunds.Count > 0)
            {
                db.AddRange(refunds);
            }

            // Single SaveChanges to persist all new entities in the correct order.
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Orders seeding done.");
        }
    }
}