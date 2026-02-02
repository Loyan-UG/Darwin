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
    /// </summary>
    public sealed class OrdersSeedSection
    {
        private readonly ILogger<OrdersSeedSection> _logger;

        public OrdersSeedSection(ILogger<OrdersSeedSection> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Orders (orders/payments/shipments) ...");

            if (await db.Orders.AnyAsync(ct)) return;

            var variants = await db.ProductVariants
                .OrderBy(v => v.Sku)
                .ToListAsync(ct);

            if (variants.Count == 0)
            {
                _logger.LogWarning("Skipping order seeding because no ProductVariants exist.");
                return;
            }

            var orders = new List<Order>();
            var lines = new List<OrderLine>();
            var payments = new List<Payment>();
            var shipments = new List<Shipment>();
            var shipmentLines = new List<ShipmentLine>();
            var refunds = new List<Refund>();

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

                var order = new Order
                {
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

                // after creating 'order' object
                order.Id = Guid.NewGuid(); // <-- ensure Order.Id is non-empty

                // create line
                var line = new OrderLine
                {
                    Id = Guid.NewGuid(),    // <-- ensure OrderLine.Id is non-empty (used by ShipmentLine)
                    OrderId = order.Id,
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

                // payment
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),     // optional but consistent
                    OrderId = order.Id,
                    Provider = "PayPal",
                    ProviderReference = $"PAY-{Guid.NewGuid():N}",
                    AmountMinor = grandTotal,
                    Currency = "EUR",
                    Status = PaymentStatus.Captured,
                    CapturedAtUtc = DateTime.UtcNow.AddDays(-i)
                };

                // shipment
                var shipment = new Shipment
                {
                    Id = Guid.NewGuid(),     // <-- ensure Shipment.Id is non-empty
                    OrderId = order.Id,
                    Carrier = "DHL",
                    Service = "Standard",
                    TrackingNumber = $"DHL{i + 1000000}",
                    TotalWeight = 1200,
                    Status = ShipmentStatus.Shipped,
                    ShippedAtUtc = DateTime.UtcNow.AddDays(-i)
                };

                // shipmentLine referencing shipment.Id and line.Id
                shipmentLines.Add(new ShipmentLine
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    OrderLineId = line.Id,
                    Quantity = qty
                });

                refunds.Add(new Refund
                {
                    OrderId = order.Id,
                    PaymentId = payment.Id,
                    AmountMinor = i % 3 == 0 ? 500 : 0,
                    Reason = i % 3 == 0 ? "Teilrückerstattung (Test)" : null
                });
            }

            db.AddRange(orders);
            db.AddRange(lines);
            db.AddRange(payments);
            db.AddRange(shipments);
            db.AddRange(shipmentLines);
            db.AddRange(refunds);

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Orders seeding done.");
        }
    }
}