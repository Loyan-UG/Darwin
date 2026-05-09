using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Covers <see cref="GetMyInvoicesPageHandler"/> and <see cref="GetMyInvoiceDetailHandler"/>
/// member-facing invoice query behavior.
/// </summary>
public sealed class CrmMemberInvoiceQueryHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Invoice MakeInvoice(
        Guid? id = null,
        string currency = "EUR",
        InvoiceStatus status = InvoiceStatus.Open,
        bool isDeleted = false,
        Guid? customerId = null,
        Guid? orderId = null,
        Guid? paymentId = null,
        Guid? businessId = null,
        long totalGrossMinor = 10000)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Currency = currency,
            Status = status,
            IsDeleted = isDeleted,
            CustomerId = customerId,
            OrderId = orderId,
            PaymentId = paymentId,
            BusinessId = businessId,
            TotalGrossMinor = totalGrossMinor,
            DueDateUtc = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1, 2, 3 }
        };

    private static Customer MakeCustomer(Guid? id = null, Guid? userId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "+49001234567"
        };

    private static Order MakeOrder(Guid? id = null, Guid? userId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}".Substring(0, 12)
        };

    private static Payment MakePayment(Guid? id = null, long amountMinor = 10000)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = amountMinor,
            Status = PaymentStatus.Captured,
            RowVersion = new byte[] { 1 }
        };

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        public FakeCurrentUser(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMyInvoicesPageHandler — user-scoping via Order
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyInvoicesPage_Should_Return_Empty_When_No_Invoices_Exist()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Return_Invoices_Linked_Via_Order()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(MakeOrder(id: orderId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(orderId: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "invoice linked via the user's order must be returned");
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Return_Invoices_Linked_Via_Customer()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(customerId: customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "invoice linked via the user's customer record must be returned");
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Not_Return_Other_Users_Invoices()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var myUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var otherOrderId = Guid.NewGuid();
        db.Set<Order>().Add(MakeOrder(id: otherOrderId, userId: otherUserId));
        db.Set<Invoice>().Add(MakeInvoice(orderId: otherOrderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(myUserId));
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0, "another user's invoice must not be visible");
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Exclude_Soft_Deleted_Invoices()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(MakeOrder(id: orderId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(orderId: orderId, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0, "soft-deleted invoices must not appear in member history");
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Clamp_Page_Param_Below_One()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(MakeOrder(id: orderId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(orderId: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, total) = await handler.HandleAsync(page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "page < 1 must be clamped to 1");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Enrich_With_Order_Number()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = MakeOrder(id: orderId, userId: userId);
        order.OrderNumber = "ORD-2026-001";

        db.Set<Order>().Add(order);
        db.Set<Invoice>().Add(MakeInvoice(orderId: orderId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Single().OrderNumber.Should().Be("ORD-2026-001");
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Compute_Balance_Without_Payment()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(customerId: customerId, status: InvoiceStatus.Draft, totalGrossMinor: 8000));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.BalanceMinor.Should().Be(8000, "Draft invoice with no payment has full balance outstanding");
        item.SettledAmountMinor.Should().Be(0);
        item.RefundedAmountMinor.Should().Be(0);
    }

    [Fact]
    public async Task GetMyInvoicesPage_Should_Compute_Zero_Balance_For_Paid_Invoice_Without_Payment_Record()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(customerId: customerId, status: InvoiceStatus.Paid, totalGrossMinor: 8000));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoicesPageHandler(db, new FakeCurrentUser(userId));
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.SettledAmountMinor.Should().Be(8000, "Paid invoice without payment record defaults to fully settled");
        item.BalanceMinor.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMyInvoiceDetailHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Null_For_Empty_Guid()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.HandleAsync(Guid.Empty, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("empty Guid must short-circuit and return null");
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Null_When_Invoice_Not_Found()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.HandleAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("non-existent invoice must return null");
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Null_For_Another_Users_Invoice()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var myUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(MakeOrder(id: orderId, userId: otherUserId));
        var invoice = MakeInvoice(orderId: orderId);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(myUserId));
        var result = await handler.HandleAsync(invoice.Id, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("invoice belonging to another user must not be accessible");
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Null_When_Invoice_Is_Soft_Deleted()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(MakeOrder(id: orderId, userId: userId));
        var invoice = MakeInvoice(orderId: orderId, isDeleted: true);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoice.Id, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted invoice must not be accessible by the owner");
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Detail_When_Owner_And_Invoice_Linked_Via_Order()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var order = MakeOrder(id: orderId, userId: userId);
        order.OrderNumber = "ORD-2026-DETAIL";
        db.Set<Order>().Add(order);

        var invoice = MakeInvoice(
            id: invoiceId,
            orderId: orderId,
            status: InvoiceStatus.Open,
            totalGrossMinor: 9500);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoiceId, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invoiceId);
        result.Status.Should().Be(InvoiceStatus.Open);
        result.TotalGrossMinor.Should().Be(9500);
        result.OrderNumber.Should().Be("ORD-2026-DETAIL");
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Return_Detail_When_Owner_And_Invoice_Linked_Via_Customer()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(id: invoiceId, customerId: customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoiceId, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull("owner can access invoice linked via their customer record");
        result!.Id.Should().Be(invoiceId);
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Compute_Balance_For_Unpaid_Invoice_Without_Payment()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(id: invoiceId, customerId: customerId, status: InvoiceStatus.Draft, totalGrossMinor: 7500));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoiceId, ct: TestContext.Current.CancellationToken);

        result!.BalanceMinor.Should().Be(7500, "Draft invoice with no payment has full balance outstanding");
        result.SettledAmountMinor.Should().Be(0);
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Compute_Zero_Balance_For_Paid_Invoice_Without_Payment_Record()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Invoice>().Add(MakeInvoice(id: invoiceId, customerId: customerId, status: InvoiceStatus.Paid, totalGrossMinor: 7500));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoiceId, ct: TestContext.Current.CancellationToken);

        result!.SettledAmountMinor.Should().Be(7500, "Paid invoice without payment defaults to fully settled");
        result.BalanceMinor.Should().Be(0);
    }

    [Fact]
    public async Task GetMyInvoiceDetail_Should_Enrich_Payment_Summary_When_Payment_Exists()
    {
        await using var db = MemberInvoiceDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(id: customerId, userId: userId));
        db.Set<Payment>().Add(MakePayment(id: paymentId, amountMinor: 9500));
        db.Set<Invoice>().Add(MakeInvoice(id: invoiceId, customerId: customerId, paymentId: paymentId, totalGrossMinor: 9500));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyInvoiceDetailHandler(db, new FakeCurrentUser(userId));
        var result = await handler.HandleAsync(invoiceId, ct: TestContext.Current.CancellationToken);

        result!.PaymentSummary.Should().NotBeNullOrEmpty("a linked payment should produce a non-empty summary");
        result.SettledAmountMinor.Should().Be(9500, "fully captured payment settles the full invoice amount");
        result.BalanceMinor.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class MemberInvoiceDbContext : DbContext, IAppDbContext
    {
        private MemberInvoiceDbContext(DbContextOptions<MemberInvoiceDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MemberInvoiceDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MemberInvoiceDbContext>()
                .UseInMemoryDatabase($"darwin_member_invoice_tests_{Guid.NewGuid()}")
                .Options;
            return new MemberInvoiceDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Invoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
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

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
            });

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
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
