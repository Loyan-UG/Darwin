using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceReportingQueriesTests
{
    private static readonly DateTime BaseDate = new(2032, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Overview_Should_Project_Receivables_Postings_And_CreditNote_Attention()
    {
        await using var db = FinanceReportingTestDbContext.Create();
        var businessId = SeedBusiness(db, "Finance Co");
        var receivables = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        db.Set<FinancePostingAccountMapping>().Add(new FinancePostingAccountMapping
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Role = FinancePostingAccountRole.Receivables,
            FinancialAccountId = receivables,
            IsActive = true
        });
        var invoiceId = Guid.NewGuid();
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "invoice.issued:1", "Invoice", invoiceId, "INV-1", BaseDate, (receivables, 1200, 0), (revenue, 0, 1200));
        AddEntry(db, businessId, JournalEntryPostingKind.PaymentRecorded, "payment.recorded:1", "Invoice", invoiceId, "INV-1", BaseDate.AddHours(1), (receivables, 0, 500), (revenue, 500, 0));
        db.Set<CreditNote>().Add(new CreditNote
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceId = Guid.NewGuid(),
            Status = CreditNoteStatus.Issued,
            CreditNoteNumber = "CN-1",
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetFinanceOverviewHandler(db, new ReceivablesProjectionService(db, new FinanceAccountMappingService(db)));

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.BusinessId.Should().Be(businessId);
        result.OpenReceivablesMinor.Should().Be(700);
        result.PostedJournalEntryCount.Should().Be(2);
        result.SourceLinkedPostingCount.Should().Be(2);
        result.IssuedCreditNoteCount.Should().Be(1);
        result.UnpostedIssuedCreditNoteCount.Should().Be(1);
        result.PostingKindBreakdown.Should().Contain(x => x.PostingKind == JournalEntryPostingKind.InvoiceIssued && x.Count == 1);
        result.TopReceivables.Should().ContainSingle(x => x.SourceDocumentNumber == "INV-1" && x.OpenBalanceMinor == 700);
        result.RecentPostings.Should().HaveCount(2);
    }

    [Fact]
    public async Task Receivables_Page_Should_Filter_And_Page_Projectable_Sources()
    {
        await using var db = FinanceReportingTestDbContext.Create();
        var businessId = SeedBusiness(db, "Receivables Co");
        var receivables = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        db.Set<FinancePostingAccountMapping>().Add(new FinancePostingAccountMapping
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Role = FinancePostingAccountRole.Receivables,
            FinancialAccountId = receivables,
            IsActive = true
        });
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "invoice.alpha", "Invoice", Guid.NewGuid(), "INV-ALPHA", BaseDate, (receivables, 900, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "invoice.beta", "Invoice", Guid.NewGuid(), "INV-BETA", BaseDate, (receivables, 400, 0));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetFinanceReceivablesPageHandler(db, new ReceivablesProjectionService(db, new FinanceAccountMappingService(db)));

        var result = await handler.HandleAsync(businessId, "alpha", 1, 10, TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.OpenBalanceMinor.Should().Be(1300);
        result.Items.Should().ContainSingle(x => x.SourceDocumentNumber == "INV-ALPHA");
    }

    [Fact]
    public async Task Postings_Page_Should_Filter_By_Status_Kind_And_Query()
    {
        await using var db = FinanceReportingTestDbContext.Create();
        var businessId = SeedBusiness(db, "Posting Co");
        var account = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "invoice.keep", "Invoice", Guid.NewGuid(), "INV-KEEP", BaseDate, (account, 500, 0));
        AddEntry(db, businessId, JournalEntryPostingKind.PaymentRecorded, "payment.skip", "Payment", Guid.NewGuid(), "PAY-1", BaseDate, [(account, 0, 500)], JournalEntryPostingStatus.Posted);
        AddEntry(db, businessId, JournalEntryPostingKind.InvoiceIssued, "invoice.draft", "Invoice", Guid.NewGuid(), "INV-DRAFT", BaseDate, [(account, 500, 0)], JournalEntryPostingStatus.Draft);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetFinancePostingsPageHandler(db);

        var result = await handler.HandleAsync(
            businessId,
            "keep",
            JournalEntryPostingKind.InvoiceIssued,
            JournalEntryPostingStatus.Posted,
            1,
            10,
            TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.PostingKey == "invoice.keep" && x.DebitMinor == 500);
    }

    [Fact]
    public async Task AccountMappings_Page_Should_Show_All_Roles_With_Compatible_Account_Options()
    {
        await using var db = FinanceReportingTestDbContext.Create();
        var businessId = SeedBusiness(db, "Mapping Co");
        var receivables = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        var liability = SeedAccount(db, businessId, AccountType.Liability, "Tax");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);
        await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            receivables,
            Description: "Primary receivables"), TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetFinanceAccountMappingsPageHandler(db, service);

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.BusinessId.Should().Be(businessId);
        result.Rows.Should().HaveCount(Enum.GetValues<FinancePostingAccountRole>().Length);
        result.Rows.Single(x => x.Role == FinancePostingAccountRole.Receivables).FinancialAccountId.Should().Be(receivables);
        result.Rows.Single(x => x.Role == FinancePostingAccountRole.SalesRevenue).CompatibleAccountOptions.Should().ContainSingle(x => x.Id == revenue);
        result.Rows.Single(x => x.Role == FinancePostingAccountRole.TaxPayable).CompatibleAccountOptions.Should().ContainSingle(x => x.Id == liability);
        result.MissingRequiredMappingCount.Should().BeGreaterThan(0);
        result.IncompatibleMappingCount.Should().Be(0);
    }

    [Fact]
    public async Task UpsertAccountMapping_Handler_Should_Delegate_To_Service_And_Reject_Incompatible_Account()
    {
        await using var db = FinanceReportingTestDbContext.Create();
        var businessId = SeedBusiness(db, "Mapping Command Co");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        var handler = new UpsertFinanceAccountMappingHandler(new FinanceAccountMappingService(db));

        var act = () => handler.HandleAsync(new FinanceAccountMappingUpsertDto
        {
            BusinessId = businessId,
            Role = FinancePostingAccountRole.Receivables,
            FinancialAccountId = revenue,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        db.Set<FinancePostingAccountMapping>().Should().BeEmpty();
    }

    private static Guid SeedBusiness(FinanceReportingTestDbContext db, string name)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = name,
            DefaultCurrency = "EUR"
        };
        db.Set<Business>().Add(business);
        return business.Id;
    }

    private static Guid SeedAccount(FinanceReportingTestDbContext db, Guid businessId, AccountType type, string name)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Type = type,
            Name = name
        };
        db.Set<FinancialAccount>().Add(account);
        return account.Id;
    }

    private static JournalEntry AddEntry(
        FinanceReportingTestDbContext db,
        Guid businessId,
        JournalEntryPostingKind kind,
        string postingKey,
        string sourceType,
        Guid sourceId,
        string documentNumber,
        DateTime entryDateUtc,
        params (Guid AccountId, long DebitMinor, long CreditMinor)[] lines)
        => AddEntry(db, businessId, kind, postingKey, sourceType, sourceId, documentNumber, entryDateUtc, lines, JournalEntryPostingStatus.Posted);

    private static JournalEntry AddEntry(
        FinanceReportingTestDbContext db,
        Guid businessId,
        JournalEntryPostingKind kind,
        string postingKey,
        string sourceType,
        Guid sourceId,
        string documentNumber,
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
            SourceDocumentNumber = documentNumber,
            PostedAtUtc = status == JournalEntryPostingStatus.Posted ? entryDateUtc : null,
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

    private sealed class FinanceReportingTestDbContext : DbContext, IAppDbContext
    {
        private FinanceReportingTestDbContext(DbContextOptions<FinanceReportingTestDbContext> options)
            : base(options)
        {
        }

        public static FinanceReportingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceReportingTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_reporting_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceReportingTestDbContext(options);
        }

        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
        public DbSet<FinancePostingAccountMapping> FinancePostingAccountMappings => Set<FinancePostingAccountMapping>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
        public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business>().Ignore(x => x.Members);
            modelBuilder.Entity<Business>().Ignore(x => x.Locations);
            modelBuilder.Entity<Business>().Ignore(x => x.Favorites);
            modelBuilder.Entity<Business>().Ignore(x => x.Likes);
            modelBuilder.Entity<Business>().Ignore(x => x.Reviews);
            modelBuilder.Entity<Business>().Ignore(x => x.EngagementStats);
        }
    }
}
