using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Covers <see cref="CreateInvoiceRefundHandler"/> and <see cref="PurgeExpiredInvoiceArchivesHandler"/>
/// invoice command handler behavior.
/// </summary>
public sealed class CreateInvoiceRefundHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Invoice MakeInvoice(
        Guid? id = null,
        Guid? paymentId = null,
        string currency = "EUR",
        long totalGrossMinor = 10000,
        InvoiceStatus status = InvoiceStatus.Open,
        byte[]? rowVersion = null,
        string? issuedSnapshotJson = null,
        DateTime? archiveRetainUntilUtc = null,
        DateTime? archivePurgedAtUtc = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Currency = currency,
            TotalGrossMinor = totalGrossMinor,
            PaymentId = paymentId,
            Status = status,
            DueDateUtc = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = rowVersion ?? new byte[] { 1, 2, 3, 4 },
            IssuedSnapshotJson = issuedSnapshotJson,
            ArchiveRetainUntilUtc = archiveRetainUntilUtc,
            ArchivePurgedAtUtc = archivePurgedAtUtc
        };

    private static Payment MakePayment(
        Guid? id = null,
        string currency = "EUR",
        long amountMinor = 10000,
        PaymentStatus status = PaymentStatus.Captured)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Provider = "Stripe",
            Currency = currency,
            AmountMinor = amountMinor,
            Status = status,
            RowVersion = new byte[] { 1 }
        };

    private static CreateInvoiceRefundHandler MakeHandler(
        IAppDbContext db,
        IClock? clock = null)
        => new(db, new InvoiceRefundCreateValidator(), new TestStringLocalizer(), clock);

    private static IClock MakeClock(DateTime utcNow) => new FixedClock(utcNow);

    // ─────────────────────────────────────────────────────────────────────────
    // CreateInvoiceRefundHandler — validator guards
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_InvoiceId_Is_Empty()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.Empty,
            RowVersion = new byte[] { 1 },
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Test"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty InvoiceId fails the validator");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_RowVersion_Is_Empty()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Test"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty RowVersion fails the validator");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_AmountMinor_Is_Zero()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            AmountMinor = 0,
            Currency = "EUR",
            Reason = "Test"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("AmountMinor <= 0 fails the validator");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_Currency_Is_Empty()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            AmountMinor = 500,
            Currency = "",
            Reason = "Test"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Currency fails the validator");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_Reason_Is_Empty()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            AmountMinor = 500,
            Currency = "EUR",
            Reason = ""
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Reason fails the validator");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateInvoiceRefundHandler — business logic guards
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRefund_Should_Throw_InvalidOperationException_When_Invoice_Not_Found()
    {
        await using var db = RefundTestDbContext.Create();
        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Missing invoice"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent invoice must be rejected");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ConcurrencyException_When_RowVersion_Stale()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var invoice = MakeInvoice(paymentId: paymentId, rowVersion: new byte[] { 1, 2, 3, 4 });
        db.Set<Payment>().Add(MakePayment(id: paymentId));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = new byte[] { 9, 9, 9, 9 }, // stale
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Stale"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must trigger concurrency guard");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_InvalidOperationException_When_Invoice_Has_No_Payment()
    {
        await using var db = RefundTestDbContext.Create();
        var rowVersion = new byte[] { 5, 6, 7, 8 };
        var invoice = MakeInvoice(paymentId: null, rowVersion: rowVersion);
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "No payment"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "an invoice without a linked payment cannot be refunded");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_Payment_Status_Is_Pending()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, rowVersion: rowVersion);
        db.Set<Payment>().Add(MakePayment(id: paymentId, status: PaymentStatus.Pending));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Pending payment"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "only Captured or Completed payments can be refunded");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_Currency_Mismatch()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, currency: "EUR", rowVersion: rowVersion);
        db.Set<Payment>().Add(MakePayment(id: paymentId, currency: "EUR"));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 500,
            Currency = "USD", // wrong currency
            Reason = "Currency mismatch"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "refund currency must match invoice and payment currency");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_No_Refundable_Amount_Remains()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, currency: "EUR",
            totalGrossMinor: 5000, status: InvoiceStatus.Cancelled, rowVersion: rowVersion);

        db.Set<Payment>().Add(MakePayment(id: paymentId, currency: "EUR", amountMinor: 5000));
        db.Set<Invoice>().Add(invoice);
        // Full refund already done
        db.Set<Refund>().Add(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            AmountMinor = 5000,
            Currency = "EUR",
            Reason = "Full refund",
            Status = RefundStatus.Completed,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 100,
            Currency = "EUR",
            Reason = "Extra refund"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "no refundable amount remains after a cancelled invoice");
    }

    [Fact]
    public async Task CreateRefund_Should_Throw_ValidationException_When_Amount_Exceeds_Refundable()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, currency: "EUR",
            totalGrossMinor: 5000, rowVersion: rowVersion);

        db.Set<Payment>().Add(MakePayment(id: paymentId, currency: "EUR", amountMinor: 5000));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var act = () => handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 9999, // exceeds payment amount of 5000
            Currency = "EUR",
            Reason = "Over limit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "refund amount exceeding the remaining refundable amount must be rejected");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateInvoiceRefundHandler — successful paths
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRefund_Should_Create_Partial_Refund_And_Return_Refund_Id()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, currency: "EUR",
            totalGrossMinor: 10000, rowVersion: rowVersion);

        db.Set<Payment>().Add(MakePayment(id: paymentId, currency: "EUR", amountMinor: 10000));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = MakeClock(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        var handler = MakeHandler(db, clock);

        var refundId = await handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 3000,
            Currency = "EUR",
            Reason = " Partial return "
        }, TestContext.Current.CancellationToken);

        refundId.Should().NotBeEmpty();

        var refund = db.Set<Refund>().Single(x => x.Id == refundId);
        refund.AmountMinor.Should().Be(3000);
        refund.Currency.Should().Be("EUR");
        refund.Reason.Should().Be("Partial return", "reason must be trimmed");
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.CompletedAtUtc.Should().Be(clock.UtcNow);

        // Invoice and payment must NOT change status for a partial refund
        var savedInvoice = db.Set<Invoice>().Single(x => x.Id == invoice.Id);
        savedInvoice.Status.Should().NotBe(InvoiceStatus.Cancelled,
            "a partial refund must not cancel the invoice");

        var savedPayment = db.Set<Payment>().Single(x => x.Id == paymentId);
        savedPayment.Status.Should().NotBe(PaymentStatus.Refunded,
            "a partial refund must not mark the payment as Refunded");
    }

    [Fact]
    public async Task CreateRefund_Should_Cancel_Invoice_And_Mark_Payment_Refunded_On_Full_Refund()
    {
        await using var db = RefundTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var invoice = MakeInvoice(paymentId: paymentId, currency: "EUR",
            totalGrossMinor: 5000, status: InvoiceStatus.Paid, rowVersion: rowVersion);

        db.Set<Payment>().Add(MakePayment(id: paymentId, currency: "EUR", amountMinor: 5000,
            status: PaymentStatus.Captured));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = MakeHandler(db);

        var refundId = await handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = invoice.Id,
            RowVersion = rowVersion,
            AmountMinor = 5000,
            Currency = "EUR",
            Reason = "Full refund requested"
        }, TestContext.Current.CancellationToken);

        refundId.Should().NotBeEmpty();

        var savedInvoice = db.Set<Invoice>().Single(x => x.Id == invoice.Id);
        savedInvoice.Status.Should().Be(InvoiceStatus.Cancelled,
            "fully refunded invoice must be cancelled");
        savedInvoice.PaidAtUtc.Should().BeNull("paid timestamp must be cleared on full refund");

        var savedPayment = db.Set<Payment>().Single(x => x.Id == paymentId);
        savedPayment.Status.Should().Be(PaymentStatus.Refunded,
            "fully refunded payment must transition to Refunded status");
        savedPayment.PaidAtUtc.Should().BeNull("payment paid timestamp must be cleared");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PurgeExpiredInvoiceArchivesHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeExpiredArchives_Should_Return_Zero_When_No_Invoices_Exist()
    {
        await using var db = RefundTestDbContext.Create();
        var clock = MakeClock(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);

        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.EvaluatedCount.Should().Be(0);
        result.PurgedCount.Should().Be(0);
        result.PurgedInvoiceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Purge_Invoice_Whose_Retention_Has_Elapsed()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        var invoiceId = Guid.NewGuid();
        var invoice = MakeInvoice(
            id: invoiceId,
            issuedSnapshotJson: "{\"number\":\"INV-001\"}",
            archiveRetainUntilUtc: now.AddDays(-1)); // expired yesterday
        db.Set<Invoice>().Add(invoice);
        db.Set<EventLog>(); // ensure table is known
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(1);
        result.PurgedInvoiceIds.Should().Contain(invoiceId);

        var savedInvoice = db.Set<Invoice>().Single(x => x.Id == invoiceId);
        savedInvoice.IssuedSnapshotJson.Should().BeNull("snapshot must be cleared after purge");
        savedInvoice.ArchivePurgedAtUtc.Should().Be(now);
        savedInvoice.ArchivePurgeReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Skip_Already_Purged_Invoice()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        var invoice = MakeInvoice(
            issuedSnapshotJson: "{\"number\":\"INV-002\"}",
            archiveRetainUntilUtc: now.AddDays(-1),
            archivePurgedAtUtc: now.AddDays(-2)); // already purged
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(0, "already-purged invoice must be skipped");
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Skip_Invoice_With_Future_Retention()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        var invoice = MakeInvoice(
            issuedSnapshotJson: "{\"number\":\"INV-003\"}",
            archiveRetainUntilUtc: now.AddDays(30)); // future retention
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(0, "invoice with future retention must not be purged");
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Skip_Invoice_With_No_Snapshot()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        var invoice = MakeInvoice(
            issuedSnapshotJson: null, // no snapshot
            archiveRetainUntilUtc: now.AddDays(-1));
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(0, "invoice without snapshot must not be processed");
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Respect_BatchSize_Clamp()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        // Create 5 expired invoices
        for (var i = 0; i < 5; i++)
        {
            db.Set<Invoice>().Add(MakeInvoice(
                issuedSnapshotJson: $"{{\"n\":{i}}}",
                archiveRetainUntilUtc: now.AddDays(-1)));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        // batchSize=2, so only 2 should be processed in this run
        var result = await handler.HandleAsync(batchSize: 2, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(2, "batch size of 2 should limit the purge to 2 invoices");
        result.PurgedInvoiceIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Clamp_BatchSize_To_MaxBatchSize()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        // Only 2 expired invoices in store
        for (var i = 0; i < 2; i++)
        {
            db.Set<Invoice>().Add(MakeInvoice(
                issuedSnapshotJson: $"{{\"n\":{i}}}",
                archiveRetainUntilUtc: now.AddDays(-1)));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        // batchSize=9999 is above max (250) but only 2 rows match
        var result = await handler.HandleAsync(batchSize: 9999, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(2, "all 2 matching invoices should be purged even when batchSize exceeds max");
    }

    [Fact]
    public async Task PurgeExpiredArchives_Should_Write_EventLog_Per_Purged_Invoice()
    {
        await using var db = RefundTestDbContext.Create();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = MakeClock(now);

        var invoice1 = MakeInvoice(issuedSnapshotJson: "{\"n\":1}", archiveRetainUntilUtc: now.AddDays(-1));
        var invoice2 = MakeInvoice(issuedSnapshotJson: "{\"n\":2}", archiveRetainUntilUtc: now.AddDays(-5));
        db.Set<Invoice>().AddRange(invoice1, invoice2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeExpiredInvoiceArchivesHandler(db, clock);
        var result = await handler.HandleAsync(100, TestContext.Current.CancellationToken);

        result.PurgedCount.Should().Be(2);

        var eventLogs = db.Set<EventLog>().ToList();
        eventLogs.Should().HaveCount(2, "one EventLog must be written per purged invoice");
        eventLogs.Should().AllSatisfy(log =>
        {
            log.Type.Should().Be("InvoiceArchivePurged");
            log.OccurredAtUtc.Should().Be(now);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class RefundTestDbContext : DbContext, IAppDbContext
    {
        private RefundTestDbContext(DbContextOptions<RefundTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RefundTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RefundTestDbContext>()
                .UseInMemoryDatabase($"darwin_refund_tests_{Guid.NewGuid()}")
                .Options;
            return new RefundTestDbContext(options);
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
    }
}
