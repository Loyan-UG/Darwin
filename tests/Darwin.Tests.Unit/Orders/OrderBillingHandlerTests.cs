using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Verifies order billing workflows so refunds and order-created invoices stay aligned
/// with the shared billing payment aggregate.
/// </summary>
public sealed class OrderBillingHandlerTests
{
    [Fact]
    public async Task AddRefundHandler_Should_MarkPaymentRefunded_WhenFullAmountIsReturned()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-1001",
            Currency = "EUR",
            GrandTotalGrossMinor = 2500,
            SubtotalNetMinor = 2101,
            TaxTotalMinor = 399,
            Status = OrderStatus.Paid
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 2500,
            Currency = "EUR",
            Status = PaymentStatus.Captured,
            Provider = "Stripe"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddRefundHandler(db, new RefundCreateValidator());

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 2500,
            Currency = "EUR",
            Reason = "Customer cancellation"
        }, TestContext.Current.CancellationToken);

        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);

        payment.Status.Should().Be(PaymentStatus.Refunded);
        order.Status.Should().Be(OrderStatus.Refunded);
        refund.Status.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task CreateOrderInvoiceHandler_Should_LinkPayment_AndCaptureAuthorizedPayment()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-1002",
            Currency = "EUR",
            GrandTotalGrossMinor = 1900,
            SubtotalNetMinor = 1597,
            TaxTotalMinor = 303,
            Status = OrderStatus.Paid,
            Lines =
            [
                new OrderLine
                {
                    Id = Guid.NewGuid(),
                    VariantId = Guid.NewGuid(),
                    Name = "Notebook",
                    Sku = "NB-001",
                    Quantity = 1,
                    UnitPriceNetMinor = 1597,
                    VatRate = 0.19m,
                    UnitPriceGrossMinor = 1900,
                    LineTaxMinor = 303,
                    LineGrossMinor = 1900
                }
            ]
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 1900,
            Currency = "EUR",
            Status = PaymentStatus.Authorized,
            Provider = "PayPal"
        });

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Lea",
            LastName = "Fischer",
            Email = "lea.fischer@example.de",
            Phone = "+491701112233"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateOrderInvoiceHandler(db, new OrderInvoiceCreateValidator());

        var invoiceId = await handler.HandleAsync(new OrderInvoiceCreateDto
        {
            OrderId = orderId,
            CustomerId = customerId,
            PaymentId = paymentId
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaymentId.Should().Be(paymentId);
        payment.InvoiceId.Should().Be(invoiceId);
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CustomerId.Should().Be(customerId);
        payment.PaidAtUtc.Should().Be(invoice.PaidAtUtc);
    }

    private sealed class OrderBillingTestDbContext : DbContext, IAppDbContext
    {
        private OrderBillingTestDbContext(DbContextOptions<OrderBillingTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static OrderBillingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<OrderBillingTestDbContext>()
                .UseInMemoryDatabase($"darwin_order_billing_tests_{Guid.NewGuid()}")
                .Options;
            return new OrderBillingTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.BillingAddressJson).IsRequired();
                builder.Property(x => x.ShippingAddressJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
            });

            modelBuilder.Entity<OrderLine>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Sku).IsRequired();
                builder.Property(x => x.AddOnValueIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Refund>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Reason).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Customer>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FirstName).IsRequired();
                builder.Property(x => x.LastName).IsRequired();
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.Phone).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Invoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InvoiceId);
            });

            modelBuilder.Entity<InvoiceLine>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Description).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
