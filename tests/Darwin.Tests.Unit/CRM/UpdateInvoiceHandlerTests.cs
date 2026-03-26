using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Verifies invoice editing behavior so CRM invoice lifecycle changes do not leave
/// stale payment associations or silently drop linked order references.
/// </summary>
public sealed class UpdateInvoiceHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_MovePaymentLink_AndPersistOrderId()
    {
        await using var db = InvoiceTestDbContext.Create();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var oldPaymentId = Guid.NewGuid();
        var newPaymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Anna",
            LastName = "Schmidt",
            Email = "anna.schmidt@example.de",
            Phone = "+491701234567"
        });

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            PaymentId = oldPaymentId,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().AddRange(
            new Payment
            {
                Id = oldPaymentId,
                Provider = "Stripe",
                Currency = "EUR",
                AmountMinor = 1200,
                InvoiceId = invoiceId
            },
            new Payment
            {
                Id = newPaymentId,
                Provider = "PayPal",
                Currency = "EUR",
                AmountMinor = 1500
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator());

        await handler.HandleAsync(new InvoiceEditDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            CustomerId = customerId,
            OrderId = orderId,
            PaymentId = newPaymentId,
            Status = InvoiceStatus.Paid,
            Currency = "EUR",
            TotalNetMinor = 1000,
            TotalTaxMinor = 200,
            TotalGrossMinor = 1200,
            DueDateUtc = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var oldPayment = await db.Set<Payment>().SingleAsync(x => x.Id == oldPaymentId, TestContext.Current.CancellationToken);
        var newPayment = await db.Set<Payment>().SingleAsync(x => x.Id == newPaymentId, TestContext.Current.CancellationToken);

        invoice.OrderId.Should().Be(orderId);
        invoice.PaymentId.Should().Be(newPaymentId);
        invoice.CustomerId.Should().Be(customerId);
        oldPayment.InvoiceId.Should().BeNull();
        newPayment.InvoiceId.Should().Be(invoiceId);
        newPayment.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Payment_AlreadyLinkedToAnotherInvoice()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var otherInvoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 9, 8, 7, 6 };

        db.Set<Invoice>().AddRange(
            new Invoice
            {
                Id = invoiceId,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = rowVersion.ToArray()
            },
            new Invoice
            {
                Id = otherInvoiceId,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[] { 4, 5, 6, 7 }
            });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 2200,
            InvoiceId = otherInvoiceId
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator());

        var act = () => handler.HandleAsync(new InvoiceEditDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            PaymentId = paymentId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 2000,
            TotalTaxMinor = 200,
            TotalGrossMinor = 2200,
            DueDateUtc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Linked payment is already assigned to another invoice.");
    }

    [Fact]
    public async Task HandleAsync_Should_CreatePartialRefund_WithoutCancellingInvoice()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 2, 4, 2 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            PaymentId = paymentId,
            Status = InvoiceStatus.Paid,
            Currency = "EUR",
            TotalGrossMinor = 1200,
            DueDateUtc = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 1200,
            Status = PaymentStatus.Captured,
            PaidAtUtc = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc)
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateInvoiceRefundHandler(db, new InvoiceRefundCreateValidator());
        await handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoiceId,
            RowVersion = rowVersion,
            AmountMinor = 300,
            Currency = "EUR",
            Reason = "Partial goodwill refund"
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var refunds = await db.Set<Refund>().Where(x => x.PaymentId == paymentId).ToListAsync(TestContext.Current.CancellationToken);

        refunds.Should().ContainSingle();
        refunds[0].AmountMinor.Should().Be(300);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Captured);
        invoice.PaidAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_FullyRefund_AndCancelInvoice()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 7, 7, 7, 7 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            PaymentId = paymentId,
            Status = InvoiceStatus.Paid,
            Currency = "EUR",
            TotalGrossMinor = 1800,
            DueDateUtc = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "PayPal",
            Currency = "EUR",
            AmountMinor = 1800,
            Status = PaymentStatus.Completed,
            PaidAtUtc = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc)
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateInvoiceRefundHandler(db, new InvoiceRefundCreateValidator());
        await handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoiceId,
            RowVersion = rowVersion,
            AmountMinor = 1800,
            Currency = "EUR",
            Reason = "Full cancellation"
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.PaidAtUtc.Should().BeNull();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.PaidAtUtc.Should().BeNull();
    }

    private sealed class InvoiceTestDbContext : DbContext, IAppDbContext
    {
        private InvoiceTestDbContext(DbContextOptions<InvoiceTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InvoiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InvoiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_invoice_tests_{Guid.NewGuid()}")
                .Options;
            return new InvoiceTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
