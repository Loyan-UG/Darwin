using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinancePostingServiceTests
{
    private static readonly DateTime FixedNow = new(2031, 2, 3, 10, 15, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PostAsync_Should_CreatePostedJournalEntry_WithBalancedLines()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var creditAccountId = SeedAccount(db, businessId, AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);
        var sourceId = Guid.NewGuid();

        var result = await service.PostAsync(new FinancePostingCommand(
            businessId,
            default,
            JournalEntryPostingKind.InvoiceIssued,
            " invoice-issued-1 ",
            " Invoice ",
            sourceId,
            " Invoice issued ",
            [
                new FinancePostingLineCommand(debitAccountId, 1250, 0, " Receivable "),
                new FinancePostingLineCommand(creditAccountId, 0, 1250, " Revenue ")
            ],
            SourceDocumentNumber: " INV-1001 ",
            PostingReason: "Initial issue",
            MetadataJson: "{\"source\":\"unit\"}"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Created.Should().BeTrue();
        var entry = await db.Set<JournalEntry>()
            .Include(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);
        entry.PostingStatus.Should().Be(JournalEntryPostingStatus.Posted);
        entry.PostingKind.Should().Be(JournalEntryPostingKind.InvoiceIssued);
        entry.PostingKey.Should().Be("invoice-issued-1");
        entry.SourceEntityType.Should().Be("Invoice");
        entry.SourceEntityId.Should().Be(sourceId);
        entry.SourceDocumentNumber.Should().Be("INV-1001");
        entry.Description.Should().Be("Invoice issued");
        entry.EntryDateUtc.Should().Be(FixedNow);
        entry.PostedAtUtc.Should().Be(FixedNow);
        entry.MetadataJson.Should().Be("{\"source\":\"unit\"}");
        entry.Lines.Should().HaveCount(2);
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(1250);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(1250);
        entry.Lines.Select(x => x.Memo).Should().BeEquivalentTo("Receivable", "Revenue");
    }

    [Fact]
    public async Task PostAsync_Should_ReturnExistingJournalEntry_ForSamePostingKeyAndSource()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var creditAccountId = SeedAccount(db, businessId, AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);
        var sourceId = Guid.NewGuid();
        var command = BuildCommand(businessId, debitAccountId, creditAccountId, sourceId);

        var first = await service.PostAsync(command, TestContext.Current.CancellationToken);
        var duplicate = await service.PostAsync(command with { Description = "Retry should not matter" }, TestContext.Current.CancellationToken);

        first.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeTrue();
        duplicate.Value!.Created.Should().BeFalse();
        duplicate.Value.JournalEntryId.Should().Be(first.Value!.JournalEntryId);
        db.Set<JournalEntry>().Should().ContainSingle();
    }

    [Fact]
    public async Task PostAsync_Should_RejectPostingKeyUsedForDifferentSource()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var creditAccountId = SeedAccount(db, businessId, AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.PostAsync(BuildCommand(businessId, debitAccountId, creditAccountId, Guid.NewGuid()), TestContext.Current.CancellationToken);
        var mismatch = await service.PostAsync(BuildCommand(businessId, debitAccountId, creditAccountId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        mismatch.Succeeded.Should().BeFalse();
        mismatch.Error.Should().Contain("Posting key");
        db.Set<JournalEntry>().Should().ContainSingle();
    }

    [Fact]
    public async Task PostAsync_Should_RejectUnbalancedOrInvalidLines()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var creditAccountId = SeedAccount(db, businessId, AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        var unbalanced = await service.PostAsync(BuildCommand(
            businessId,
            debitAccountId,
            creditAccountId,
            Guid.NewGuid(),
            debit: 100,
            credit: 90), TestContext.Current.CancellationToken);
        var bothDebitAndCredit = await service.PostAsync(new FinancePostingCommand(
            businessId,
            default,
            JournalEntryPostingKind.InvoiceIssued,
            "posting-invalid-line",
            "Invoice",
            Guid.NewGuid(),
            "Invalid",
            [
                new FinancePostingLineCommand(debitAccountId, 100, 10),
                new FinancePostingLineCommand(creditAccountId, 0, 110)
            ]), TestContext.Current.CancellationToken);

        unbalanced.Succeeded.Should().BeFalse();
        bothDebitAndCredit.Succeeded.Should().BeFalse();
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task PostAsync_Should_RejectMissingOrCrossBusinessAccounts()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var otherBusinessAccountId = SeedAccount(db, Guid.NewGuid(), AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        var crossBusiness = await service.PostAsync(BuildCommand(
            businessId,
            debitAccountId,
            otherBusinessAccountId,
            Guid.NewGuid()), TestContext.Current.CancellationToken);
        var missing = await service.PostAsync(BuildCommand(
            businessId,
            debitAccountId,
            Guid.NewGuid(),
            Guid.NewGuid()), TestContext.Current.CancellationToken);

        crossBusiness.Succeeded.Should().BeFalse();
        crossBusiness.Error.Should().Contain("business");
        missing.Succeeded.Should().BeFalse();
        missing.Error.Should().Contain("exist");
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task PostAsync_Should_RejectManualKindAndSensitiveMetadata()
    {
        await using var db = FinancePostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var debitAccountId = SeedAccount(db, businessId, AccountType.Asset);
        var creditAccountId = SeedAccount(db, businessId, AccountType.Revenue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        var manual = await service.PostAsync(BuildCommand(
            businessId,
            debitAccountId,
            creditAccountId,
            Guid.NewGuid(),
            kind: JournalEntryPostingKind.Manual), TestContext.Current.CancellationToken);
        var sensitive = await service.PostAsync(BuildCommand(
            businessId,
            debitAccountId,
            creditAccountId,
            Guid.NewGuid(),
            metadataJson: "{\"refreshToken\":\"secret\"}"), TestContext.Current.CancellationToken);

        manual.Succeeded.Should().BeFalse();
        sensitive.Succeeded.Should().BeFalse();
        sensitive.Error.Should().Contain("Sensitive");
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    private static FinancePostingCommand BuildCommand(
        Guid businessId,
        Guid debitAccountId,
        Guid creditAccountId,
        Guid sourceId,
        long debit = 100,
        long credit = 100,
        JournalEntryPostingKind kind = JournalEntryPostingKind.InvoiceIssued,
        string? metadataJson = null)
        => new(
            businessId,
            FixedNow,
            kind,
            "posting-key-1",
            "Invoice",
            sourceId,
            "Invoice posting",
            [
                new FinancePostingLineCommand(debitAccountId, debit, 0),
                new FinancePostingLineCommand(creditAccountId, 0, credit)
            ],
            MetadataJson: metadataJson);

    private static FinancePostingService CreateService(IAppDbContext db)
        => new(db, new FixedClock(FixedNow));

    private static Guid SeedAccount(FinancePostingTestDbContext db, Guid businessId, AccountType accountType)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = accountType.ToString(),
            Type = accountType
        };
        db.Set<FinancialAccount>().Add(account);
        return account.Id;
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class FinancePostingTestDbContext : DbContext, IAppDbContext
    {
        private FinancePostingTestDbContext(DbContextOptions<FinancePostingTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinancePostingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinancePostingTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_posting_tests_{Guid.NewGuid()}")
                .Options;
            return new FinancePostingTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.MetadataJson).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId);
            });

            modelBuilder.Entity<JournalEntryLine>(b =>
            {
                b.HasKey(x => x.Id);
            });
        }
    }
}
