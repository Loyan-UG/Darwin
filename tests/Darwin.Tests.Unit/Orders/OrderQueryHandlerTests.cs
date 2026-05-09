using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Handler-level unit tests for admin and member order query handlers.
/// Covers <see cref="GetOrdersPageHandler"/>, <see cref="GetOrderForViewHandler"/>,
/// <see cref="GetOrderPaymentsPageHandler"/>, <see cref="GetMyOrdersPageHandler"/>,
/// and <see cref="GetMyOrderForViewHandler"/>.
/// </summary>
public sealed class OrderQueryHandlerTests
{
    // ─── GetOrdersPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersPage_Should_ReturnAllNonDeleted()
    {
        await using var db = OrderQueryTestDbContext.Create();
        db.Set<Order>().AddRange(
            BuildOrder("ORD-001"),
            BuildOrder("ORD-002"),
            BuildOrder("ORD-003", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Select(x => x.OrderNumber).Should().NotContain("ORD-003");
    }

    [Fact]
    public async Task GetOrdersPage_Should_NormalizePage_WhenBelowOne()
    {
        await using var db = OrderQueryTestDbContext.Create();
        db.Set<Order>().Add(BuildOrder("ORD-X"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, _) = await handler.HandleAsync(page: -5, pageSize: 20, TestContext.Current.CancellationToken);

        items.Should().HaveCount(1, "negative page is clamped to 1");
    }

    [Fact]
    public async Task GetOrdersPage_Should_ClampPageSize_WhenAboveMax()
    {
        await using var db = OrderQueryTestDbContext.Create();
        for (var i = 1; i <= 210; i++)
            db.Set<Order>().Add(BuildOrder($"ORD-{i:D3}"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 9999, TestContext.Current.CancellationToken);

        total.Should().Be(210);
        items.Should().HaveCount(200, "max page size is 200");
    }

    [Fact]
    public async Task GetOrdersPage_Filter_Open_Should_ExcludeCancelledRefundedCompleted()
    {
        await using var db = OrderQueryTestDbContext.Create();
        db.Set<Order>().AddRange(
            BuildOrder("CREATED",   status: OrderStatus.Created),
            BuildOrder("CONFIRMED", status: OrderStatus.Confirmed),
            BuildOrder("PAID",      status: OrderStatus.Paid),
            BuildOrder("CANCELLED", status: OrderStatus.Cancelled),
            BuildOrder("REFUNDED",  status: OrderStatus.Refunded),
            BuildOrder("COMPLETED", status: OrderStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, null, OrderQueueFilter.Open, TestContext.Current.CancellationToken);

        total.Should().Be(3, "Created, Confirmed, Paid are open");
        items.Select(x => x.OrderNumber).Should().NotContain(new[] { "CANCELLED", "REFUNDED", "COMPLETED" });
    }

    [Fact]
    public async Task GetOrdersPage_Filter_PaymentIssues_Should_ReturnOrders_WithFailedPayments()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "PAY-FAIL", Currency = "EUR", Status = OrderStatus.Paid, BillingAddressJson = "{}", ShippingAddressJson = "{}", RowVersion = new byte[] { 1 } });
        db.Set<Order>().Add(BuildOrder("PAY-OK", status: OrderStatus.Paid));

        db.Set<Payment>().Add(new Payment { Id = Guid.NewGuid(), OrderId = orderId, Status = PaymentStatus.Failed, Provider = "Stripe", Currency = "EUR", AmountMinor = 100, RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, null, OrderQueueFilter.PaymentIssues, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].OrderNumber.Should().Be("PAY-FAIL");
    }

    [Fact]
    public async Task GetOrdersPage_Filter_FulfillmentAttention_Should_ReturnPaidAndPartiallyShipped()
    {
        await using var db = OrderQueryTestDbContext.Create();
        db.Set<Order>().AddRange(
            BuildOrder("PAID",    status: OrderStatus.Paid),
            BuildOrder("PSHIP",   status: OrderStatus.PartiallyShipped),
            BuildOrder("SHIPPED", status: OrderStatus.Shipped));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, null, OrderQueueFilter.FulfillmentAttention, TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Select(x => x.OrderNumber).Should().Contain("PAID").And.Contain("PSHIP");
    }

    [Fact]
    public async Task GetOrdersPage_Search_Should_MatchByOrderNumber()
    {
        await using var db = OrderQueryTestDbContext.Create();
        db.Set<Order>().AddRange(
            BuildOrder("ORD-ALPHA"),
            BuildOrder("ORD-BETA"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "ALPHA", OrderQueueFilter.All, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].OrderNumber.Should().Be("ORD-ALPHA");
    }

    [Fact]
    public async Task GetOrdersPage_Should_IncludePaymentAndShipmentCounts()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CNT",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            RowVersion = new byte[] { 1 }
        });
        db.Set<Payment>().AddRange(
            new Payment { Id = Guid.NewGuid(), OrderId = orderId, Status = PaymentStatus.Captured, Provider = "Stripe", Currency = "EUR", AmountMinor = 1000, RowVersion = new byte[] { 1 } },
            new Payment { Id = Guid.NewGuid(), OrderId = orderId, Status = PaymentStatus.Failed, Provider = "Stripe", Currency = "EUR", AmountMinor = 1000, RowVersion = new byte[] { 1 } });
        db.Set<Shipment>().Add(new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrdersPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].PaymentCount.Should().Be(2);
        items[0].FailedPaymentCount.Should().Be(1);
        items[0].ShipmentCount.Should().Be(1);
    }

    // ─── GetOrderForViewHandler ───────────────────────────────────────────────

    [Fact]
    public async Task GetOrderForView_Should_ReturnDetail_WhenFound()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-DETAIL",
            Currency = "EUR",
            GrandTotalGrossMinor = 1500,
            Status = OrderStatus.Paid,
            BillingAddressJson = "{\"city\":\"Berlin\"}",
            ShippingAddressJson = "{\"city\":\"Berlin\"}",
            RowVersion = new byte[] { 1 }
        });
        db.Set<OrderLine>().Add(new OrderLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            VariantId = variantId,
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 2,
            UnitPriceNetMinor = 630,
            VatRate = 0.19m,
            UnitPriceGrossMinor = 750,
            LineTaxMinor = 240,
            LineGrossMinor = 1500,
            AddOnValueIdsJson = "[]",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderForViewHandler(db);
        var result = await handler.HandleAsync(orderId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("ORD-DETAIL");
        result.Lines.Should().HaveCount(1);
        result.Lines[0].Name.Should().Be("Widget");
    }

    [Fact]
    public async Task GetOrderForView_Should_ReturnNull_WhenNotFound()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var handler = new GetOrderForViewHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderForView_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-DEL",
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            RowVersion = new byte[] { 1 },
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderForViewHandler(db);
        var result = await handler.HandleAsync(orderId, TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted orders are excluded");
    }

    [Fact]
    public async Task GetOrderForView_Should_IncludeNonDeletedLines_And_ExcludeDeletedLines()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrderEntity(orderId));
        db.Set<OrderLine>().AddRange(
            BuildOrderLine(orderId, "Active Line"),
            BuildOrderLine(orderId, "Deleted Line", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderForViewHandler(db);
        var result = await handler.HandleAsync(orderId, TestContext.Current.CancellationToken);

        result!.Lines.Should().HaveCount(1, "deleted lines are filtered out");
        result.Lines[0].Name.Should().Be("Active Line");
    }

    // ─── GetOrderPaymentsPageHandler ──────────────────────────────────────────

    [Fact]
    public async Task GetOrderPaymentsPage_Should_ReturnNonDeletedPayments()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrderEntity(orderId));
        db.Set<Payment>().AddRange(
            BuildPayment(orderId, PaymentStatus.Captured),
            BuildPayment(orderId, PaymentStatus.Failed),
            BuildPayment(orderId, PaymentStatus.Captured, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderPaymentsPageHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "deleted payment excluded");
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderPaymentsPage_Filter_Failed_Should_ReturnOnlyFailed()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrderEntity(orderId));
        db.Set<Payment>().AddRange(
            BuildPayment(orderId, PaymentStatus.Captured),
            BuildPayment(orderId, PaymentStatus.Failed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderPaymentsPageHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, PaymentQueueFilter.Failed, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task GetOrderPaymentsPage_Should_ComputeRefundedAmount()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrderEntity(orderId));
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Status = PaymentStatus.Refunded,
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 2000,
            RowVersion = new byte[] { 1 }
        });
        db.Set<Refund>().Add(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            OrderId = orderId,
            AmountMinor = 800,
            Currency = "EUR",
            Status = RefundStatus.Completed,
            Reason = "Partial return",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderPaymentsPageHandler(db);
        var (items, _) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        items[0].RefundedAmountMinor.Should().Be(800);
        items[0].NetCapturedAmountMinor.Should().Be(1200);
    }

    [Fact]
    public async Task GetOrderPaymentsPage_Should_ReturnEmpty_WhenOrderHasNoPayments()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrderEntity(orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrderPaymentsPageHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── GetMyOrdersPageHandler ───────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrdersPage_Should_ReturnOnlyCurrentUserOrders()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Set<Order>().AddRange(
            BuildOrder("MY-ORD-1", userId: userId),
            BuildOrder("MY-ORD-2", userId: userId),
            BuildOrder("OTHER-ORD", userId: otherUserId),
            BuildOrder("ANON-ORD")); // no userId
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = CreateCurrentUser(userId);
        var handler = new GetMyOrdersPageHandler(db, currentUser);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(2, "only current user's orders are returned");
        items.Select(x => x.OrderNumber).Should().BeEquivalentTo(new[] { "MY-ORD-1", "MY-ORD-2" });
    }

    [Fact]
    public async Task GetMyOrdersPage_Should_ExcludeSoftDeleted()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<Order>().AddRange(
            BuildOrder("ACTIVE",  userId: userId),
            BuildOrder("DELETED", userId: userId, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyOrdersPageHandler(db, CreateCurrentUser(userId));
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].OrderNumber.Should().Be("ACTIVE");
    }

    // ─── GetMyOrderForViewHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrderForView_Should_ReturnNull_ForEmptyId()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var handler = new GetMyOrderForViewHandler(db, CreateCurrentUser(Guid.NewGuid()));

        var result = await handler.HandleAsync(Guid.Empty, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("empty Guid is always rejected");
    }

    [Fact]
    public async Task GetMyOrderForView_Should_ReturnNull_WhenOrderBelongsToOtherUser()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrder("SOMEONE-ELSE", userId: otherUserId, id: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyOrderForViewHandler(db, CreateCurrentUser(Guid.NewGuid()));
        var result = await handler.HandleAsync(orderId, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("cannot view another user's order");
    }

    [Fact]
    public async Task GetMyOrderForView_Should_ReturnDetail_ForOwner()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrder("MY-DETAIL", userId: userId, id: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyOrderForViewHandler(db, CreateCurrentUser(userId));
        var result = await handler.HandleAsync(orderId, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("MY-DETAIL");
    }

    [Fact]
    public async Task GetMyOrderForView_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = OrderQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrder("DELETED", userId: userId, id: orderId, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyOrderForViewHandler(db, CreateCurrentUser(userId));
        var result = await handler.HandleAsync(orderId, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── Shared builders ──────────────────────────────────────────────────────

    private static Order BuildOrder(
        string orderNumber,
        OrderStatus status = OrderStatus.Created,
        bool isDeleted = false,
        Guid? userId = null,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OrderNumber = orderNumber,
        Currency = "EUR",
        Status = status,
        IsDeleted = isDeleted,
        UserId = userId,
        BillingAddressJson = "{}",
        ShippingAddressJson = "{}",
        RowVersion = new byte[] { 1 }
    };

    private static Order BuildOrderEntity(Guid id) => new()
    {
        Id = id,
        OrderNumber = $"ORD-{id:N}",
        Currency = "EUR",
        Status = OrderStatus.Created,
        BillingAddressJson = "{}",
        ShippingAddressJson = "{}",
        RowVersion = new byte[] { 1 }
    };

    private static OrderLine BuildOrderLine(Guid orderId, string name, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        VariantId = Guid.NewGuid(),
        Name = name,
        Sku = "SKU-1",
        Quantity = 1,
        UnitPriceNetMinor = 1000,
        VatRate = 0.19m,
        UnitPriceGrossMinor = 1190,
        LineTaxMinor = 190,
        LineGrossMinor = 1190,
        AddOnValueIdsJson = "[]",
        IsDeleted = isDeleted,
        RowVersion = new byte[] { 1 }
    };

    private static Payment BuildPayment(Guid orderId, PaymentStatus status, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Status = status,
        Provider = "Stripe",
        Currency = "EUR",
        AmountMinor = 1000,
        IsDeleted = isDeleted,
        RowVersion = new byte[] { 1 }
    };

    private static ICurrentUserService CreateCurrentUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.GetCurrentUserId()).Returns(userId);
        return mock.Object;
    }

    // ─── Test DbContext ───────────────────────────────────────────────────────

    private sealed class OrderQueryTestDbContext : DbContext, IAppDbContext
    {
        private OrderQueryTestDbContext(DbContextOptions<OrderQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static OrderQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<OrderQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_order_query_tests_{Guid.NewGuid()}")
                .Options;
            return new OrderQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderNumber).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.BillingAddressJson).IsRequired();
                b.Property(x => x.ShippingAddressJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId);
                b.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.OrderId);
                b.HasMany(x => x.Shipments).WithOne().HasForeignKey(s => s.OrderId);
            });

            modelBuilder.Entity<OrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Sku).IsRequired();
                b.Property(x => x.AddOnValueIdsJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Shipment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Carrier).IsRequired();
                b.Property(x => x.Service).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.ShipmentId);
                b.Ignore(x => x.CarrierEvents);
            });

            modelBuilder.Entity<ShipmentLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Refund>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Reason).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Invoice>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.InvoiceId);
            });

            modelBuilder.Entity<InvoiceLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
