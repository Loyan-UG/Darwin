using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class BankReconciliationHandlersTests
{
    private static readonly DateTime Now = new(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_Should_Normalize_Totals_And_Record_Evidence()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db);

        var id = await new CreateBankReconciliationMatchHandler(db, Clock(), Events(db)).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);

        var match = await db.Set<BankReconciliationMatch>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        match.BankTotalMinor.Should().Be(1000);
        match.FinanceTotalMinor.Should().Be(1000);
        match.DifferenceMinor.Should().Be(0);
        match.Lines.Should().ContainSingle(x => x.JournalEntryId == seed.JournalEntryId && x.IsActive);
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "treasury.bank_reconciliation.created");
    }

    [Fact]
    public async Task Create_Should_Reject_Duplicate_Active_Statement_Line()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db);
        await new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);

        var act = () => new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*StatementLineAlreadyMatched*");
    }

    [Fact]
    public async Task Create_Should_Reject_Cancelled_Statement_Import_And_Draft_Posting()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db, importStatus: BankStatementImportStatus.Cancelled);

        var cancelledAct = () => new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);
        await cancelledAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BankStatementImportNotImported*");

        await using var draftDb = BankReconciliationTestDbContext.Create();
        var draftSeed = await SeedAsync(draftDb, postingStatus: JournalEntryPostingStatus.Draft);
        var draftAct = () => new CreateBankReconciliationMatchHandler(draftDb, Clock()).HandleAsync(CreateDto(draftSeed), TestContext.Current.CancellationToken);
        await draftAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PostingNotEligible*");
    }

    [Fact]
    public async Task MarkMatched_Should_Allow_Zero_Difference_And_Not_Mutate_Finance_Facts()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db);
        var supplierPayment = new SupplierPayment { Id = Guid.NewGuid(), BusinessId = seed.BusinessId, SupplierId = Guid.NewGuid(), Status = SupplierPaymentStatus.Posted, Currency = "EUR", TotalAmountMinor = 1000 };
        var payment = new Payment { Id = Guid.NewGuid(), BusinessId = seed.BusinessId, AmountMinor = 1000, Currency = "EUR" };
        var refund = new Refund { Id = Guid.NewGuid(), PaymentId = payment.Id, AmountMinor = 1000, Currency = "EUR", Status = RefundStatus.Completed };
        var exportBatch = new FinanceExportBatch { Id = Guid.NewGuid(), BusinessId = seed.BusinessId, ExternalSystemId = Guid.NewGuid(), ExportKey = "bank-recon-test", Status = FinanceExportBatchStatus.Generated, PeriodStartUtc = Now.Date, PeriodEndUtc = Now.Date.AddDays(1), PostingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed };
        db.AddRange(supplierPayment, payment, refund, exportBatch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);
        var match = await db.Set<BankReconciliationMatch>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        match.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await new MarkBankReconciliationMatchedHandler(db, Clock(), Events(db)).HandleAsync(new BankReconciliationLifecycleActionDto { Id = id, RowVersion = [1, 2, 3] }, TestContext.Current.CancellationToken);

        (await db.Set<BankReconciliationMatch>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken)).Status.Should().Be(BankReconciliationMatchStatus.Matched);
        (await db.Set<SupplierPayment>().SingleAsync(x => x.Id == supplierPayment.Id, TestContext.Current.CancellationToken)).Status.Should().Be(SupplierPaymentStatus.Posted);
        (await db.Set<Payment>().SingleAsync(x => x.Id == payment.Id, TestContext.Current.CancellationToken)).AmountMinor.Should().Be(1000);
        (await db.Set<Refund>().SingleAsync(x => x.Id == refund.Id, TestContext.Current.CancellationToken)).Status.Should().Be(RefundStatus.Completed);
        (await db.Set<JournalEntry>().SingleAsync(x => x.Id == seed.JournalEntryId, TestContext.Current.CancellationToken)).PostingStatus.Should().Be(JournalEntryPostingStatus.Posted);
        (await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == exportBatch.Id, TestContext.Current.CancellationToken)).Status.Should().Be(FinanceExportBatchStatus.Generated);
    }

    [Fact]
    public async Task MarkMatched_Should_Require_Review_Note_For_NonZero_Difference()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db, journalAmountMinor: 500);
        var dto = CreateDto(seed);
        var id = await new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(dto, TestContext.Current.CancellationToken);
        var match = await db.Set<BankReconciliationMatch>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        match.RowVersion = [5, 6, 7];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => new MarkBankReconciliationMatchedHandler(db, Clock()).HandleAsync(new BankReconciliationLifecycleActionDto { Id = id, RowVersion = [5, 6, 7] }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*DifferenceRequiresReviewNotes*");
        match.ReviewNotes = "Accepted timing difference";
        match.RowVersion = [5, 6, 7];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await new MarkBankReconciliationMatchedHandler(db, Clock()).HandleAsync(new BankReconciliationLifecycleActionDto { Id = id, RowVersion = [5, 6, 7] }, TestContext.Current.CancellationToken);
        (await db.Set<BankReconciliationMatch>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken)).Status.Should().Be(BankReconciliationMatchStatus.Matched);
    }

    [Fact]
    public async Task Cancel_Should_Deactivate_Lines_Without_Rewriting_History()
    {
        await using var db = BankReconciliationTestDbContext.Create();
        var seed = await SeedAsync(db);
        var id = await new CreateBankReconciliationMatchHandler(db, Clock()).HandleAsync(CreateDto(seed), TestContext.Current.CancellationToken);
        var match = await db.Set<BankReconciliationMatch>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        match.RowVersion = [9, 9, 9];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await new CancelBankReconciliationMatchHandler(db, Clock()).HandleAsync(new BankReconciliationLifecycleActionDto { Id = id, RowVersion = [9, 9, 9] }, TestContext.Current.CancellationToken);

        var cancelled = await db.Set<BankReconciliationMatch>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        cancelled.Status.Should().Be(BankReconciliationMatchStatus.Cancelled);
        cancelled.Lines.Should().OnlyContain(x => !x.IsActive);
    }

    private static BankReconciliationCreateDto CreateDto(SeedIds seed) => new()
    {
        BusinessId = seed.BusinessId,
        BankAccountId = seed.BankAccountId,
        MatchDateUtc = Now,
        Currency = "eur",
        MetadataJson = "{}",
        Lines =
        [
            new BankReconciliationLineDto
            {
                BankStatementLineId = seed.StatementLineId,
                JournalEntryId = seed.JournalEntryId,
                AmountMinor = 1000,
                Memo = "settlement"
            }
        ]
    };

    private static async Task<SeedIds> SeedAsync(BankReconciliationTestDbContext db, BankStatementImportStatus importStatus = BankStatementImportStatus.Imported, JournalEntryPostingStatus postingStatus = JournalEntryPostingStatus.Posted, long journalAmountMinor = 1000)
    {
        var ids = new SeedIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        db.Set<BankAccount>().Add(new BankAccount { Id = ids.BankAccountId, BusinessId = ids.BusinessId, Code = "MAIN", DisplayName = "Main bank", Currency = "EUR", Status = BankAccountStatus.Active });
        db.Set<BankStatementImport>().Add(new BankStatementImport { Id = ids.ImportId, BusinessId = ids.BusinessId, BankAccountId = ids.BankAccountId, StatementReference = "ST-1", PeriodStartUtc = Now.Date, PeriodEndUtc = Now.Date.AddDays(1), ImportedAtUtc = Now, Status = importStatus });
        db.Set<BankStatementLine>().Add(new BankStatementLine { Id = ids.StatementLineId, BusinessId = ids.BusinessId, BankAccountId = ids.BankAccountId, BankStatementImportId = ids.ImportId, TransactionDateUtc = Now, Direction = BankStatementLineDirection.Debit, AmountMinor = 1000, Currency = "EUR", NormalizedIdentityKey = "BANK-LINE-1", ReviewStatus = BankStatementLineReviewStatus.Unreviewed });
        db.Set<JournalEntry>().Add(new JournalEntry { Id = ids.JournalEntryId, BusinessId = ids.BusinessId, EntryDateUtc = Now, Description = "Supplier payment", PostingStatus = postingStatus, PostingKind = JournalEntryPostingKind.SupplierPaymentPosted, PostingKey = "supplier-payment-posted:test", SourceEntityType = "SupplierPayment", SourceEntityId = Guid.NewGuid(), Lines = [new JournalEntryLine { Id = Guid.NewGuid(), JournalEntryId = ids.JournalEntryId, AccountId = Guid.NewGuid(), DebitMinor = journalAmountMinor, CreditMinor = 0 }, new JournalEntryLine { Id = Guid.NewGuid(), JournalEntryId = ids.JournalEntryId, AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = journalAmountMinor }] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ids;
    }

    private static BusinessEventService Events(BankReconciliationTestDbContext db) => new(db, Clock());
    private static FixedClock Clock() => new(Now);

    private sealed record SeedIds(Guid BusinessId, Guid BankAccountId, Guid ImportId, Guid StatementLineId, Guid JournalEntryId);

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class BankReconciliationTestDbContext : DbContext, IAppDbContext
    {
        private BankReconciliationTestDbContext(DbContextOptions<BankReconciliationTestDbContext> options) : base(options) { }
        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BankReconciliationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BankReconciliationTestDbContext>()
                .UseInMemoryDatabase($"darwin_bank_reconciliation_tests_{Guid.NewGuid()}")
                .Options;
            return new BankReconciliationTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BankAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementImport>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementLine>().HasKey(x => x.Id);
            modelBuilder.Entity<BankReconciliationMatch>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankReconciliationMatchId);
            });
            modelBuilder.Entity<BankReconciliationMatchLine>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId);
            });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
            modelBuilder.Entity<SupplierPayment>().HasKey(x => x.Id);
            modelBuilder.Entity<Payment>().HasKey(x => x.Id);
            modelBuilder.Entity<Refund>().HasKey(x => x.Id);
            modelBuilder.Entity<FinanceExportBatch>().HasKey(x => x.Id);
            modelBuilder.Entity<BusinessEvent>().HasKey(x => x.Id);
            modelBuilder.Entity<AuditTrail>().HasKey(x => x.Id);
        }
    }
}
