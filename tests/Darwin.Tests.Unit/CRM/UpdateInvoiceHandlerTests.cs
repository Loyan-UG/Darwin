using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
        SeedReadyInvoiceSettings(db);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator(), new TestStringLocalizer());

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
    public async Task HandleAsync_Should_RejectIssueFromEdit_WhenBusinessVatIdIsMissing()
    {
        await using var db = InvoiceTestDbContext.Create();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 3, 1, 4, 1 };

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.test",
            Phone = "+491111111111",
            TaxProfileType = CustomerTaxProfileType.Business
        });

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Draft,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });

        SeedReadyInvoiceSettings(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InvoiceEditDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            CustomerId = customerId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 1000,
            TotalTaxMinor = 190,
            TotalGrossMinor = 1190,
            DueDateUtc = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("BusinessCustomerVatIdRequiredBeforeIssuingInvoice");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectFinancialFieldChanges_WhenInvoiceIsIssued()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 8, 8, 8, 8 };

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 1000,
            TotalTaxMinor = 190,
            TotalGrossMinor = 1190,
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = "{\"schemaVersion\":1}",
            RowVersion = rowVersion.ToArray()
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InvoiceEditDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 1200,
            TotalTaxMinor = 228,
            TotalGrossMinor = 1428,
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IssuedInvoiceFinancialFieldsCannotBeEdited");
    }

    [Fact]
    public async Task GetInvoiceArchiveSnapshotHandler_Should_ReturnSnapshotOnlyForIssuedInvoices()
    {
        await using var db = InvoiceTestDbContext.Create();
        var issuedInvoiceId = Guid.NewGuid();
        var draftInvoiceId = Guid.NewGuid();
        const string snapshotJson = "{\"schemaVersion\":1,\"invoiceId\":\"test\"}";

        db.Set<Invoice>().AddRange(
            new Invoice
            {
                Id = issuedInvoiceId,
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
                IssuedAtUtc = new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
                IssuedSnapshotJson = snapshotJson,
                RowVersion = new byte[] { 1, 1, 1, 1 }
            },
            new Invoice
            {
                Id = draftInvoiceId,
                Status = InvoiceStatus.Draft,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[] { 2, 2, 2, 2 }
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveSnapshotHandler(db);

        var issuedSnapshot = await handler.HandleAsync(issuedInvoiceId, TestContext.Current.CancellationToken);
        var draftSnapshot = await handler.HandleAsync(draftInvoiceId, TestContext.Current.CancellationToken);

        issuedSnapshot.Should().NotBeNull();
        issuedSnapshot!.InvoiceId.Should().Be(issuedInvoiceId);
        issuedSnapshot.SnapshotJson.Should().Be(snapshotJson);
        issuedSnapshot.FileName.Should().Be($"invoice-{issuedInvoiceId:N}-issued-snapshot.json");
        draftSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_CreateArchiveRetentionMetadata_WhenInvoiceIsIssued()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 6, 1, 6, 1 };
        var issuedAtUtc = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc);

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Draft,
            Currency = "EUR",
            TotalNetMinor = 1000,
            TotalTaxMinor = 190,
            TotalGrossMinor = 1190,
            DueDateUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });
        SeedReadyInvoiceSettings(db, invoiceArchiveRetentionYears: 12);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(
            db,
            new InvoiceStatusTransitionValidator(),
            new TestStringLocalizer(),
            new FixedClock(issuedAtUtc));

        await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            TargetStatus = InvoiceStatus.Open
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        invoice.IssuedSnapshotJson.Should().NotBeNullOrWhiteSpace();
        invoice.IssuedSnapshotHashSha256.Should().HaveLength(64);
        invoice.ArchiveGeneratedAtUtc.Should().Be(issuedAtUtc);
        invoice.ArchiveRetainUntilUtc.Should().Be(issuedAtUtc.AddYears(12));
        invoice.ArchiveRetentionPolicyVersion.Should().Be("invoice-archive-retention:v1:12y");
    }


    [Fact]
    public async Task GetInvoiceArchiveDocumentHandler_Should_RenderPrintableHtml_FromIssuedSnapshot()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var snapshotJson = $$"""
            {
              "schemaVersion": 1,
              "invoiceId": "{{invoiceId}}",
              "status": 1,
              "currency": "EUR",
              "totalNetMinor": 1000,
              "totalTaxMinor": 190,
              "totalGrossMinor": 1190,
              "dueDateUtc": "2026-03-26T10:00:00Z",
              "issuedAtUtc": "2026-03-25T10:00:00Z",
              "issuer": { "legalName": "Darwin GmbH", "taxId": "DE123456789", "addressLine1": "Main Street 1", "postalCode": "10115", "city": "Berlin", "country": "DE" },
              "customer": { "firstName": "Ada", "lastName": "Lovelace", "email": "ada@example.test", "taxProfileType": 0 },
              "lines": [
                { "description": "Consulting", "quantity": 1, "unitPriceNetMinor": 1000, "taxRate": 0.19, "totalNetMinor": 1000, "totalGrossMinor": 1190 }
              ]
            }
            """;

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = snapshotJson,
            RowVersion = new byte[] { 3, 3, 3, 3 }
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInvoiceArchiveDocumentHandler(new GetInvoiceArchiveSnapshotHandler(db));

        var document = await handler.HandleAsync(invoiceId, TestContext.Current.CancellationToken);

        document.Should().NotBeNull();
        document!.FileName.Should().Be($"invoice-{invoiceId:N}-archive.html");
        document.Html.Should().Contain("Invoice archive document");
        document.Html.Should().Contain("Darwin GmbH");
        document.Html.Should().Contain("Ada Lovelace");
        document.Html.Should().Contain("Consulting");
        document.Html.Should().Contain("11.90 EUR");
    }

    [Fact]
    public async Task PurgeExpiredInvoiceArchivesHandler_Should_ClearExpiredArchivePayload_AndWriteAuditEvent()
    {
        await using var db = InvoiceTestDbContext.Create();
        var expiredInvoiceId = Guid.NewGuid();
        var retainedInvoiceId = Guid.NewGuid();
        var nowUtc = new DateTime(2038, 5, 9, 12, 0, 0, DateTimeKind.Utc);

        db.Set<Invoice>().AddRange(
            new Invoice
            {
                Id = expiredInvoiceId,
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
                IssuedAtUtc = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc),
                IssuedSnapshotJson = """{"invoiceId":"expired"}""",
                IssuedSnapshotHashSha256 = new string('a', 64),
                ArchiveGeneratedAtUtc = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc),
                ArchiveRetainUntilUtc = new DateTime(2038, 5, 9, 11, 59, 0, DateTimeKind.Utc),
                ArchiveRetentionPolicyVersion = "invoice-archive-retention:v1:12y",
                RowVersion = new byte[] { 8, 8, 8, 8 }
            },
            new Invoice
            {
                Id = retainedInvoiceId,
                Status = InvoiceStatus.Open,
                Currency = "EUR",
                DueDateUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
                IssuedAtUtc = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc),
                IssuedSnapshotJson = """{"invoiceId":"retained"}""",
                IssuedSnapshotHashSha256 = new string('b', 64),
                ArchiveGeneratedAtUtc = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc),
                ArchiveRetainUntilUtc = new DateTime(2038, 5, 10, 12, 0, 0, DateTimeKind.Utc),
                ArchiveRetentionPolicyVersion = "invoice-archive-retention:v1:12y",
                RowVersion = new byte[] { 9, 9, 9, 9 }
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, new FixedClock(nowUtc));

        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(1);
        result.PurgedInvoiceIds.Should().ContainSingle().Which.Should().Be(expiredInvoiceId);

        var expiredInvoice = await db.Set<Invoice>().SingleAsync(x => x.Id == expiredInvoiceId, TestContext.Current.CancellationToken);
        expiredInvoice.IssuedSnapshotJson.Should().BeNull();
        expiredInvoice.IssuedSnapshotHashSha256.Should().BeNull();
        expiredInvoice.ArchivePurgedAtUtc.Should().Be(nowUtc);
        expiredInvoice.ArchivePurgeReason.Should().Be("Retention period elapsed");

        var retainedInvoice = await db.Set<Invoice>().SingleAsync(x => x.Id == retainedInvoiceId, TestContext.Current.CancellationToken);
        retainedInvoice.IssuedSnapshotJson.Should().NotBeNullOrWhiteSpace();
        retainedInvoice.ArchivePurgedAtUtc.Should().BeNull();

        var audit = await db.Set<EventLog>().SingleAsync(TestContext.Current.CancellationToken);
        audit.Type.Should().Be("InvoiceArchivePurged");
        audit.IdempotencyKey.Should().Be($"InvoiceArchivePurged:{expiredInvoiceId:N}");
        audit.PropertiesJson.Should().Contain(expiredInvoiceId.ToString());
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

        var handler = new UpdateInvoiceHandler(db, new InvoiceEditValidator(), new TestStringLocalizer());

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
            .WithMessage("LinkedPaymentAlreadyAssignedToAnotherInvoice");
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

        var handler = new CreateInvoiceRefundHandler(db, new InvoiceRefundCreateValidator(), new TestStringLocalizer());
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

        var handler = new CreateInvoiceRefundHandler(db, new InvoiceRefundCreateValidator(), new TestStringLocalizer());
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

    [Fact]
    public async Task UpdateInvoiceReverseChargeDecisionHandler_Should_PersistOperatorDecision()
    {
        await using var db = InvoiceTestDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 5, 5, 5 };
        var reviewedAtUtc = new DateTime(2026, 5, 9, 10, 15, 0, DateTimeKind.Utc);

        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalGrossMinor = 1190,
            DueDateUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceReverseChargeDecisionHandler(
            db,
            new FixedClock(reviewedAtUtc),
            new TestStringLocalizer());

        await handler.HandleAsync(new InvoiceReverseChargeDecisionDto
        {
            Id = invoiceId,
            RowVersion = rowVersion,
            Applies = true,
            Note = "  EU B2B VAT reverse charge confirmed.  "
        }, TestContext.Current.CancellationToken);

        var invoice = await db.Set<Invoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        invoice.ReverseChargeApplied.Should().BeTrue();
        invoice.ReverseChargeReviewedAtUtc.Should().Be(reviewedAtUtc);
        invoice.ReverseChargeReviewNote.Should().Be("EU B2B VAT reverse charge confirmed.");
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

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
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
        }
    }

    private static void SeedReadyInvoiceSettings(IAppDbContext db, int invoiceArchiveRetentionYears = 10)
    {
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            VatEnabled = true,
            InvoiceIssuerLegalName = "Darwin GmbH",
            InvoiceIssuerTaxId = "DE123456789",
            InvoiceIssuerAddressLine1 = "Main Street 1",
            InvoiceIssuerPostalCode = "10115",
            InvoiceIssuerCity = "Berlin",
            InvoiceIssuerCountry = "DE",
            InvoiceArchiveRetentionYears = invoiceArchiveRetentionYears
        });
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
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
