using Darwin.Application;
using Darwin.Application.Abstractions.Payments;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer());

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

        var localizer = new TestStringLocalizer();
        var handler = new CreateOrderInvoiceHandler(db, new OrderInvoiceCreateValidator(localizer), localizer);

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

    [Fact]
    public async Task AddRefundHandler_Should_CallStripeProvider_AndStoreProviderRefundReference_WhenPaymentHasProviderPaymentIntentRef()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-PROV-001",
            Currency = "EUR",
            GrandTotalGrossMinor = 5000,
            SubtotalNetMinor = 4202,
            TaxTotalMinor = 798,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 5000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_test_provider",
            Status = PaymentStatus.Captured
        });
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            StripeEnabled = true,
            StripeSecretKey = "sk_test_fake_key_for_unit_tests"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var providerClient = new FakeRefundProviderClient(new RefundProviderResult
        {
            ProviderRefundReference = "re_test_prov_001",
            ProviderPaymentReference = "pi_test_provider",
            ProviderStatus = "succeeded",
            IsCompleted = true
        });

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer(), refundProviderClient: providerClient);

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 5000,
            Currency = "EUR",
            Reason = "Customer request"
        }, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);

        providerClient.CallCount.Should().Be(1, "the Stripe provider must be called once for Stripe payments with a provider reference");
        refund.ProviderRefundReference.Should().Be("re_test_prov_001");
        refund.ProviderStatus.Should().Be("succeeded");
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AddRefundHandler_Should_StoreFailureReason_WhenProviderCallFails()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-PROV-002",
            Currency = "EUR",
            GrandTotalGrossMinor = 2000,
            SubtotalNetMinor = 1681,
            TaxTotalMinor = 319,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 2000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_fail_ref",
            Status = PaymentStatus.Captured
        });
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            StripeEnabled = true,
            StripeSecretKey = "sk_test_fake_key_for_unit_tests"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var providerClient = new FakeRefundProviderClient(new RefundProviderResult
        {
            ProviderRefundReference = "re_failed_prov",
            ProviderStatus = "failed",
            IsFailed = true,
            FailureReason = "insufficient_funds"
        });

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer(), refundProviderClient: providerClient);

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 2000,
            Currency = "EUR",
            Reason = "Damaged goods"
        }, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);

        refund.Status.Should().Be(RefundStatus.Failed);
        refund.ProviderStatus.Should().Be("failed");
        refund.FailureReason.Should().Be("insufficient_funds");
    }

    [Fact]
    public async Task AddRefundHandler_Should_MarkRefundFailed_WhenProviderClientIsNull()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-PROV-003",
            Currency = "EUR",
            GrandTotalGrossMinor = 1500,
            SubtotalNetMinor = 1261,
            TaxTotalMinor = 239,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 1500,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_no_client_ref",
            Status = PaymentStatus.Captured
        });
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            StripeEnabled = true,
            StripeSecretKey = "sk_test_fake_key_for_unit_tests"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer(), refundProviderClient: null);

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 1500,
            Currency = "EUR",
            Reason = "Return"
        }, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);

        refund.Status.Should().Be(RefundStatus.Failed,
            "the handler must mark refund Failed when IRefundProviderClient is null but provider should be called");
        refund.ProviderStatus.Should().Be("request_failed");
    }

    [Fact]
    public async Task AddRefundHandler_Should_NotCallProvider_WhenPaymentIsNotStripe()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-PROV-004",
            Currency = "EUR",
            GrandTotalGrossMinor = 3000,
            SubtotalNetMinor = 2521,
            TaxTotalMinor = 479,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 3000,
            Currency = "EUR",
            Provider = "PayPal",
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var providerClient = new FakeRefundProviderClient(new RefundProviderResult
        {
            ProviderRefundReference = "should_not_be_called",
            IsCompleted = true
        });

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer(), refundProviderClient: providerClient);

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 3000,
            Currency = "EUR",
            Reason = "Not needed"
        }, TestContext.Current.CancellationToken);

        providerClient.CallCount.Should().Be(0,
            "the provider must not be called for non-Stripe payments");

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);
        refund.Status.Should().Be(RefundStatus.Completed,
            "non-Stripe refunds are completed locally without provider involvement");
    }

    [Fact]
    public async Task AddRefundHandler_Should_NotCallProvider_WhenStripePaymentHasNoProviderReference()
    {
        await using var db = OrderBillingTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-PROV-005",
            Currency = "EUR",
            GrandTotalGrossMinor = 800,
            SubtotalNetMinor = 672,
            TaxTotalMinor = 128,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 800,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = null,
            ProviderTransactionRef = null,
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var providerClient = new FakeRefundProviderClient(new RefundProviderResult
        {
            ProviderRefundReference = "should_not_be_called",
            IsCompleted = true
        });

        var handler = new AddRefundHandler(db, new RefundCreateValidator(), new TestStringLocalizer(), refundProviderClient: providerClient);

        await handler.HandleAsync(new RefundCreateDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            AmountMinor = 800,
            Currency = "EUR",
            Reason = "Manual refund"
        }, TestContext.Current.CancellationToken);

        providerClient.CallCount.Should().Be(0,
            "the provider must not be called when the Stripe payment has no provider reference");

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);
        refund.Status.Should().Be(RefundStatus.Completed,
            "Stripe payments without a provider reference are completed locally");
    }

    private sealed class FakeRefundProviderClient : IRefundProviderClient
    {
        private readonly RefundProviderResult _result;
        public int CallCount { get; private set; }

        public FakeRefundProviderClient(RefundProviderResult result)
        {
            _result = result;
        }

        public Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
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

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
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
