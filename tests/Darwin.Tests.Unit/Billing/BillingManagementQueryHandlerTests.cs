using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for <see cref="GetPaymentsPageHandler"/>,
/// <see cref="GetPaymentOpsSummaryHandler"/>, and
/// <see cref="GetPaymentForEditHandler"/>.
/// </summary>
public sealed class BillingManagementQueryHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static readonly Guid DefaultBusinessId = Guid.NewGuid();
    private static readonly DateTime FixedNow = new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Payment MakePayment(
        Guid? businessId = null,
        PaymentStatus status = PaymentStatus.Completed,
        string provider = "Manual",
        bool isDeleted = false,
        Guid? orderId = null,
        Guid? invoiceId = null,
        Guid? customerId = null,
        Guid? userId = null,
        string? providerTransactionRef = null,
        string? providerPaymentIntentRef = null,
        string? providerCheckoutSessionRef = null,
        string? failureReason = null,
        DateTime? paidAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId ?? DefaultBusinessId,
            AmountMinor = 5000L,
            Currency = "EUR",
            Status = status,
            Provider = provider,
            IsDeleted = isDeleted,
            OrderId = orderId,
            InvoiceId = invoiceId,
            CustomerId = customerId,
            UserId = userId,
            ProviderTransactionRef = providerTransactionRef,
            ProviderPaymentIntentRef = providerPaymentIntentRef,
            ProviderCheckoutSessionRef = providerCheckoutSessionRef,
            FailureReason = failureReason,
            PaidAtUtc = paidAtUtc,
            CreatedAtUtc = FixedNow.AddDays(-1)
        };

    private static Refund MakeRefund(
        Guid paymentId,
        RefundStatus status = RefundStatus.Completed,
        bool isDeleted = false,
        long amountMinor = 1000L) =>
        new()
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            AmountMinor = amountMinor,
            Currency = "EUR",
            Reason = "Test reason",
            Status = status,
            Provider = "Manual",
            IsDeleted = isDeleted,
            CreatedAtUtc = FixedNow.AddHours(-2)
        };

    private static BillingManagementQueryTestDbContext CreateDb() =>
        BillingManagementQueryTestDbContext.Create();

    private static GetPaymentsPageHandler CreatePaymentsHandler(
        BillingManagementQueryTestDbContext db) =>
        new(db, new BillingQueryFixedClock(FixedNow));

    private static GetPaymentOpsSummaryHandler CreateOpsSummaryHandler(
        BillingManagementQueryTestDbContext db) =>
        new(db);

    private static GetPaymentForEditHandler CreatePaymentForEditHandler(
        BillingManagementQueryTestDbContext db) =>
        new(db, new BillingQueryFixedClock(FixedNow));

    // ═══════════════════════════════════════════════════════════════════════
    // GetPaymentsPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPaymentsPage_Should_ReturnEmpty_WhenNoPaymentsExist()
    {
        await using var db = CreateDb();
        var handler = CreatePaymentsHandler(db);

        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetPaymentsPage_Should_ExcludeSoftDeletedPayments()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(status: PaymentStatus.Completed),
            MakePayment(status: PaymentStatus.Completed, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1, "soft-deleted payments must be excluded");
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPaymentsPage_Should_ExcludeOtherBusiness_Payments()
    {
        await using var db = CreateDb();
        var otherBusinessId = Guid.NewGuid();
        db.Set<Payment>().AddRange(
            MakePayment(businessId: DefaultBusinessId, status: PaymentStatus.Completed),
            MakePayment(businessId: otherBusinessId, status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1, "payments from other businesses must be excluded");
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPaymentsPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = CreateDb();
        db.Set<Payment>().Add(MakePayment(status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);

        var (items1, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);
        items1.Should().HaveCount(1, "page < 1 should be clamped to 1");

        var (items2, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 1, pageSize: 0, ct: TestContext.Current.CancellationToken);
        items2.Should().HaveCount(1, "pageSize < 1 should be clamped to 20");

        var (items3, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 1, pageSize: 9999, ct: TestContext.Current.CancellationToken);
        items3.Should().HaveCount(1, "pageSize > 200 should be clamped to 200 and still return data");
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterPending_IncludesAuthorized()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(status: PaymentStatus.Pending),
            MakePayment(status: PaymentStatus.Authorized),
            MakePayment(status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.Pending,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Pending filter includes both Pending and Authorized status");
        items.Select(x => x.Status).Should().BeEquivalentTo(
            new[] { PaymentStatus.Pending, PaymentStatus.Authorized },
            because: "Pending filter returns Pending and Authorized payments");
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterFailed()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(status: PaymentStatus.Failed),
            MakePayment(status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.Failed,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterUnlinked_ExcludesOrderAndInvoiceLinked()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-001", Currency = "EUR" });
        db.Set<Payment>().AddRange(
            MakePayment(status: PaymentStatus.Completed),
            MakePayment(status: PaymentStatus.Completed, orderId: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.Unlinked,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only payments without Order or Invoice links should appear");
        items.Single().OrderId.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterStripe()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed),
            MakePayment(provider: "Manual", status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.Stripe,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Provider.Should().Be("Stripe");
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterMissingProviderRef()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed),
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed,
                providerTransactionRef: "ch_test123"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.MissingProviderRef,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only payments with no provider refs should appear");
        items.Single().ProviderTransactionRef.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterProviderLinked()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", providerPaymentIntentRef: "pi_test"),
            MakePayment(provider: "Manual", status: PaymentStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.ProviderLinked,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only payments with at least one provider reference should appear");
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterFailedStripe()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", status: PaymentStatus.Failed),
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed),
            MakePayment(provider: "Manual", status: PaymentStatus.Failed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.FailedStripe,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Provider.Should().Be("Stripe");
        items.Single().Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task GetPaymentsPage_Should_FilterRefunded_IncludesCompletedRefund()
    {
        await using var db = CreateDb();
        var payment1 = MakePayment(status: PaymentStatus.Refunded, provider: "Manual");
        var payment2 = MakePayment(status: PaymentStatus.Completed, provider: "Manual");
        var payment3 = MakePayment(status: PaymentStatus.Completed, provider: "Manual");
        db.Set<Payment>().AddRange(payment1, payment2, payment3);
        db.Set<Refund>().Add(MakeRefund(payment2.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: PaymentQueueFilter.Refunded,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Refunded filter includes Refunded-status payments AND those with completed refunds");
        items.Select(x => x.Id).Should().BeEquivalentTo(new[] { payment1.Id, payment2.Id });
    }

    [Fact]
    public async Task GetPaymentsPage_Should_EnrichOrderNumber()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-ENRICH-001", Currency = "EUR" });
        var payment = MakePayment(status: PaymentStatus.Completed, orderId: orderId);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, _) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Single().OrderNumber.Should().Be("ORD-ENRICH-001");
    }

    [Fact]
    public async Task GetPaymentsPage_Should_ComputeRefundedAndNetAmounts()
    {
        await using var db = CreateDb();
        var payment = MakePayment(status: PaymentStatus.Completed, provider: "Manual");
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().Add(MakeRefund(payment.Id, status: RefundStatus.Completed, amountMinor: 2000L));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentsHandler(db);
        var (items, _) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.RefundedAmountMinor.Should().Be(2000L);
        item.NetCapturedAmountMinor.Should().Be(payment.AmountMinor - 2000L);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetPaymentOpsSummaryHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPaymentOpsSummary_Should_ReturnZeroCounts_WhenNoPaymentsExist()
    {
        await using var db = CreateDb();
        var handler = CreateOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(0);
        summary.FailedCount.Should().Be(0);
        summary.UnlinkedCount.Should().Be(0);
        summary.ProviderLinkedCount.Should().Be(0);
        summary.RefundedCount.Should().Be(0);
        summary.StripeCount.Should().Be(0);
        summary.MissingProviderRefCount.Should().Be(0);
        summary.FailedStripeCount.Should().Be(0);
        summary.NeedsReconciliationCount.Should().Be(0);
        summary.DisputeFollowUpCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPaymentOpsSummary_Should_ExcludeSoftDeletedPayments()
    {
        await using var db = CreateDb();
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed,
                providerTransactionRef: "ch_live"),
            MakePayment(provider: "Stripe", status: PaymentStatus.Completed,
                providerTransactionRef: "ch_deleted", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.StripeCount.Should().Be(1, "soft-deleted payments are excluded from all counts");
        summary.ProviderLinkedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPaymentOpsSummary_Should_CountAllStatusGroups()
    {
        await using var db = CreateDb();
        var refundablePayment = MakePayment(provider: "Manual", status: PaymentStatus.Completed);
        db.Set<Payment>().AddRange(
            MakePayment(provider: "Stripe", status: PaymentStatus.Pending,
                providerPaymentIntentRef: "pi_1"),
            MakePayment(provider: "Stripe", status: PaymentStatus.Authorized,
                providerPaymentIntentRef: "pi_2"),
            MakePayment(provider: "Stripe", status: PaymentStatus.Failed,
                providerCheckoutSessionRef: "cs_3"),
            MakePayment(provider: "Manual", status: PaymentStatus.Refunded),
            refundablePayment,
            MakePayment(provider: "Manual", status: PaymentStatus.Completed));
        db.Set<Refund>().Add(MakeRefund(refundablePayment.Id, status: RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(2, "Pending and Authorized both count as Pending");
        summary.FailedCount.Should().Be(1);
        summary.StripeCount.Should().Be(3);
        summary.ProviderLinkedCount.Should().Be(3, "three Stripe payments have provider refs");
        summary.RefundedCount.Should().Be(2, "Refunded-status plus one with completed refund");
    }

    [Fact]
    public async Task GetPaymentOpsSummary_Should_OnlyScopeToMatchingBusiness()
    {
        await using var db = CreateDb();
        var otherBusinessId = Guid.NewGuid();
        db.Set<Payment>().AddRange(
            MakePayment(businessId: DefaultBusinessId, status: PaymentStatus.Failed),
            MakePayment(businessId: otherBusinessId, status: PaymentStatus.Failed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.FailedCount.Should().Be(1, "only the DefaultBusinessId payment should be counted");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetPaymentForEditHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPaymentForEdit_Should_ReturnNull_WhenPaymentNotFound()
    {
        await using var db = CreateDb();
        var handler = CreatePaymentForEditHandler(db);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_ReturnNull_WhenPaymentSoftDeleted()
    {
        await using var db = CreateDb();
        var payment = MakePayment(status: PaymentStatus.Completed, isDeleted: true);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(
            payment.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_ReturnProjection_WithBasicFields()
    {
        await using var db = CreateDb();
        var payment = MakePayment(
            provider: "Manual",
            status: PaymentStatus.Completed,
            paidAtUtc: FixedNow.AddHours(-5));
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(
            payment.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.BusinessId.Should().Be(DefaultBusinessId);
        result.AmountMinor.Should().Be(payment.AmountMinor);
        result.Currency.Should().Be("EUR");
        result.Status.Should().Be(PaymentStatus.Completed);
        result.Provider.Should().Be("Manual");
        result.IsStripe.Should().BeFalse();
        result.Refunds.Should().BeEmpty("no refunds were added");
        result.ProviderEvents.Should().BeEmpty("non-Stripe payments have no provider events");
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_EnrichOrderNumber_WhenOrderIdSet()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-EDIT-001", Currency = "EUR" });
        var payment = MakePayment(status: PaymentStatus.Completed, orderId: orderId);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(payment.Id, TestContext.Current.CancellationToken);

        result!.OrderNumber.Should().Be("ORD-EDIT-001");
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_IncludeRefundHistory_OrderedByCreatedDesc()
    {
        await using var db = CreateDb();
        var payment = MakePayment(status: PaymentStatus.Refunded, provider: "Manual");
        db.Set<Payment>().Add(payment);
        var refund1 = MakeRefund(payment.Id, status: RefundStatus.Completed, amountMinor: 1000L);
        refund1.CreatedAtUtc = FixedNow.AddDays(-2);
        var refund2 = MakeRefund(payment.Id, status: RefundStatus.Completed, amountMinor: 2000L);
        refund2.CreatedAtUtc = FixedNow.AddDays(-1);
        db.Set<Refund>().AddRange(refund1, refund2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(payment.Id, TestContext.Current.CancellationToken);

        result!.Refunds.Should().HaveCount(2);
        result.Refunds.First().AmountMinor.Should().Be(2000L, "most recent refund appears first");
        result.RefundedAmountMinor.Should().Be(3000L, "sum of completed refunds");
        result.NetCapturedAmountMinor.Should().Be(payment.AmountMinor - 3000L);
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_ExcludeSoftDeletedRefunds()
    {
        await using var db = CreateDb();
        var payment = MakePayment(status: PaymentStatus.Completed, provider: "Manual");
        db.Set<Payment>().Add(payment);
        db.Set<Refund>().AddRange(
            MakeRefund(payment.Id, status: RefundStatus.Completed, amountMinor: 1500L),
            MakeRefund(payment.Id, status: RefundStatus.Completed, amountMinor: 500L, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(payment.Id, TestContext.Current.CancellationToken);

        result!.Refunds.Should().HaveCount(1, "soft-deleted refunds are excluded");
        result.RefundedAmountMinor.Should().Be(1500L, "only non-deleted completed refunds count");
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_SetIsStripe_ForStripeProvider()
    {
        await using var db = CreateDb();
        var payment = MakePayment(provider: "Stripe", status: PaymentStatus.Completed);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(payment.Id, TestContext.Current.CancellationToken);

        result!.IsStripe.Should().BeTrue();
        result.ProviderEvents.Should().BeEmpty("no EventLog entries have matching refs");
    }

    [Fact]
    public async Task GetPaymentForEdit_Should_EnrichCustomerDisplayName_WhenCustomerIdSet()
    {
        await using var db = CreateDb();
        var customerId = Guid.NewGuid();
        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com"
        });
        var payment = MakePayment(status: PaymentStatus.Completed, customerId: customerId);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePaymentForEditHandler(db);
        var result = await handler.HandleAsync(payment.Id, TestContext.Current.CancellationToken);

        result!.CustomerDisplayName.Should().NotBeNullOrEmpty("customer name must be resolved");
        result.CustomerEmail.Should().Be("alice@example.com");
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────

    private sealed class BillingQueryFixedClock : IClock
    {
        public BillingQueryFixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class BillingManagementQueryTestDbContext : DbContext, IAppDbContext
    {
        private BillingManagementQueryTestDbContext(
            DbContextOptions<BillingManagementQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingManagementQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingManagementQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_mgmt_query_{Guid.NewGuid()}")
                .Options;
            return new BillingManagementQueryTestDbContext(options);
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

            modelBuilder.Entity<Invoice>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.IsDeleted);
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

            modelBuilder.Entity<EventLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Type).IsRequired();
                b.Property(x => x.PropertiesJson).IsRequired();
                b.Property(x => x.UtmSnapshotJson).IsRequired();
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
