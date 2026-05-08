using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
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

            // If any orders already exist, don't reseed the full demo set.
            // Ensure product variants are available before order coverage is repaired.
            var variants = await db.ProductVariants
                .OrderBy(v => v.Sku)
                .ToListAsync(ct);

            if (variants.Count == 0)
            {
                _logger.LogWarning("Skipping order seeding because no ProductVariants exist.");
                return;
            }

            if (await db.Orders.AnyAsync(ct))
            {
                _logger.LogInformation("Orders already present. Checking storefront member coverage.");
                await EnsurePrimaryConsumerOrderCoverageAsync(db, variants, ct);
                return;
            }

            var orders = new List<Order>();
            var refunds = new List<Refund>(); // Refunds are collected separately because Order has no Refunds nav.

            // Build demo orders with full child graphs.
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
                    Currency = DomainDefaults.DefaultCurrency,
                    PricesIncludeTax = false,
                    SubtotalNetMinor = subtotalNet,
                    TaxTotalMinor = taxTotal,
                    ShippingTotalMinor = shipping,
                    DiscountTotalMinor = 0,
                    GrandTotalGrossMinor = grandTotal,
                    Status = i % 2 == 0 ? OrderStatus.Paid : OrderStatus.Confirmed,
                    BillingAddressJson = "{\"name\":\"Darwin Demo Customer\",\"street\":\"Hauptstrasse 1\",\"city\":\"Berlin\",\"zip\":\"10115\"}",
                    ShippingAddressJson = "{\"name\":\"Darwin Demo Customer\",\"street\":\"Hauptstrasse 1\",\"city\":\"Berlin\",\"zip\":\"10115\"}",
                    InternalNotes = "Seeded order for backend operations review."
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
                var payment = new Darwin.Domain.Entities.Billing.Payment
                {
                    Id = Guid.NewGuid(),
                    BusinessId = null,
                    OrderId = order.Id, // explicit is OK; navigation also set below
                    UserId = order.UserId,
                    Provider = "PayPal",
                    ProviderTransactionRef = $"PAY-{Guid.NewGuid():N}",
                    AmountMinor = grandTotal,
                    Currency = DomainDefaults.DefaultCurrency,
                    Status = PaymentStatus.Captured,
                    PaidAtUtc = DateTime.UtcNow.AddDays(-i)
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
                        Reason = "Partial refund (test)"
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

            await db.SaveChangesAsync(ct);

            await EnsurePrimaryConsumerOrderCoverageAsync(db, variants, ct);

            _logger.LogInformation("Orders seeding done.");
        }

        private static async Task EnsurePrimaryConsumerOrderCoverageAsync(
            DarwinDbContext db,
            IReadOnlyList<Domain.Entities.Catalog.ProductVariant> variants,
            CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == "cons1@darwin.de" && !x.IsDeleted, ct);
            if (user is null || variants.Count == 0)
            {
                return;
            }

            var businessId = await db.Set<Domain.Entities.Businesses.Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);

            var shippingMethod = await db.ShippingMethods
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .FirstOrDefaultAsync(ct);

            for (var i = 0; i < 3; i++)
            {
                var orderNumber = $"WEB-CONS1-2026-{i + 1:D3}";
                var order = await db.Orders
                    .Include(x => x.Lines)
                    .Include(x => x.Payments)
                    .Include(x => x.Shipments)
                    .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber && !x.IsDeleted, ct);

                var variant = variants[i % variants.Count];
                var quantity = i + 1;
                var vatRate = 0.19m;
                var subtotalNet = variant.BasePriceNetMinor * quantity;
                var taxTotal = (long)Math.Round(subtotalNet * vatRate);
                var shipping = i == 0 ? 0 : 590;
                var grandTotal = subtotalNet + taxTotal + shipping;
                var status = i == 2 ? OrderStatus.Confirmed : OrderStatus.Paid;

                if (order is null)
                {
                    order = new Order
                    {
                        Id = Guid.NewGuid(),
                        OrderNumber = orderNumber
                    };
                    db.Orders.Add(order);
                }

                order.UserId = user.Id;
                order.Currency = DomainDefaults.DefaultCurrency;
                order.PricesIncludeTax = false;
                order.SubtotalNetMinor = subtotalNet;
                order.TaxTotalMinor = taxTotal;
                order.ShippingTotalMinor = shipping;
                order.DiscountTotalMinor = 0;
                order.GrandTotalGrossMinor = grandTotal;
                order.Status = status;
                order.ShippingMethodId = shippingMethod?.Id;
                order.ShippingMethodName = shippingMethod?.Name ?? "DHL Standard";
                order.ShippingCarrier = shippingMethod?.Carrier ?? "DHL";
                order.ShippingService = shippingMethod?.Service ?? "Standard";
                order.BillingAddressJson = "{\"name\":\"Emma Krueger\",\"street\":\"Hauptstrasse 1\",\"city\":\"Berlin\",\"zip\":\"10115\",\"country\":\"DE\"}";
                order.ShippingAddressJson = order.BillingAddressJson;
                order.InternalNotes = "Self-healed storefront member seed order for Web testing.";

                var line = order.Lines.FirstOrDefault(x => x.VariantId == variant.Id && !x.IsDeleted);
                if (line is null)
                {
                    line = new OrderLine
                    {
                        Id = Guid.NewGuid(),
                        VariantId = variant.Id
                    };
                    order.Lines.Add(line);
                }

                line.Name = $"Seed item {variant.Sku}";
                line.Sku = variant.Sku;
                line.Quantity = quantity;
                line.UnitPriceNetMinor = variant.BasePriceNetMinor;
                line.VatRate = vatRate;
                line.UnitPriceGrossMinor = (long)Math.Round(variant.BasePriceNetMinor * (1 + vatRate));
                line.LineTaxMinor = taxTotal;
                line.LineGrossMinor = line.UnitPriceGrossMinor * quantity;
                line.AddOnValueIdsJson = "[]";
                line.AddOnPriceDeltaMinor = 0;

                var payment = order.Payments.FirstOrDefault(x => !x.IsDeleted);
                if (payment is null)
                {
                    payment = new Darwin.Domain.Entities.Billing.Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id
                    };
                    order.Payments.Add(payment);
                }

                payment.BusinessId = businessId;
                payment.UserId = user.Id;
                payment.Provider = i == 2 ? "Stripe" : "PayPal";
                payment.ProviderTransactionRef = $"WEB-SEED-PAY-{i + 1:D3}";
                payment.AmountMinor = grandTotal;
                payment.Currency = DomainDefaults.DefaultCurrency;
                payment.Status = i == 2 ? PaymentStatus.Pending : PaymentStatus.Captured;
                payment.PaidAtUtc = i == 2 ? null : DateTime.UtcNow.AddDays(-(i + 1));

                var shipment = order.Shipments.FirstOrDefault(x => !x.IsDeleted);
                if (shipment is null)
                {
                    shipment = new Shipment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id
                    };
                    order.Shipments.Add(shipment);
                }

                shipment.Carrier = order.ShippingCarrier ?? "DHL";
                shipment.Service = order.ShippingService ?? "Standard";
                shipment.TrackingNumber = $"DHL-CONS1-{i + 1:D6}";
                shipment.TotalWeight = variant.PackageWeight ?? 1200;
                shipment.Status = i == 2 ? ShipmentStatus.Pending : ShipmentStatus.Shipped;
                shipment.ShippedAtUtc = i == 2 ? null : DateTime.UtcNow.AddDays(-i);

                await db.SaveChangesAsync(ct);
                await EnsureMemberInvoiceForOrderAsync(db, order, payment, businessId, i, ct);
            }
        }

        private static async Task EnsureMemberInvoiceForOrderAsync(
            DarwinDbContext db,
            Order order,
            Darwin.Domain.Entities.Billing.Payment payment,
            Guid? businessId,
            int index,
            CancellationToken ct)
        {
            var invoice = await db.Set<Invoice>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

            if (invoice is null)
            {
                invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id
                };
                db.Set<Invoice>().Add(invoice);
            }

            invoice.BusinessId = businessId;
            invoice.PaymentId = payment.Id;
            invoice.Status = order.Status == OrderStatus.Paid ? InvoiceStatus.Paid : InvoiceStatus.Open;
            invoice.Currency = order.Currency;
            invoice.TotalNetMinor = order.SubtotalNetMinor + order.ShippingTotalMinor;
            invoice.TotalTaxMinor = order.TaxTotalMinor;
            invoice.TotalGrossMinor = order.GrandTotalGrossMinor;
            invoice.DueDateUtc = DateTime.UtcNow.Date.AddDays(14 + index);
            invoice.PaidAtUtc = invoice.Status == InvoiceStatus.Paid ? payment.PaidAtUtc : null;

            var line = invoice.Lines.FirstOrDefault(x => !x.IsDeleted);
            if (line is null)
            {
                line = new InvoiceLine { Id = Guid.NewGuid() };
                invoice.Lines.Add(line);
            }

            line.Description = $"Invoice for order {order.OrderNumber}";
            line.Quantity = 1;
            line.UnitPriceNetMinor = invoice.TotalNetMinor;
            line.TaxRate = 0.19m;
            line.TotalNetMinor = invoice.TotalNetMinor;
            line.TotalGrossMinor = invoice.TotalGrossMinor;

            await db.SaveChangesAsync(ct);
        }
    }
}
