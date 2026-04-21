using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Billing;

public sealed class ProcessStripeWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CaptureStorefrontPayment_And_RecordStripeEvent()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-STRIPE-1001",
            Currency = "EUR",
            GrandTotalGrossMinor = 3200,
            SubtotalNetMinor = 2689,
            TaxTotalMinor = 511,
            Status = OrderStatus.Created
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 3200,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_test_123",
            Status = PaymentStatus.Pending
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync("""
            {
              "id": "evt_test_success",
              "type": "payment_intent.succeeded",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "pi_test_123",
                  "latest_charge": "ch_test_123"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MatchedPaymentId.Should().Be(paymentId);

        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        var eventLog = await db.Set<EventLog>().SingleAsync(x => x.IdempotencyKey == "evt_test_success", TestContext.Current.CancellationToken);

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.ProviderTransactionRef.Should().Be("ch_test_123");
        payment.PaidAtUtc.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Paid);
        eventLog.Type.Should().Be("StripeWebhook:payment_intent.succeeded");
    }

    [Fact]
    public async Task HandleAsync_Should_BeIdempotent_ForDuplicateStripeEventIds()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        const string payload = """
            {
              "id": "evt_test_duplicate",
              "type": "customer.subscription.updated",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "sub_test_123",
                  "status": "active"
                }
              }
            }
            """;

        var first = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.Value.Should().NotBeNull();
        second.Value!.IsDuplicate.Should().BeTrue();

        var eventLogs = await db.Set<EventLog>().CountAsync(x => x.IdempotencyKey == "evt_test_duplicate", TestContext.Current.CancellationToken);
        eventLogs.Should().Be(1);
    }

    private sealed class StripeWebhookTestDbContext : DbContext, IAppDbContext
    {
        private StripeWebhookTestDbContext(DbContextOptions<StripeWebhookTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static StripeWebhookTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<StripeWebhookTestDbContext>()
                .UseInMemoryDatabase($"darwin_stripe_webhook_tests_{Guid.NewGuid()}")
                .Options;

            return new StripeWebhookTestDbContext(options);
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
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<EventLog>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Type).IsRequired();
                builder.Property(x => x.PropertiesJson).IsRequired();
                builder.Property(x => x.UtmSnapshotJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessSubscription>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SubscriptionInvoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.LinesJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
