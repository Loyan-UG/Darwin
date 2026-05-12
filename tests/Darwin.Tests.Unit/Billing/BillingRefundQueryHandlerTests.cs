using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for <see cref="GetRefundsPageHandler"/> and
/// <see cref="GetRefundOpsSummaryHandler"/>.
/// </summary>
public sealed class BillingRefundQueryHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static readonly Guid DefaultBusinessId = Guid.NewGuid();
    private static readonly DateTime FixedNow = new(2030, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Payment MakePayment(
        Guid? businessId = null,
        string provider = "Manual",
        bool isDeleted = false,
        string? providerTransactionRef = null,
        string? providerPaymentIntentRef = null,
        string? providerCheckoutSessionRef = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId ?? DefaultBusinessId,
            AmountMinor = 10_000L,
            Currency = "EUR",
            Status = PaymentStatus.Completed,
            Provider = provider,
            IsDeleted = isDeleted,
            ProviderTransactionRef = providerTransactionRef,
            ProviderPaymentIntentRef = providerPaymentIntentRef,
            ProviderCheckoutSessionRef = providerCheckoutSessionRef,
            CreatedAtUtc = FixedNow.AddDays(-5)
        };

    private static Refund MakeRefund(
        Guid paymentId,
        RefundStatus status = RefundStatus.Completed,
        bool isDeleted = false,
        long amountMinor = 2000L,
        Guid? orderId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            OrderId = orderId,
            AmountMinor = amountMinor,
            Currency = "EUR",
            Reason = "Customer request",
            Status = status,
            Provider = "Manual",
            IsDeleted = isDeleted,
            CreatedAtUtc = FixedNow.AddDays(-2)
        };

    private static RefundQueryTestDbContext CreateDb() =>
        RefundQueryTestDbContext.Create();

    private static GetRefundsPageHandler CreatePageHandler(RefundQueryTestDbContext db) =>
        new(db, new RefundQueryFixedClock(FixedNow));

    private static GetRefundOpsSummaryHandler CreateOpsSummaryHandler(RefundQueryTestDbContext db) =>
        new(db);

    // ═══════════════════════════════════════════════════════════════════════
    // GetRefundsPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRefundsPage_Should_ReturnEmpty_WhenNoRefundsExist()
    {
        await using var db = CreateDb();
        var handler = CreatePageHandler(db);

        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetRefundsPage_Should_ExcludeSoftDeletedRefunds()
    {
        await using var db = CreateDb();
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Completed),
            MakeRefund(payment.Id, status: RefundStatus.Completed, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1, "soft-deleted refunds are excluded");
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetRefundsPage_Should_ExcludeRefundsFromOtherBusiness()
    {
        await using var db = CreateDb();
        var payment1 = MakePayment(businessId: DefaultBusinessId);
        var payment2 = MakePayment(businessId: Guid.NewGuid());
        db.Set<Payment>().AddRange(payment1, payment2);
        db.Set<Refund>().AddRange(
            MakeRefund(payment1.Id),
            MakeRefund(payment2.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only refunds for DefaultBusinessId should appear");
    }

    [Fact]
    public async Task GetRefundsPage_Should_ExcludeRefundsWithSoftDeletedPayment()
    {
        await using var db = CreateDb();
        var activePayment = MakePayment();
        var deletedPayment = MakePayment(isDeleted: true);
        db.Set<Payment>().AddRange(activePayment, deletedPayment);
        db.Set<Refund>().AddRange(
            MakeRefund(activePayment.Id),
            MakeRefund(deletedPayment.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "refunds linked to soft-deleted payments are excluded");
    }

    [Fact]
    public async Task GetRefundsPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = CreateDb();
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().Add(MakeRefund(payment.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);

        var (items0, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);
        items0.Should().HaveCount(1, "page < 1 is clamped to 1");

        var (items1, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 1, pageSize: 0, ct: TestContext.Current.CancellationToken);
        items1.Should().HaveCount(1, "pageSize < 1 is clamped to 20");
    }

    [Fact]
    public async Task GetRefundsPage_Should_FilterPending()
    {
        await using var db = CreateDb();
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Pending),
            MakeRefund(payment.Id, status: RefundStatus.Completed),
            MakeRefund(payment.Id, status: RefundStatus.Failed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: BillingRefundQueueFilter.Pending,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(RefundStatus.Pending);
    }

    [Fact]
    public async Task GetRefundsPage_Should_FilterCompleted()
    {
        await using var db = CreateDb();
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Completed),
            MakeRefund(payment.Id, status: RefundStatus.Failed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: BillingRefundQueueFilter.Completed,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task GetRefundsPage_Should_FilterFailed()
    {
        await using var db = CreateDb();
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Failed),
            MakeRefund(payment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: BillingRefundQueueFilter.Failed,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(RefundStatus.Failed);
    }

    [Fact]
    public async Task GetRefundsPage_Should_FilterStripe()
    {
        await using var db = CreateDb();
        var stripePayment = MakePayment(provider: "Stripe");
        var manualPayment = MakePayment(provider: "Manual");
        db.Set<Payment>().AddRange(stripePayment, manualPayment);
        db.Set<Refund>().AddRange(
            MakeRefund(stripePayment.Id, status: RefundStatus.Completed),
            MakeRefund(manualPayment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: BillingRefundQueueFilter.Stripe,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().PaymentProvider.Should().Be("Stripe");
    }

    [Fact]
    public async Task GetRefundsPage_Should_FilterNeedsSupport_IncludesPendingAndFailed()
    {
        await using var db = CreateDb();
        var payment = MakePayment(provider: "Manual");
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Pending),
            MakeRefund(payment.Id, status: RefundStatus.Failed),
            MakeRefund(payment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: BillingRefundQueueFilter.NeedsSupport,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "NeedsSupport includes Pending and Failed refunds");
        items.Select(x => x.Status)
            .Should().BeEquivalentTo(new[] { RefundStatus.Pending, RefundStatus.Failed });
    }

    [Fact]
    public async Task GetRefundsPage_Should_EnrichOrderNumber()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-REFUND-001", Currency = "EUR" });
        var payment = MakePayment();
        db.Set<Payment>().Add(payment);
        var refund = MakeRefund(payment.Id, orderId: orderId);
        db.Set<Refund>().Add(refund);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, _) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Single().OrderNumber.Should().Be("ORD-REFUND-001");
    }

    [Fact]
    public async Task GetRefundsPage_Should_MapPaymentProviderFields()
    {
        await using var db = CreateDb();
        var payment = MakePayment(
            provider: "Stripe",
            providerTransactionRef: "ch_test123",
            providerPaymentIntentRef: "pi_test456",
            providerCheckoutSessionRef: "cs_test789");
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().Add(MakeRefund(payment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, _) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.PaymentProvider.Should().Be("Stripe");
        item.PaymentProviderReference.Should().Be("ch_test123");
        item.PaymentProviderPaymentIntentRef.Should().Be("pi_test456");
        item.PaymentProviderCheckoutSessionRef.Should().Be("cs_test789");
        item.IsStripe.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetRefundOpsSummaryHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRefundOpsSummary_Should_ReturnZeroCounts_WhenNoRefundsExist()
    {
        await using var db = CreateDb();
        var handler = CreateOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(0);
        summary.CompletedCount.Should().Be(0);
        summary.FailedCount.Should().Be(0);
        summary.StripeCount.Should().Be(0);
        summary.NeedsSupportCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRefundOpsSummary_Should_ExcludeSoftDeletedRefunds()
    {
        await using var db = CreateDb();
        var payment = MakePayment(provider: "Stripe");
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Completed),
            MakeRefund(payment.Id, status: RefundStatus.Completed, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.CompletedCount.Should().Be(1, "soft-deleted refunds are excluded");
        summary.StripeCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRefundOpsSummary_Should_CountAllStatusAndProviderGroups()
    {
        await using var db = CreateDb();
        var stripePayment = MakePayment(provider: "Stripe", providerTransactionRef: "ch_live");
        var manualPayment = MakePayment(provider: "Manual");
        db.Set<Payment>().AddRange(stripePayment, manualPayment);
        db.Set<Refund>().AddRange(
            MakeRefund(stripePayment.Id, status: RefundStatus.Pending),
            MakeRefund(stripePayment.Id, status: RefundStatus.Completed),
            MakeRefund(manualPayment.Id, status: RefundStatus.Failed),
            MakeRefund(manualPayment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(1);
        summary.CompletedCount.Should().Be(2);
        summary.FailedCount.Should().Be(1);
        summary.StripeCount.Should().Be(2, "two refunds belong to the Stripe payment");
        summary.NeedsSupportCount.Should().Be(2, "Pending and Failed need support");
    }

    [Fact]
    public async Task GetRefundOpsSummary_Should_OnlyScopeToMatchingBusiness()
    {
        await using var db = CreateDb();
        var myPayment = MakePayment(businessId: DefaultBusinessId);
        var otherPayment = MakePayment(businessId: Guid.NewGuid());
        db.Set<Payment>().AddRange(myPayment, otherPayment);
        db.Set<Refund>().AddRange(
            MakeRefund(myPayment.Id, status: RefundStatus.Pending),
            MakeRefund(otherPayment.Id, status: RefundStatus.Pending));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(1, "only refunds for DefaultBusinessId are counted");
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────

    private sealed class RefundQueryFixedClock : IClock
    {
        public RefundQueryFixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class RefundQueryTestDbContext : DbContext, IAppDbContext
    {
        private RefundQueryTestDbContext(DbContextOptions<RefundQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RefundQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RefundQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_refund_query_{Guid.NewGuid()}")
                .Options;
            return new RefundQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Refund>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.PaymentId).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Reason).IsRequired();
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderNumber).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.Lines);
                b.Ignore(x => x.Payments);
                b.Ignore(x => x.Shipments);
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Addresses);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.UserName).IsRequired();
                b.Property(x => x.NormalizedUserName).IsRequired();
                b.Property(x => x.NormalizedEmail).IsRequired();
                b.Property(x => x.PasswordHash).IsRequired();
                b.Property(x => x.SecurityStamp).IsRequired();
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
