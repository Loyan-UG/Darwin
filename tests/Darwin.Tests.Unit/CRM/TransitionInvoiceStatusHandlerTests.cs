using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Verifies explicit invoice status transitions so operator workflows do not create
/// invalid invoice and payment state combinations.
/// </summary>
public sealed class TransitionInvoiceStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CapturePendingPayment_WhenMarkingInvoicePaid()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            CustomerId = customerId,
            PaymentId = paymentId,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 1200,
            Status = PaymentStatus.Pending
        });
        SeedReadyInvoiceSettings(db);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Paid
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.PaidAtUtc.Should().Be(invoice.PaidAtUtc);
        payment.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectOpeningDraftInvoice_WhenIssuerDataIsMissing()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 3, 5, 7 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Draft,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Open
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("InvoiceIssuerDataRequiredBeforeIssuing");
    }

    [Fact]
    public async Task HandleAsync_Should_CaptureImmutableIssuedSnapshot_WhenOpeningDraftInvoice()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var rowVersion = new byte[] { 6, 6, 6, 6 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Mina",
            LastName = "Becker",
            Email = "mina@example.test",
            Phone = "+491111111111",
            TaxProfileType = CustomerTaxProfileType.Consumer
        });

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            CustomerId = customerId,
            Status = InvoiceStatus.Draft,
            Currency = "EUR",
            TotalNetMinor = 1000,
            TotalTaxMinor = 190,
            TotalGrossMinor = 1190,
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<InvoiceLine>().Add(new InvoiceLine
        {
            InvoiceId = invoiceId,
            Description = "Consulting",
            Quantity = 1,
            UnitPriceNetMinor = 1000,
            TaxRate = 0.19m,
            TotalNetMinor = 1000,
            TotalGrossMinor = 1190
        });

        SeedReadyInvoiceSettings(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Open
        }, TestContext.Current.CancellationToken);

        var openedInvoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var firstSnapshotJson = openedInvoice.IssuedSnapshotJson;
        var firstIssuedAtUtc = openedInvoice.IssuedAtUtc;

        openedInvoice.Status.Should().Be(InvoiceStatus.Open);
        firstIssuedAtUtc.Should().NotBeNull();
        firstSnapshotJson.Should().Contain("\"schemaVersion\":1");
        firstSnapshotJson.Should().Contain("\"status\":1");
        firstSnapshotJson.Should().Contain("Darwin GmbH");
        firstSnapshotJson.Should().Contain("Consulting");

        await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Paid,
            PaidAtUtc = new DateTime(2026, 3, 27, 10, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        var paidInvoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);

        paidInvoice.Status.Should().Be(InvoiceStatus.Paid);
        paidInvoice.IssuedAtUtc.Should().Be(firstIssuedAtUtc);
        paidInvoice.IssuedSnapshotJson.Should().Be(firstSnapshotJson);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectOpeningBusinessInvoice_WhenVatIdIsMissing()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 2, 4, 6, 8 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Phone = "+491111111111",
            TaxProfileType = CustomerTaxProfileType.Business
        });

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            CustomerId = customerId,
            Status = InvoiceStatus.Draft,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        SeedReadyInvoiceSettings(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Open
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("BusinessCustomerVatIdRequiredBeforeIssuingInvoice");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectCancellation_WhenPaymentAlreadyCaptured()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 9, 8, 7, 6 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            PaymentId = paymentId,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 1800,
            Status = PaymentStatus.Captured
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Cancelled
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("PaidInvoicesMustBeRefundedBeforeCancellation");
    }

    [Fact]
    public async Task HandleAsync_Should_VoidPendingPayment_WhenCancellingInvoice()
    {
        await using var db = InvoiceTransitionTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 4, 3, 2 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            PaymentId = paymentId,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "PayPal",
            Currency = "EUR",
            AmountMinor = 900,
            Status = PaymentStatus.Authorized,
            PaidAtUtc = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc)
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(db, new InvoiceStatusTransitionValidator(), new TestStringLocalizer());

        await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Cancelled
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.PaidAtUtc.Should().BeNull();
        payment.Status.Should().Be(PaymentStatus.Voided);
        payment.PaidAtUtc.Should().BeNull();
    }

    private sealed class InvoiceTransitionTestDbContext : DbContext, IAppDbContext
    {
        private InvoiceTransitionTestDbContext(DbContextOptions<InvoiceTransitionTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InvoiceTransitionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InvoiceTransitionTestDbContext>()
                .UseInMemoryDatabase($"darwin_invoice_transition_tests_{Guid.NewGuid()}")
                .Options;
            return new InvoiceTransitionTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FirstName).IsRequired(false);
                builder.Property(x => x.LastName).IsRequired(false);
                builder.Property(x => x.Email).IsRequired(false);
                builder.Property(x => x.Phone).IsRequired(false);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Invoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<InvoiceLine>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Description).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private static void SeedReadyInvoiceSettings(IAppDbContext db)
    {
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            VatEnabled = true,
            InvoiceIssuerLegalName = "Darwin GmbH",
            InvoiceIssuerTaxId = "DE123456789",
            InvoiceIssuerAddressLine1 = "Main Street 1",
            InvoiceIssuerPostalCode = "10115",
            InvoiceIssuerCity = "Berlin",
            InvoiceIssuerCountry = "DE"
        });
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
