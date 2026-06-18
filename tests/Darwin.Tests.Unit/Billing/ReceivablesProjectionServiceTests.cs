using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class ReceivablesProjectionServiceTests
{
    private static readonly DateTime BaseDate = new(2031, 4, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSummaryAsync_Should_ProjectOpenReceivables_FromPostedJournalLines()
    {
        await using var db = ReceivablesProjectionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenueAccountId = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var mappingService = new FinanceAccountMappingService(db);
        await mappingService.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            receivablesAccountId), TestContext.Current.CancellationToken);
        var invoiceId = Guid.NewGuid();
        AddEntry(
            db,
            businessId,
            JournalEntryPostingKind.InvoiceIssued,
            "invoice-issued",
            "Invoice",
            invoiceId,
            "INV-100",
            BaseDate,
            (receivablesAccountId, 1200, 0),
            (revenueAccountId, 0, 1200));
        AddEntry(
            db,
            businessId,
            JournalEntryPostingKind.PaymentRecorded,
            "payment-recorded",
            "Invoice",
            invoiceId,
            "INV-100",
            BaseDate.AddHours(2),
            (receivablesAccountId, 0, 500),
            (revenueAccountId, 500, 0));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReceivablesProjectionService(db, mappingService);

        var result = await service.GetSummaryAsync(
            new ReceivablesProjectionQuery(businessId),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ReceivablesAccountId.Should().Be(receivablesAccountId);
        result.Value.TotalDebitMinor.Should().Be(1200);
        result.Value.TotalCreditMinor.Should().Be(500);
        result.Value.OpenBalanceMinor.Should().Be(700);
        result.Value.Sources.Should().ContainSingle();
        result.Value.Sources[0].SourceEntityType.Should().Be("Invoice");
        result.Value.Sources[0].SourceEntityId.Should().Be(invoiceId);
        result.Value.Sources[0].SourceDocumentNumber.Should().Be("INV-100");
        result.Value.Sources[0].OpenBalanceMinor.Should().Be(700);
        result.Value.Sources[0].LastPostingKind.Should().Be(JournalEntryPostingKind.PaymentRecorded);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_IgnoreDraftVoidedDeletedAndNonReceivableLines()
    {
        await using var db = ReceivablesProjectionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenueAccountId = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var mappingService = new FinanceAccountMappingService(db);
        await mappingService.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            receivablesAccountId), TestContext.Current.CancellationToken);
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "posted", "Invoice", Guid.NewGuid(), "INV-1", BaseDate, (receivablesAccountId, 1000, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "draft", "Invoice", Guid.NewGuid(), "INV-2", BaseDate, [(receivablesAccountId, 500, 0)], JournalEntryPostingStatus.Draft);
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "voided", "Invoice", Guid.NewGuid(), "INV-3", BaseDate, [(receivablesAccountId, 500, 0)], JournalEntryPostingStatus.Voided);
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "revenue-only", "Invoice", Guid.NewGuid(), "INV-4", BaseDate, (revenueAccountId, 0, 800));
        var deleted = AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "deleted", "Invoice", Guid.NewGuid(), "INV-5", BaseDate, (receivablesAccountId, 700, 0));
        deleted.IsDeleted = true;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReceivablesProjectionService(db, mappingService);

        var result = await service.GetSummaryAsync(new ReceivablesProjectionQuery(businessId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.OpenBalanceMinor.Should().Be(1000);
        result.Value.Sources.Should().ContainSingle();
        result.Value.Sources[0].SourceDocumentNumber.Should().Be("INV-1");
    }

    [Fact]
    public async Task GetSummaryAsync_Should_FilterByDateAndSource()
    {
        await using var db = ReceivablesProjectionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var mappingService = new FinanceAccountMappingService(db);
        await mappingService.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            receivablesAccountId), TestContext.Current.CancellationToken);
        var targetId = Guid.NewGuid();
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "before", "Invoice", targetId, "INV-1", BaseDate.AddDays(-2), (receivablesAccountId, 400, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "inside", "Invoice", targetId, "INV-1", BaseDate, (receivablesAccountId, 900, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "other", "Invoice", Guid.NewGuid(), "INV-2", BaseDate, (receivablesAccountId, 300, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "after", "Invoice", targetId, "INV-1", BaseDate.AddDays(2), (receivablesAccountId, 200, 0));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReceivablesProjectionService(db, mappingService);

        var result = await service.GetSummaryAsync(new ReceivablesProjectionQuery(
            businessId,
            FromUtc: BaseDate.AddDays(-1),
            AsOfUtc: BaseDate.AddDays(1),
            SourceEntityType: " Invoice ",
            SourceEntityId: targetId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.OpenBalanceMinor.Should().Be(900);
        result.Value.Sources.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSummaryAsync_Should_FailClosed_WhenReceivablesMappingIsMissingOrInactive()
    {
        await using var db = ReceivablesProjectionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var mappingService = new FinanceAccountMappingService(db);
        var service = new ReceivablesProjectionService(db, mappingService);

        var missing = await service.GetSummaryAsync(new ReceivablesProjectionQuery(businessId), TestContext.Current.CancellationToken);
        await mappingService.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            accountId,
            IsActive: false), TestContext.Current.CancellationToken);
        var inactive = await service.GetSummaryAsync(new ReceivablesProjectionQuery(businessId), TestContext.Current.CancellationToken);

        missing.Succeeded.Should().BeFalse();
        missing.Error.Should().Contain(nameof(FinancePostingAccountRole.Receivables));
        inactive.Succeeded.Should().BeFalse();
        inactive.Error.Should().Contain(nameof(FinancePostingAccountRole.Receivables));
    }

    [Fact]
    public async Task GetSummaryAsync_Should_RejectEmptyBusinessAndInvalidDateRange()
    {
        await using var db = ReceivablesProjectionTestDbContext.Create();
        var service = new ReceivablesProjectionService(db, new FinanceAccountMappingService(db));

        var emptyBusiness = await service.GetSummaryAsync(new ReceivablesProjectionQuery(Guid.Empty), TestContext.Current.CancellationToken);
        var invalidRange = await service.GetSummaryAsync(new ReceivablesProjectionQuery(
            Guid.NewGuid(),
            FromUtc: BaseDate.AddDays(1),
            AsOfUtc: BaseDate), TestContext.Current.CancellationToken);

        emptyBusiness.Succeeded.Should().BeFalse();
        invalidRange.Succeeded.Should().BeFalse();
    }

    private static Guid SeedAccount(ReceivablesProjectionTestDbContext db, Guid businessId, AccountType accountType, string name)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = name,
            Type = accountType
        };
        db.Set<FinancialAccount>().Add(account);
        return account.Id;
    }

    private static JournalEntry AddEntry(
        ReceivablesProjectionTestDbContext db,
        Guid businessId,
        JournalEntryPostingKind kind,
        string postingKey,
        string sourceType,
        Guid sourceId,
        string sourceDocumentNumber,
        DateTime entryDateUtc,
        params (Guid AccountId, long DebitMinor, long CreditMinor)[] lines)
        => AddEntry(db, businessId, kind, postingKey, sourceType, sourceId, sourceDocumentNumber, entryDateUtc, lines, JournalEntryPostingStatus.Posted);

    private static JournalEntry AddEntry(
        ReceivablesProjectionTestDbContext db,
        Guid businessId,
        JournalEntryPostingKind kind,
        string postingKey,
        string sourceType,
        Guid sourceId,
        string sourceDocumentNumber,
        DateTime entryDateUtc,
        (Guid AccountId, long DebitMinor, long CreditMinor)[] lines,
        JournalEntryPostingStatus status)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            EntryDateUtc = entryDateUtc,
            Description = postingKey,
            PostingStatus = status,
            PostingKind = kind,
            PostingKey = postingKey,
            SourceEntityType = sourceType,
            SourceEntityId = sourceId,
            SourceDocumentNumber = sourceDocumentNumber,
            PostedAtUtc = status == JournalEntryPostingStatus.Draft ? null : entryDateUtc,
            Lines = lines.Select(line => new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                AccountId = line.AccountId,
                DebitMinor = line.DebitMinor,
                CreditMinor = line.CreditMinor
            }).ToList()
        };
        db.Set<JournalEntry>().Add(entry);
        return entry;
    }

    private sealed class ReceivablesProjectionTestDbContext : DbContext, IAppDbContext
    {
        private ReceivablesProjectionTestDbContext(DbContextOptions<ReceivablesProjectionTestDbContext> options)
            : base(options)
        {
        }

        public static ReceivablesProjectionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ReceivablesProjectionTestDbContext>()
                .UseInMemoryDatabase($"darwin_receivables_projection_tests_{Guid.NewGuid()}")
                .Options;
            return new ReceivablesProjectionTestDbContext(options);
        }

        public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
        public DbSet<FinancePostingAccountMapping> FinancePostingAccountMappings => Set<FinancePostingAccountMapping>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    }
}
