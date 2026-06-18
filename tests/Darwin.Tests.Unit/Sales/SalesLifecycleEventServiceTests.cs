using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Sales.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Sales;

public sealed class SalesLifecycleEventServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 12, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecordOrderStatusChanged_Should_Create_Idempotent_Event_And_Audit()
    {
        await using var db = SalesLifecycleTestDbContext.Create();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-EVENT-1",
            BusinessId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "EUR",
            SalesChannel = SalesChannel.WebStorefront,
            Status = OrderStatus.Paid,
            GrandTotalGrossMinor = 11900
        };

        var service = CreateService(db);

        var first = await service.RecordOrderStatusChangedAsync(
            order,
            OrderStatus.Confirmed,
            OrderStatus.Paid,
            FixedNow,
            TestContext.Current.CancellationToken);
        var duplicate = await service.RecordOrderStatusChangedAsync(
            order,
            OrderStatus.Confirmed,
            OrderStatus.Paid,
            FixedNow,
            TestContext.Current.CancellationToken);

        first.Succeeded.Should().BeTrue(first.Error);
        duplicate.Succeeded.Should().BeTrue(duplicate.Error);
        duplicate.Value.Should().Be(first.Value);
        db.Set<BusinessEvent>().Should().ContainSingle(x =>
            x.EventKey == $"sales.order.status_changed:{order.Id:N}:Confirmed:Paid" &&
            x.PayloadJson.Contains("\"salesChannel\":\"WebStorefront\"", StringComparison.Ordinal));
        db.Set<AuditTrail>().Should().ContainSingle(x =>
            x.EntityType == SalesLifecycleEventService.OrderEntityType &&
            x.EntityId == order.Id &&
            x.Action == AuditTrailAction.StatusChanged &&
            x.CorrelationId == $"sales.order.status_changed:{order.Id:N}:Confirmed:Paid");
    }

    [Fact]
    public async Task RecordPaymentAndRefund_Should_Not_Store_Provider_Secrets()
    {
        await using var db = SalesLifecycleTestDbContext.Create();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-EVENT-2",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            GrandTotalGrossMinor = 5000
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            AmountMinor = 5000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_secret_like_value",
            ProviderTransactionRef = "txn_secret_like_value",
            Status = PaymentStatus.Captured
        };
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentId = payment.Id,
            AmountMinor = 1200,
            Currency = "EUR",
            Reason = "Return",
            ProviderRefundReference = "re_secret_like_value",
            Status = RefundStatus.Completed
        };

        var service = CreateService(db);

        (await service.RecordPaymentRecordedAsync(payment, order, FixedNow, TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        (await service.RecordRefundCreatedAsync(refund, payment, order, invoice: null, FixedNow, TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();

        var payloads = await db.Set<BusinessEvent>()
            .Select(x => x.PayloadJson)
            .ToListAsync(TestContext.Current.CancellationToken);

        payloads.Should().HaveCount(2);
        payloads.Should().OnlyContain(x =>
            !x.Contains("secret", StringComparison.OrdinalIgnoreCase) &&
            !x.Contains("ProviderPaymentIntentRef", StringComparison.Ordinal) &&
            !x.Contains("ProviderTransactionRef", StringComparison.Ordinal) &&
            !x.Contains("ProviderRefundReference", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecordInvoiceStatusChanged_Should_Reject_Sensitive_Invoice_Number()
    {
        await using var db = SalesLifecycleTestDbContext.Create();
        var service = CreateService(db);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "secret-token-value",
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalGrossMinor = 1000
        };

        var result = await service.RecordInvoiceStatusChangedAsync(
            invoice,
            InvoiceStatus.Draft,
            InvoiceStatus.Open,
            FixedNow,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        db.Set<BusinessEvent>().Should().BeEmpty();
        db.Set<AuditTrail>().Should().BeEmpty();
    }

    private static SalesLifecycleEventService CreateService(SalesLifecycleTestDbContext db)
        => new(new BusinessEventService(db, new FixedClock(FixedNow)), db);

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class SalesLifecycleTestDbContext : DbContext, IAppDbContext
    {
        private SalesLifecycleTestDbContext(DbContextOptions<SalesLifecycleTestDbContext> options)
            : base(options)
        {
        }

        public static SalesLifecycleTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SalesLifecycleTestDbContext>()
                .UseInMemoryDatabase($"darwin_sales_lifecycle_tests_{Guid.NewGuid():N}")
                .Options;
            return new SalesLifecycleTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusinessEvent>();
            modelBuilder.Entity<AuditTrail>();
        }
    }
}
