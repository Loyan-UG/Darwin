using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Sales.Commands;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Application.Sales.Services;
using Darwin.Application.Sales.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Sales;

public sealed class CreditNoteHandlersTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 12, 14, 15, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCreditNote_Should_Snapshot_Issued_Invoice_And_Prevent_OverCredit()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var invoice = SeedIssuedInvoice(db, businessId: null);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = CreateHandler(db);

        var id = await handler.HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Reason = CreditNoteReason.PostIssueCorrection,
            InternalNotes = "  Price correction  ",
            Lines =
            {
                new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 }
            }
        }, TestContext.Current.CancellationToken);

        var note = await db.Set<CreditNote>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        note.Status.Should().Be(CreditNoteStatus.Draft);
        note.CreditNoteNumber.Should().BeNull();
        note.Currency.Should().Be("EUR");
        note.OriginalInvoiceNumber.Should().Be("INV-1001");
        note.InternalNotes.Should().Be("Price correction");
        note.TotalNetMinor.Should().Be(1000);
        note.TotalTaxMinor.Should().Be(190);
        note.TotalGrossMinor.Should().Be(1190);
        note.Lines.Should().ContainSingle().Which.SourceLineJson.Should().Contain("invoiceLineId");
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.credit_note.created");

        await handler.Invoking(x => x.HandleAsync(new CreditNoteCreateDto
            {
                InvoiceId = invoice.Id,
                Lines =
                {
                    new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 3 }
                }
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateCreditNote_Should_Aggregate_Duplicate_Line_Input_And_Reject_Mismatched_Refund()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var invoice = SeedIssuedInvoice(db, businessId: null);
        var mismatchedRefundId = SeedCompletedRefund(db, orderId: Guid.NewGuid(), invoice.Currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = CreateHandler(db);

        var id = await handler.HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Lines =
            {
                new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 },
                new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 }
            }
        }, TestContext.Current.CancellationToken);

        var note = await db.Set<CreditNote>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        note.Lines.Should().ContainSingle().Which.CreditedQuantity.Should().Be(2);

        await handler.Invoking(x => x.HandleAsync(new CreditNoteCreateDto
            {
                InvoiceId = invoice.Id,
                RefundId = mismatchedRefundId,
                Lines = { new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 } }
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task IssueCreditNote_Should_Reserve_Number_Post_Finance_And_Generate_Immutable_Source()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var businessId = Guid.NewGuid();
        SeedPostingAccounts(db, businessId);
        SeedCreditNoteNumberSequence(db, businessId);
        var invoice = SeedIssuedInvoice(db, businessId);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Lines = { new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var draftRowVersion = await SetRowVersionAsync(db, id, 9);

        await LifecycleHandler(db).IssueAsync(new CreditNoteLifecycleDto
        {
            Id = id,
            RowVersion = draftRowVersion,
            ActorUserId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        var issued = await db.Set<CreditNote>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        issued.Status.Should().Be(CreditNoteStatus.Issued);
        issued.CreditNoteNumber.Should().Be("CN-20260612-001");
        issued.SourceModelJson.Should().Contain("invoiceSourceHashSha256");
        issued.SourceModelHashSha256.Should().HaveLength(64);
        issued.ArchiveGeneratedAtUtc.Should().Be(FixedNow);
        issued.ArchiveRetentionPolicyVersion.Should().Be("credit-note-v1");
        issued.PostingJournalEntryId.Should().NotBeNull();

        db.Set<JournalEntry>().Should().ContainSingle(x =>
            x.PostingKind == JournalEntryPostingKind.CreditNoteIssued &&
            x.SourceEntityType == "CreditNote" &&
            x.SourceEntityId == id);
        db.Set<BusinessEvent>().Should().ContainSingle(x =>
            x.EventType == "sales.credit_note.status_changed" &&
            x.EventKey == $"sales.credit_note.status_changed:{id:N}:Draft:Issued");
        db.Set<AuditTrail>().Should().Contain(x => x.EntityType == "CreditNote" && x.EntityId == id);
    }

    [Fact]
    public async Task VoidCreditNote_Should_Post_Reversal_Without_Duplicate_Issue_Posting()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var businessId = Guid.NewGuid();
        SeedPostingAccounts(db, businessId);
        SeedCreditNoteNumberSequence(db, businessId);
        var invoice = SeedIssuedInvoice(db, businessId);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Lines = { new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var handler = LifecycleHandler(db);
        var draftRowVersion = await SetRowVersionAsync(db, id, 10);
        await handler.IssueAsync(new CreditNoteLifecycleDto { Id = id, RowVersion = draftRowVersion }, TestContext.Current.CancellationToken);
        var issuedRowVersion = await SetRowVersionAsync(db, id, 11);

        await handler.VoidAsync(new CreditNoteLifecycleDto { Id = id, RowVersion = issuedRowVersion }, TestContext.Current.CancellationToken);

        var voided = await db.Set<CreditNote>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        voided.Status.Should().Be(CreditNoteStatus.Voided);
        db.Set<JournalEntry>().Count(x => x.SourceEntityType == "CreditNote" && x.SourceEntityId == id)
            .Should().Be(2);
        db.Set<JournalEntry>().Should().ContainSingle(x => x.PostingKind == JournalEntryPostingKind.CreditNoteIssued);
        db.Set<JournalEntry>().Should().ContainSingle(x => x.PostingKind == JournalEntryPostingKind.Reversal);
        db.Set<BusinessEvent>().Should().ContainSingle(x =>
            x.EventType == "sales.credit_note.status_changed" &&
            x.EventKey == $"sales.credit_note.status_changed:{id:N}:Issued:Voided");
    }

    [Fact]
    public async Task CreditNoteQueries_Should_Return_Filtered_NonNull_Collections()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var invoice = SeedIssuedInvoice(db, businessId: null);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Lines = { new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);

        var (items, total) = await new GetCreditNotesPageHandler(db).HandleAsync(
            page: 1,
            pageSize: 20,
            query: "INV-1001",
            filter: CreditNoteDocumentFilter.Draft,
            businessId: null,
            customerId: null,
            issuedFromUtc: null,
            issuedToUtc: null,
            ct: TestContext.Current.CancellationToken);
        var detail = await new GetCreditNoteDetailHandler(db).HandleAsync(id, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.OriginalInvoiceNumber.Should().Be("INV-1001");
        detail.Should().NotBeNull();
        detail!.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task CreditNoteQueries_Should_Return_Remaining_Creditable_Quantities_And_Source_Export()
    {
        await using var db = CreditNoteTestDbContext.Create();
        var businessId = Guid.NewGuid();
        SeedPostingAccounts(db, businessId);
        SeedCreditNoteNumberSequence(db, businessId);
        var invoice = SeedIssuedInvoice(db, businessId);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new CreditNoteCreateDto
        {
            InvoiceId = invoice.Id,
            Lines = { new() { InvoiceLineId = invoice.Lines[0].Id, CreditedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);

        var draftRemaining = await new GetInvoiceLinesForCreditNoteHandler(db)
            .HandleAsync(invoice.Id, TestContext.Current.CancellationToken);
        var draftExport = await new GetCreditNoteSourceExportHandler(db)
            .HandleAsync(id, TestContext.Current.CancellationToken);
        draftRemaining.Should().ContainSingle().Which.CreditedQuantity.Should().Be(2);
        draftExport.Should().BeNull();

        var rowVersion = await SetRowVersionAsync(db, id, 12);
        await LifecycleHandler(db).IssueAsync(new CreditNoteLifecycleDto { Id = id, RowVersion = rowVersion }, TestContext.Current.CancellationToken);

        var remaining = await new GetInvoiceLinesForCreditNoteHandler(db)
            .HandleAsync(invoice.Id, TestContext.Current.CancellationToken);
        var export = await new GetCreditNoteSourceExportHandler(db)
            .HandleAsync(id, TestContext.Current.CancellationToken);

        remaining.Should().ContainSingle().Which.CreditedQuantity.Should().Be(1);
        export.Should().NotBeNull();
        export!.FileName.Should().Be("CN-20260612-001-source-model.json");
        export.SourceModelHashSha256.Should().HaveLength(64);
        export.SourceModelJson.Should().Contain("invoiceSourceHashSha256");
    }

    private static CreateCreditNoteHandler CreateHandler(CreditNoteTestDbContext db)
        => new(
            db,
            new CreditNoteCreateValidator(),
            new TestStringLocalizer(),
            new CreditNoteLifecycleEventService(new BusinessEventService(db, new FixedClock(FixedNow)), db),
            new FixedClock(FixedNow));

    private static UpdateCreditNoteLifecycleHandler LifecycleHandler(CreditNoteTestDbContext db)
    {
        var clock = new FixedClock(FixedNow);
        return new(
            db,
            new CreditNoteLifecycleValidator(),
            new TestStringLocalizer(),
            clock,
            new NumberSequenceService(db, clock),
            new CreditNoteWorkflowPolicy(),
            new CreditNoteLifecycleEventService(new BusinessEventService(db, clock), db),
            new FinanceReceivablesPostingService(
                new FinanceAccountMappingService(db),
                new FinancePostingService(db, clock)));
    }

    private static Invoice SeedIssuedInvoice(CreditNoteTestDbContext db, Guid? businessId)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceNumber = "INV-1001",
            Status = InvoiceStatus.Open,
            Currency = "eur",
            TotalNetMinor = 2000,
            TotalTaxMinor = 380,
            TotalGrossMinor = 2380,
            DueDateUtc = FixedNow.AddDays(14),
            IssuedAtUtc = FixedNow.AddDays(-1),
            IssuedSnapshotJson = "{\"invoice\":\"INV-1001\"}",
            IssuedSnapshotHashSha256 = new string('a', 64),
            RowVersion = new byte[] { 1, 2, 3 }
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Description = " Service plan ",
            Quantity = 2,
            UnitPriceNetMinor = 1000,
            TaxRate = 0.19m,
            TotalNetMinor = 2000,
            TotalTaxMinor = 380,
            TotalGrossMinor = 2380,
            RowVersion = new byte[] { 4, 5, 6 }
        });
        db.Set<Invoice>().Add(invoice);
        return invoice;
    }

    private static void SeedCreditNoteNumberSequence(CreditNoteTestDbContext db, Guid businessId)
    {
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CreatedAtUtc = FixedNow,
            DocumentType = NumberSequenceDocumentType.CreditNote,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "CN-{yyyy}{MM}{dd}-{seq}",
            NextValue = 1,
            PaddingLength = 3,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true,
            MetadataJson = "{}",
            RowVersion = new byte[] { 7, 8, 9 }
        });
    }

    private static void SeedPostingAccounts(CreditNoteTestDbContext db, Guid businessId)
    {
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.Receivables, AccountType.Asset);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.SalesRevenue, AccountType.Revenue);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.TaxPayable, AccountType.Liability);
    }

    private static void SeedMappedAccount(
        CreditNoteTestDbContext db,
        Guid businessId,
        FinancePostingAccountRole role,
        AccountType accountType)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = role.ToString(),
            Type = accountType
        };
        db.Set<FinancialAccount>().Add(account);
        db.Set<FinancePostingAccountMapping>().Add(new FinancePostingAccountMapping
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Role = role,
            FinancialAccountId = account.Id,
            IsActive = true,
            MetadataJson = "{}"
        });
    }

    private static Guid SeedCompletedRefund(CreditNoteTestDbContext db, Guid orderId, string currency)
    {
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 1190,
            Currency = currency,
            Status = PaymentStatus.Completed
        });
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            OrderId = orderId,
            AmountMinor = 1190,
            Currency = currency,
            Status = RefundStatus.Completed,
            CompletedAtUtc = FixedNow
        };
        db.Set<Refund>().Add(refund);
        return refund.Id;
    }

    private static async Task<byte[]> SetRowVersionAsync(CreditNoteTestDbContext db, Guid id, byte value)
    {
        var note = await db.Set<CreditNote>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        note.RowVersion = new[] { value };
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return note.RowVersion;
    }

    private sealed class CreditNoteTestDbContext : DbContext, IAppDbContext
    {
        private CreditNoteTestDbContext(DbContextOptions<CreditNoteTestDbContext> options)
            : base(options)
        {
        }

        public static CreditNoteTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CreditNoteTestDbContext>()
                .UseInMemoryDatabase($"darwin_credit_note_tests_{Guid.NewGuid():N}")
                .Options;
            return new CreditNoteTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.InvoiceId);
            modelBuilder.Entity<InvoiceLine>();
            modelBuilder.Entity<CreditNote>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.CreditNoteId);
            modelBuilder.Entity<CreditNoteLine>();
            modelBuilder.Entity<Refund>();
            modelBuilder.Entity<Payment>();
            modelBuilder.Entity<ReturnOrder>();
            modelBuilder.Entity<NumberSequence>();
            modelBuilder.Entity<BusinessEvent>();
            modelBuilder.Entity<AuditTrail>();
            modelBuilder.Entity<FinancialAccount>();
            modelBuilder.Entity<FinancePostingAccountMapping>();
            modelBuilder.Entity<JournalEntry>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.JournalEntryId);
            modelBuilder.Entity<JournalEntryLine>();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }
}
