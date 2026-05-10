using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Application.CRM.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Covers <see cref="GetInvoicesPageHandler"/>, <see cref="GetInvoiceForEditHandler"/>,
/// and <see cref="GetInvoiceArchiveSnapshotHandler"/> query handler behavior.
/// </summary>
public sealed class CrmInvoiceQueryHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IClock MakeClock(DateTime utcNow) =>
        new FixedClock(utcNow);

    private static Invoice MakeInvoice(
        Guid? id = null,
        string currency = "EUR",
        InvoiceStatus status = InvoiceStatus.Draft,
        bool isDeleted = false,
        Guid? customerId = null,
        Guid? orderId = null,
        Guid? paymentId = null,
        DateTime? dueDateUtc = null,
        long totalGrossMinor = 10000,
        string? issuedSnapshotJson = null,
        DateTime? archiveRetainUntilUtc = null,
        DateTime? archivePurgedAtUtc = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Currency = currency,
            Status = status,
            IsDeleted = isDeleted,
            CustomerId = customerId,
            OrderId = orderId,
            PaymentId = paymentId,
            DueDateUtc = dueDateUtc ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            TotalGrossMinor = totalGrossMinor,
            IssuedSnapshotJson = issuedSnapshotJson,
            ArchiveRetainUntilUtc = archiveRetainUntilUtc,
            ArchivePurgedAtUtc = archivePurgedAtUtc,
            RowVersion = new byte[] { 1, 2, 3 }
        };

    private static Payment MakePayment(
        Guid? id = null,
        string currency = "EUR",
        long amountMinor = 10000,
        PaymentStatus status = PaymentStatus.Captured,
        Guid? invoiceId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Provider = "Stripe",
            Currency = currency,
            AmountMinor = amountMinor,
            Status = status,
            InvoiceId = invoiceId,
            RowVersion = new byte[] { 1 }
        };

    private static Customer MakeCustomer(Guid? id = null, string firstName = "Test", string lastName = "User")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = "test@example.com",
            Phone = "+49001234567"
        };

    // ─────────────────────────────────────────────────────────────────────────
    // GetInvoicesPageHandler — pagination and soft-delete exclusion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesPage_Should_Return_Empty_When_No_Invoices_Exist()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new GetInvoicesPageHandler(db, clock);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Exclude_Soft_Deleted_Invoices()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Set<Invoice>().AddRange(
            MakeInvoice(isDeleted: false),
            MakeInvoice(isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "soft-deleted invoices must be excluded");
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Clamp_Page_Param_Below_One()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Set<Invoice>().Add(MakeInvoice());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "page < 1 should be clamped to 1");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Clamp_PageSize_Above_Max()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        for (var i = 0; i < 5; i++)
            db.Set<Invoice>().Add(MakeInvoice());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        // pageSize=9999 must be clamped to 200
        var (_, total) = await handler.HandleAsync(1, 9999, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Filter_Draft_Invoices()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Set<Invoice>().AddRange(
            MakeInvoice(status: InvoiceStatus.Draft),
            MakeInvoice(status: InvoiceStatus.Open));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, filter: InvoiceQueueFilter.Draft, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only Draft invoices must be returned for the Draft filter");
        items.Single().Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Filter_DueSoon_Invoices()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        await using var db = InvoiceQueryDbContext.Create();
        // Due in 3 days → within the 7-day window
        db.Set<Invoice>().AddRange(
            MakeInvoice(status: InvoiceStatus.Open, dueDateUtc: now.AddDays(3)),
            // Due yesterday → overdue, not DueSoon
            MakeInvoice(status: InvoiceStatus.Open, dueDateUtc: now.AddDays(-1)),
            // Paid → should be excluded even if within window
            MakeInvoice(status: InvoiceStatus.Paid, dueDateUtc: now.AddDays(3)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, filter: InvoiceQueueFilter.DueSoon, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only unpaid invoices due within 7 days should appear");
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Filter_Overdue_Invoices()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        await using var db = InvoiceQueryDbContext.Create();
        // Due yesterday → overdue
        db.Set<Invoice>().AddRange(
            MakeInvoice(status: InvoiceStatus.Open, dueDateUtc: now.AddDays(-1)),
            // Due in future → not overdue
            MakeInvoice(status: InvoiceStatus.Open, dueDateUtc: now.AddDays(5)),
            // Paid + overdue → excluded
            MakeInvoice(status: InvoiceStatus.Paid, dueDateUtc: now.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, filter: InvoiceQueueFilter.Overdue, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only unpaid past-due invoices should appear in Overdue filter");
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Filter_Refunded_Invoices()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var paymentWithRefund = Guid.NewGuid();
        var paymentNoRefund = Guid.NewGuid();

        db.Set<Payment>().AddRange(
            MakePayment(id: paymentWithRefund),
            MakePayment(id: paymentNoRefund));

        db.Set<Invoice>().AddRange(
            MakeInvoice(paymentId: paymentWithRefund),
            MakeInvoice(paymentId: paymentNoRefund),
            MakeInvoice(paymentId: null));

        db.Set<Refund>().Add(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentWithRefund,
            Currency = "EUR",
            AmountMinor = 1000,
            Reason = "Customer request",
            Status = RefundStatus.Completed,
            RowVersion = new byte[] { 1 }
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, filter: InvoiceQueueFilter.Refunded, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only invoices with a completed refund against their payment should appear");
    }

    [Fact]
    public async Task GetInvoicesPage_Should_Map_Fields_Correctly()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var invoiceId = Guid.NewGuid();
        var dueDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "USD",
            Status = InvoiceStatus.Open,
            TotalNetMinor = 8000,
            TotalTaxMinor = 1600,
            TotalGrossMinor = 9600,
            DueDateUtc = dueDate,
            RowVersion = new byte[] { 7 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoicesPageHandler(db, clock);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.Id.Should().Be(invoiceId);
        item.Currency.Should().Be("USD");
        item.Status.Should().Be(InvoiceStatus.Open);
        item.TotalNetMinor.Should().Be(8000);
        item.TotalGrossMinor.Should().Be(9600);
        item.DueDateUtc.Should().Be(dueDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetInvoiceForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var handler = new GetInvoiceForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull("no invoice exists with that id");
    }

    [Fact]
    public async Task GetInvoiceForEdit_Should_Return_Invoice_With_Basic_Fields()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "EUR",
            Status = InvoiceStatus.Open,
            TotalNetMinor = 5000,
            TotalTaxMinor = 1000,
            TotalGrossMinor = 6000,
            DueDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1, 2 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceForEditHandler(db);
        var dto = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(invoiceId);
        dto.Currency.Should().Be("EUR");
        dto.Status.Should().Be(InvoiceStatus.Open);
        dto.TotalNetMinor.Should().Be(5000);
        dto.TotalGrossMinor.Should().Be(6000);
    }

    [Fact]
    public async Task GetInvoiceForEdit_Should_Enrich_Payment_Summary_When_Payment_Exists()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.Set<Payment>().Add(MakePayment(id: paymentId, amountMinor: 6000, status: PaymentStatus.Captured));
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "EUR",
            TotalGrossMinor = 6000,
            PaymentId = paymentId,
            DueDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceForEditHandler(db);
        var dto = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.PaymentSummary.Should().Contain("Stripe");
        dto.PaymentSummary.Should().Contain("EUR");
    }

    [Fact]
    public async Task GetInvoiceForEdit_Should_Compute_Balance_When_No_Payment()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "EUR",
            Status = InvoiceStatus.Draft,
            TotalGrossMinor = 5000,
            DueDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceForEditHandler(db);
        var dto = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        dto!.BalanceMinor.Should().Be(5000, "entire gross amount is still outstanding for a Draft invoice with no payment");
        dto.SettledAmountMinor.Should().Be(0);
    }

    [Fact]
    public async Task GetInvoiceForEdit_Should_Compute_Zero_Balance_For_Paid_Invoice_Without_Payment()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "EUR",
            Status = InvoiceStatus.Paid,
            TotalGrossMinor = 5000,
            DueDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceForEditHandler(db);
        var dto = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        dto!.SettledAmountMinor.Should().Be(5000, "a Paid invoice without payment record is treated as fully settled");
        dto.BalanceMinor.Should().Be(0);
    }

    [Fact]
    public async Task GetInvoiceForEdit_Should_Enrich_Customer_Display_Name()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId, firstName: "Anna", lastName: "Muster"));
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Currency = "EUR",
            CustomerId = customerId,
            DueDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceForEditHandler(db);
        var dto = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        dto!.CustomerDisplayName.Should().Contain("Anna");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetInvoiceArchiveSnapshotHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Null_For_Empty_Guid()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var handler = new GetInvoiceArchiveSnapshotHandler(db);

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Should().BeNull("empty Guid must be treated as an invalid request");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Null_When_Invoice_Not_Found()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var handler = new GetInvoiceArchiveSnapshotHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull("non-existent invoice must return null");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Null_When_Invoice_Is_Soft_Deleted()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(
            id: invoiceId,
            isDeleted: true,
            issuedSnapshotJson: "{\"x\":1}",
            archiveRetainUntilUtc: new DateTime(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        invoice.IssuedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveSnapshotHandler(db);
        var result = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted invoice should not return a snapshot");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Null_When_Snapshot_Is_Empty()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(id: invoiceId);
        invoice.IssuedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        invoice.IssuedSnapshotJson = null; // no snapshot
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveSnapshotHandler(db);
        var result = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        result.Should().BeNull("invoice without snapshot JSON must return null");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Snapshot_When_Available()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(
            id: invoiceId,
            issuedSnapshotJson: "{\"invoiceNumber\":\"INV-001\"}",
            archiveRetainUntilUtc: new DateTime(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        invoice.IssuedAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveSnapshotHandler(db);
        var result = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.InvoiceId.Should().Be(invoiceId);
        result.SnapshotJson.Should().Contain("INV-001");
        result.FileName.Should().StartWith("invoice-");
        result.FileName.Should().EndWith(".json");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshot_Should_Return_Null_When_Archive_Is_Purged()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(
            id: invoiceId,
            issuedSnapshotJson: "{\"x\":1}",
            archivePurgedAtUtc: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        invoice.IssuedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveSnapshotHandler(db);
        var result = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        result.Should().BeNull("purged archive must not be returned");
    }

    [Fact]
    public async Task GetInvoiceStructuredDataExport_Should_Return_Null_When_Snapshot_Is_Not_Available()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoice = MakeInvoice(id: Guid.NewGuid());
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceStructuredDataExportHandler(new GetInvoiceArchiveSnapshotHandler(db));

        var result = await handler.HandleAsync(invoice.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceStructuredDataExport_Should_Map_Issued_Snapshot_To_Structured_Source_Model()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var issuedAtUtc = new DateTime(2026, 5, 10, 12, 30, 0, DateTimeKind.Utc);
        var dueAtUtc = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var snapshotJson = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                invoiceId,
                businessId,
                customerId,
                orderId = (Guid?)null,
                paymentId = (Guid?)null,
                status = "Open",
                currency = "EUR",
                totalNetMinor = 10000L,
                totalTaxMinor = 1900L,
                totalGrossMinor = 11900L,
                dueDateUtc = dueAtUtc,
                paidAtUtc = (DateTime?)null,
                issuedAtUtc,
                issuer = new
                {
                    legalName = "Darwin GmbH",
                    taxId = "DE-ISSUER",
                    addressLine1 = "Issuer Str. 1",
                    postalCode = "10115",
                    city = "Berlin",
                    country = "DE"
                },
                customer = new
                {
                    id = customerId,
                    firstName = "Ada",
                    lastName = "Buyer",
                    companyName = "Buyer GmbH",
                    email = "buyer@example.test",
                    phone = "+491234567",
                    taxProfileType = "Business",
                    vatId = "DE123456789"
                },
                business = new
                {
                    id = businessId,
                    name = "Merchant",
                    legalName = "Merchant GmbH",
                    taxId = "DE-MERCHANT",
                    defaultCurrency = "EUR",
                    defaultCulture = "de-DE"
                },
                lines = new[]
                {
                    new
                    {
                        id = lineId,
                        description = "Consulting",
                        quantity = 1,
                        unitPriceNetMinor = 10000L,
                        taxRate = 0.19m,
                        totalNetMinor = 10000L,
                        totalGrossMinor = 11900L
                    }
                }
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var invoice = MakeInvoice(
            id: invoiceId,
            status: InvoiceStatus.Open,
            issuedSnapshotJson: snapshotJson,
            archiveRetainUntilUtc: issuedAtUtc.AddYears(10));
        invoice.IssuedAtUtc = issuedAtUtc;
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceStructuredDataExportHandler(new GetInvoiceArchiveSnapshotHandler(db));

        var result = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.InvoiceId.Should().Be(invoiceId);
        result.FileName.Should().Be($"invoice-{invoiceId:N}-structured-invoice.json");

        using var document = JsonDocument.Parse(result.Json);
        var root = document.RootElement;
        root.GetProperty("schemaVersion").GetString().Should().Be("darwin.structured-invoice.v1");
        root.GetProperty("complianceStatus").GetString().Should().Be("NotZugferdFacturX");
        root.GetProperty("complianceNote").GetString().Should().Contain("not a ZUGFeRD/Factur-X");
        root.GetProperty("document").GetProperty("invoiceId").GetGuid().Should().Be(invoiceId);
        root.GetProperty("document").GetProperty("currency").GetString().Should().Be("EUR");
        root.GetProperty("seller").GetProperty("displayName").GetString().Should().Be("Darwin GmbH");
        root.GetProperty("seller").GetProperty("taxId").GetString().Should().Be("DE-ISSUER");
        root.GetProperty("buyer").GetProperty("displayName").GetString().Should().Be("Buyer GmbH");
        root.GetProperty("buyer").GetProperty("vatId").GetString().Should().Be("DE123456789");
        root.GetProperty("business").GetProperty("id").GetGuid().Should().Be(businessId);
        root.GetProperty("lines")[0].GetProperty("lineId").GetGuid().Should().Be(lineId);
        root.GetProperty("lines")[0].GetProperty("totalTaxMinor").GetInt64().Should().Be(1900L);
        root.GetProperty("taxSummary")[0].GetProperty("taxRate").GetDecimal().Should().Be(0.19m);
        root.GetProperty("taxSummary")[0].GetProperty("totalTaxMinor").GetInt64().Should().Be(1900L);
        root.GetProperty("totals").GetProperty("totalGrossMinor").GetInt64().Should().Be(11900L);
    }

    [Fact]
    public async Task DatabaseInvoiceArchiveStorage_Should_Save_Read_And_Check_Artifact()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var issuedAtUtc = new DateTime(2026, 5, 9, 9, 0, 0, DateTimeKind.Utc);
        var invoice = MakeInvoice(id: invoiceId);

        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "en-US",
            SupportedCulturesCsv = "en-US",
            DefaultCountry = "DE",
            DefaultCurrency = "EUR",
            InvoiceArchiveRetentionYears = 7,
            RowVersion = new byte[] { 1 }
        });
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var payload = "{\"invoiceNumber\":\"INV-ARCHIVE\"}";
        var storage = new DatabaseInvoiceArchiveStorage(db);
        var result = await storage.SaveAsync(
            invoice,
            new InvoiceArchiveStorageArtifact(
                invoiceId,
                issuedAtUtc,
                "application/json",
                $"invoice-{invoiceId:N}-issued-snapshot.json",
                payload),
            TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        result.HashSha256.Should().Be(ComputeSha256(payload));
        result.RetainUntilUtc.Should().Be(issuedAtUtc.AddYears(7));
        result.RetentionPolicyVersion.Should().Be("invoice-archive-retention:v1:7y");
        (await storage.ExistsAsync(invoiceId, TestContext.Current.CancellationToken)).Should().BeTrue();

        var artifact = await storage.ReadAsync(invoiceId, TestContext.Current.CancellationToken);
        artifact.Should().NotBeNull();
        artifact!.Payload.Should().Contain("INV-ARCHIVE");
        artifact.FileName.Should().Be($"invoice-{invoiceId:N}-issued-snapshot.json");
    }

    [Fact]
    public async Task DatabaseInvoiceArchiveStorage_Should_Purge_Payload_And_Preserve_Audit_Metadata()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(
            id: invoiceId,
            issuedSnapshotJson: "{\"invoiceNumber\":\"INV-PURGE\"}",
            archiveRetainUntilUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        invoice.IssuedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        invoice.IssuedSnapshotHashSha256 = new string('a', 64);
        invoice.ArchiveGeneratedAtUtc = invoice.IssuedAtUtc;
        invoice.ArchiveRetentionPolicyVersion = "invoice-archive-retention:v1:1y";
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var purgedAtUtc = new DateTime(2026, 5, 9, 10, 0, 0, DateTimeKind.Utc);
        var storage = new DatabaseInvoiceArchiveStorage(db);
        await storage.PurgePayloadAsync(invoice, "Retention elapsed", purgedAtUtc, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saved = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        saved.IssuedSnapshotJson.Should().BeNull();
        saved.IssuedSnapshotHashSha256.Should().BeNull();
        saved.ArchivePurgedAtUtc.Should().Be(purgedAtUtc);
        saved.ArchivePurgeReason.Should().Be("Retention elapsed");
        saved.ArchiveRetentionPolicyVersion.Should().Be("invoice-archive-retention:v1:1y");
        (await storage.ExistsAsync(invoiceId, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task DatabaseInvoiceArchiveStorage_Should_Reject_Mismatched_Invoice_Id()
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoice = MakeInvoice(id: Guid.NewGuid());
        var storage = new DatabaseInvoiceArchiveStorage(db);

        var action = () => storage.SaveAsync(
            invoice,
            new InvoiceArchiveStorageArtifact(
                Guid.NewGuid(),
                new DateTime(2026, 5, 9, 9, 0, 0, DateTimeKind.Utc),
                "application/json",
                "invoice.json",
                "{\"invoiceNumber\":\"INV-MISMATCH\"}"),
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Archive artifact invoice id does not match the target invoice.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DatabaseInvoiceArchiveStorage_Should_Reject_Empty_Payload(string payload)
    {
        await using var db = InvoiceQueryDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(id: invoiceId);
        var storage = new DatabaseInvoiceArchiveStorage(db);

        var action = () => storage.SaveAsync(
            invoice,
            new InvoiceArchiveStorageArtifact(
                invoiceId,
                new DateTime(2026, 5, 9, 9, 0, 0, DateTimeKind.Utc),
                "application/json",
                "invoice.json",
                payload),
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Archive artifact payload is required.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class InvoiceQueryDbContext : DbContext, IAppDbContext
    {
        private InvoiceQueryDbContext(DbContextOptions<InvoiceQueryDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InvoiceQueryDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InvoiceQueryDbContext>()
                .UseInMemoryDatabase($"darwin_invoice_query_tests_{Guid.NewGuid()}")
                .Options;
            return new InvoiceQueryDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.SupportedCulturesCsv).IsRequired();
                builder.Property(x => x.DefaultCountry).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class TestStringLocalizer : IStringLocalizer<Darwin.Application.ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }
}
