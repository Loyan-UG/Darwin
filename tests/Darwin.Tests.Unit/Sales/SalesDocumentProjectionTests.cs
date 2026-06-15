using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Sales
{
    public sealed class SalesDocumentProjectionTests
    {
        [Fact]
        public async Task GetSalesOrderDocument_Should_Project_Current_Order_Without_Recomputing_Snapshots()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var orderId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var now = new DateTime(2026, 6, 11, 10, 0, 0, DateTimeKind.Utc);

            db.Set<Order>().Add(new Order
            {
                Id = orderId,
                CreatedAtUtc = now,
                OrderNumber = "ORD-1001",
                UserId = Guid.NewGuid(),
                BusinessId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Currency = "EUR",
                PricesIncludeTax = true,
                SalesChannel = SalesChannel.WebStorefront,
                OrderedAtUtc = now.AddMinutes(-10),
                SubtotalNetMinor = 10000,
                TaxTotalMinor = 1900,
                ShippingTotalMinor = 490,
                DiscountTotalMinor = 500,
                GrandTotalGrossMinor = 11890,
                Status = OrderStatus.Paid,
                BillingAddressJson = "{\"line1\":\"Snapshot billing\"}",
                ShippingAddressJson = "{\"line1\":\"Snapshot shipping\"}",
                ShippingMethodId = Guid.NewGuid(),
                ShippingMethodName = "Standard",
                ShippingCarrier = "DHL",
                ShippingService = "Parcel",
                InternalNotes = "internal only",
                Lines =
                {
                    new OrderLine
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = now.AddMinutes(1),
                        OrderId = orderId,
                        VariantId = Guid.NewGuid(),
                        WarehouseId = Guid.NewGuid(),
                        Name = "Snapshot product",
                        Sku = "SKU-1",
                        Quantity = 2,
                        UnitPriceNetMinor = 5000,
                        VatRate = 0.19m,
                        UnitPriceGrossMinor = 5950,
                        LineTaxMinor = 1900,
                        LineGrossMinor = 11900,
                        AddOnValueIdsJson = "[\"addon-1\"]",
                        AddOnPriceDeltaMinor = 250
                    }
                },
                Payments =
                {
                    new Payment
                    {
                        Id = paymentId,
                        CreatedAtUtc = now.AddMinutes(2),
                        OrderId = orderId,
                        InvoiceId = invoiceId,
                        CustomerId = Guid.NewGuid(),
                        UserId = Guid.NewGuid(),
                        AmountMinor = 11890,
                        Currency = "EUR",
                        Status = PaymentStatus.Completed,
                        Provider = "provider",
                        ProviderTransactionRef = "txn_1",
                        ProviderPaymentIntentRef = "pi_1",
                        ProviderCheckoutSessionRef = "cs_1",
                        PaidAtUtc = now.AddMinutes(3)
                    }
                },
                Shipments =
                {
                    new Shipment
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = now.AddMinutes(4),
                        OrderId = orderId,
                        MethodId = Guid.NewGuid(),
                        Status = ShipmentStatus.Shipped,
                        Carrier = "DHL",
                        Service = "Parcel",
                        ProviderShipmentReference = "ship_1",
                        TrackingNumber = "TRACK-1",
                        LabelUrl = "https://labels.example/1",
                        TotalWeight = 1200,
                        ShippedAtUtc = now.AddMinutes(5)
                    }
                }
            });

            db.Set<Invoice>().Add(new Invoice
            {
                Id = invoiceId,
                CreatedAtUtc = now.AddMinutes(6),
                OrderId = orderId,
                PaymentId = paymentId,
                InvoiceNumber = "INV-1001",
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                TotalNetMinor = 10000,
                TotalTaxMinor = 1900,
                TotalGrossMinor = 11900,
                DueDateUtc = now.AddDays(14),
                IssuedAtUtc = now.AddMinutes(7),
                IssuedSnapshotJson = "{\"invoice\":\"issued\"}",
                ArchiveGeneratedAtUtc = now.AddMinutes(8)
            });

            await db.SaveChangesAsync();

            var handler = new GetSalesOrderDocumentHandler(db);
            var result = await handler.HandleAsync(orderId);

            result.Should().NotBeNull();
            result!.OrderNumber.Should().Be("ORD-1001");
            result.SalesChannel.Should().Be(SalesChannel.WebStorefront);
            result.OrderedAtUtc.Should().Be(now.AddMinutes(-10));
            result.BillingAddressJson.Should().Be("{\"line1\":\"Snapshot billing\"}");
            result.ShippingAddressJson.Should().Be("{\"line1\":\"Snapshot shipping\"}");
            result.Lines.Should().ContainSingle().Which.Name.Should().Be("Snapshot product");
            result.Settlements.Should().ContainSingle().Which.ProviderPaymentIntentReference.Should().Be("pi_1");
            result.Fulfillments.Should().ContainSingle().Which.TrackingNumber.Should().Be("TRACK-1");
            var invoiceSummary = result.Invoices.Should().ContainSingle().Which;
            invoiceSummary.InvoiceNumber.Should().Be("INV-1001");
            invoiceSummary.HasIssuedSnapshot.Should().BeTrue();
        }

        [Fact]
        public async Task GetSalesInvoiceDocument_Should_Project_Current_Invoice_And_Link_Order_Number()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var orderId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var now = new DateTime(2026, 6, 11, 11, 0, 0, DateTimeKind.Utc);

            db.Set<Order>().Add(new Order
            {
                Id = orderId,
                CreatedAtUtc = now,
                OrderNumber = "ORD-2002"
            });

            db.Set<Invoice>().Add(new Invoice
            {
                Id = invoiceId,
                CreatedAtUtc = now.AddMinutes(1),
                BusinessId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                OrderId = orderId,
                PaymentId = Guid.NewGuid(),
                InvoiceNumber = "INV-2002",
                Status = InvoiceStatus.Paid,
                Currency = "EUR",
                TotalNetMinor = 20000,
                TotalTaxMinor = 3800,
                TotalGrossMinor = 23800,
                DueDateUtc = now.AddDays(7),
                PaidAtUtc = now.AddMinutes(2),
                IssuedAtUtc = now.AddMinutes(3),
                ReverseChargeApplied = false,
                IssuedSnapshotJson = "{\"issued\":true}",
                IssuedSnapshotHashSha256 = "hash",
                ArchiveGeneratedAtUtc = now.AddMinutes(4),
                ArchiveRetentionPolicyVersion = "v1",
                Lines =
                {
                    new InvoiceLine
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = now.AddMinutes(5),
                        InvoiceId = invoiceId,
                        Description = "Invoice line",
                        Quantity = 1,
                        UnitPriceNetMinor = 20000,
                        TaxRate = 0.19m,
                        TotalNetMinor = 20000,
                        TotalTaxMinor = 3800,
                        TotalGrossMinor = 23800
                    }
                }
            });

            await db.SaveChangesAsync();

            var handler = new GetSalesInvoiceDocumentHandler(db);
            var result = await handler.HandleAsync(invoiceId);

            result.Should().NotBeNull();
            result!.OrderNumber.Should().Be("ORD-2002");
            result.InvoiceNumber.Should().Be("INV-2002");
            result.Status.Should().Be(InvoiceStatus.Paid);
            result.IssuedSnapshotJson.Should().Be("{\"issued\":true}");
            result.ArchiveRetentionPolicyVersion.Should().Be("v1");
            var line = result.Lines.Should().ContainSingle().Which;
            line.Description.Should().Be("Invoice line");
            line.TotalTaxMinor.Should().Be(3800);
        }

        [Fact]
        public async Task SalesDocumentHandlers_Should_Return_Null_For_Empty_NotFound_And_SoftDeleted_Records()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var deletedOrderId = Guid.NewGuid();
            var deletedInvoiceId = Guid.NewGuid();

            db.Set<Order>().Add(new Order
            {
                Id = deletedOrderId,
                OrderNumber = "ORD-DELETED",
                IsDeleted = true
            });

            db.Set<Invoice>().Add(new Invoice
            {
                Id = deletedInvoiceId,
                Status = InvoiceStatus.Draft,
                IsDeleted = true
            });

            await db.SaveChangesAsync();

            var orderHandler = new GetSalesOrderDocumentHandler(db);
            var invoiceHandler = new GetSalesInvoiceDocumentHandler(db);

            (await orderHandler.HandleAsync(Guid.Empty)).Should().BeNull();
            (await orderHandler.HandleAsync(Guid.NewGuid())).Should().BeNull();
            (await orderHandler.HandleAsync(deletedOrderId)).Should().BeNull();
            (await invoiceHandler.HandleAsync(Guid.Empty)).Should().BeNull();
            (await invoiceHandler.HandleAsync(Guid.NewGuid())).Should().BeNull();
            (await invoiceHandler.HandleAsync(deletedInvoiceId)).Should().BeNull();
        }

        [Fact]
        public async Task GetSalesOverview_Should_Project_Summary_And_Channel_Breakdown()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var now = new DateTime(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc);
            var paymentId = Guid.NewGuid();

            db.Set<Order>().AddRange(
                new Order
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderedAtUtc = now,
                    OrderNumber = "ORD-WEB",
                    Currency = "EUR",
                    SalesChannel = SalesChannel.WebStorefront,
                    Status = OrderStatus.Paid,
                    GrandTotalGrossMinor = 11900,
                    TaxTotalMinor = 1900
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderedAtUtc = now.AddMinutes(1),
                    OrderNumber = "ORD-ADMIN",
                    Currency = "EUR",
                    SalesChannel = SalesChannel.Admin,
                    Status = OrderStatus.Created,
                    GrandTotalGrossMinor = 5000,
                    TaxTotalMinor = 800
                });
            db.Set<Invoice>().Add(new Invoice
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = now,
                PaymentId = paymentId,
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                TotalGrossMinor = 11900,
                DueDateUtc = now.AddDays(-1)
            });
            db.Set<Payment>().Add(new Payment
            {
                Id = paymentId,
                CreatedAtUtc = now,
                AmountMinor = 5000,
                Currency = "EUR",
                Provider = "provider",
                Status = PaymentStatus.Completed
            });
            db.Set<Refund>().Add(new Refund
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = now,
                PaymentId = paymentId,
                AmountMinor = 1000,
                Currency = "EUR",
                Reason = "Partial return",
                Status = RefundStatus.Completed
            });
            db.Set<ReturnOrder>().AddRange(
                new ReturnOrder
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderId = Guid.NewGuid(),
                    Status = ReturnOrderStatus.Received,
                    Currency = "EUR"
                },
                new ReturnOrder
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderId = Guid.NewGuid(),
                    Status = ReturnOrderStatus.Closed,
                    Currency = "EUR"
                });

            await db.SaveChangesAsync();

            var result = await new GetSalesOverviewHandler(db).HandleAsync();

            result.OrderCount.Should().Be(2);
            result.GrossTotalMinor.Should().Be(16900);
            result.TaxTotalMinor.Should().Be(2700);
            result.InvoiceCount.Should().Be(1);
            result.OpenInvoiceBalanceMinor.Should().Be(7900);
            result.ReturnOrderCount.Should().Be(2);
            result.ReturnOrderAttentionCount.Should().Be(1);
            result.ChannelBreakdown.Should().HaveCount(2);
            result.ChannelBreakdown.Single(x => x.SalesChannel == SalesChannel.WebStorefront).OrderCount.Should().Be(1);
        }

        [Fact]
        public async Task GetSalesOrdersPage_Should_Filter_By_Query_Status_Channel_Date_And_Scope()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var businessId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var now = new DateTime(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc);

            db.Set<Order>().AddRange(
                new Order
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderedAtUtc = now,
                    OrderNumber = "ORD-MATCH",
                    BusinessId = businessId,
                    CustomerId = customerId,
                    SalesChannel = SalesChannel.WebStorefront,
                    Status = OrderStatus.Paid,
                    Currency = "EUR",
                    GrandTotalGrossMinor = 10000
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    OrderedAtUtc = now.AddDays(-10),
                    OrderNumber = "ORD-OTHER",
                    BusinessId = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    SalesChannel = SalesChannel.Admin,
                    Status = OrderStatus.Completed,
                    Currency = "EUR",
                    GrandTotalGrossMinor = 9000
                });

            await db.SaveChangesAsync();

            var (items, total) = await new GetSalesOrdersPageHandler(db).HandleAsync(
                page: 1,
                pageSize: 20,
                query: "MATCH",
                filter: SalesOrderDocumentFilter.Paid,
                salesChannel: SalesChannel.WebStorefront,
                orderedFromUtc: now.AddDays(-1),
                orderedToUtc: now.AddDays(1),
                businessId: businessId,
                customerId: customerId);

            total.Should().Be(1);
            items.Should().ContainSingle().Which.OrderNumber.Should().Be("ORD-MATCH");
        }

        [Fact]
        public async Task GetSalesInvoicesPage_Should_Filter_And_Calculate_Balance()
        {
            await using var db = SalesDocumentTestDbContext.Create();
            var invoiceId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var now = new DateTime(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc);

            db.Set<Invoice>().Add(new Invoice
            {
                Id = invoiceId,
                CreatedAtUtc = now,
                PaymentId = paymentId,
                InvoiceNumber = "INV-MATCH",
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                TotalGrossMinor = 11900,
                DueDateUtc = now.AddDays(-1)
            });
            db.Set<Payment>().Add(new Payment
            {
                Id = paymentId,
                CreatedAtUtc = now,
                AmountMinor = 5000,
                Currency = "EUR",
                Provider = "provider",
                Status = PaymentStatus.Completed
            });

            await db.SaveChangesAsync();

            var (items, total) = await new GetSalesInvoicesPageHandler(db).HandleAsync(
                page: 1,
                pageSize: 20,
                query: "INV-MATCH",
                filter: SalesInvoiceDocumentFilter.Overdue,
                dateFromUtc: now.AddDays(-2),
                dateToUtc: now.AddDays(1));

            total.Should().Be(1);
            var item = items.Should().ContainSingle().Which;
            item.InvoiceNumber.Should().Be("INV-MATCH");
            item.BalanceMinor.Should().Be(6900);
        }

        private sealed class SalesDocumentTestDbContext : DbContext, IAppDbContext
        {
            private SalesDocumentTestDbContext(DbContextOptions<SalesDocumentTestDbContext> options)
                : base(options)
            {
            }

            public static SalesDocumentTestDbContext Create()
            {
                var options = new DbContextOptionsBuilder<SalesDocumentTestDbContext>()
                    .UseInMemoryDatabase($"darwin_sales_document_tests_{Guid.NewGuid():N}")
                    .Options;
                return new SalesDocumentTestDbContext(options);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderLine>();
                modelBuilder.Entity<Payment>();
                modelBuilder.Entity<Refund>();
                modelBuilder.Entity<Shipment>();
                modelBuilder.Entity<Invoice>();
                modelBuilder.Entity<InvoiceLine>();
                modelBuilder.Entity<SalesQuote>();
                modelBuilder.Entity<SalesQuoteLine>();
                modelBuilder.Entity<ReturnOrder>();
                modelBuilder.Entity<ReturnOrderLine>();
                modelBuilder.Entity<ReturnOrderRefundLink>();
            }
        }
    }
}
