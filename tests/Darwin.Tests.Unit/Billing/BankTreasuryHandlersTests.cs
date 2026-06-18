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

public sealed class BankTreasuryHandlersTests
{
    private static readonly DateTime Now = new(2036, 4, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateBankAccount_Should_NormalizeValidateAssetMappingAndClearPreviousDefault()
    {
        await using var db = BankTreasuryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var previousDefault = new BankAccount
        {
            BusinessId = businessId,
            Code = "OPER",
            DisplayName = "Operating",
            Currency = "EUR",
            Status = BankAccountStatus.Active,
            IsDefault = true,
            MetadataJson = "{}"
        };
        var assetAccount = SeedFinancialAccount(db, businessId, AccountType.Asset);
        db.Set<BankAccount>().Add(previousDefault);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBankAccountHandler(db, Clock(), Events(db));
        var id = await handler.HandleAsync(new BankAccountCreateDto
        {
            BusinessId = businessId,
            FinancialAccountId = assetAccount.Id,
            Code = " treasury ",
            DisplayName = " Treasury Account ",
            BankName = " House Bank ",
            Currency = " eur ",
            MaskedAccountIdentifier = "****1234",
            IsDefault = true,
            MetadataJson = "{}"
        }, TestContext.Current.CancellationToken);

        var created = await db.Set<BankAccount>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        created.Code.Should().Be("treasury");
        created.DisplayName.Should().Be("Treasury Account");
        created.Currency.Should().Be("EUR");
        created.IsDefault.Should().BeTrue();
        (await db.Set<BankAccount>().SingleAsync(x => x.Id == previousDefault.Id, TestContext.Current.CancellationToken)).IsDefault.Should().BeFalse();
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "treasury.bank_account.created");
        db.Set<AuditTrail>().Should().ContainSingle(x => x.Action == AuditTrailAction.Created);
    }

    [Fact]
    public async Task CreateBankAccount_Should_RejectNonAssetFinancialAccountAndRawAccountNumber()
    {
        await using var db = BankTreasuryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var liability = SeedFinancialAccount(db, businessId, AccountType.Liability);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateBankAccountHandler(db, Clock());

        var wrongType = async () => await handler.HandleAsync(new BankAccountCreateDto
        {
            BusinessId = businessId,
            FinancialAccountId = liability.Id,
            Code = "BANK",
            DisplayName = "Bank",
            Currency = "EUR",
            MaskedAccountIdentifier = "****9999",
            MetadataJson = "{}"
        }, TestContext.Current.CancellationToken);

        await wrongType.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BankAccountFinancialAccountMustBeAsset*");

        var rawAccount = async () => await handler.HandleAsync(new BankAccountCreateDto
        {
            BusinessId = businessId,
            Code = "BANK2",
            DisplayName = "Bank 2",
            Currency = "EUR",
            MaskedAccountIdentifier = "123456789012",
            MetadataJson = "{}"
        }, TestContext.Current.CancellationToken);

        await rawAccount.Should().ThrowAsync<ArgumentException>().WithMessage("*BankAccountIdentifierMustBeMasked*");
    }

    [Fact]
    public async Task StatementImport_Should_CreateEvidenceOnlyAndReconcileTotals()
    {
        await using var db = BankTreasuryTestDbContext.Create();
        var account = await SeedBankAccountAsync(db);
        var handler = new CreateBankStatementImportHandler(db, Clock(), Events(db));

        var id = await handler.HandleAsync(BuildStatement(account), TestContext.Current.CancellationToken);

        var import = await db.Set<BankStatementImport>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        import.LineCount.Should().Be(2);
        import.DebitTotalMinor.Should().Be(1500);
        import.CreditTotalMinor.Should().Be(2500);
        import.Lines.Should().OnlyContain(x => x.ReviewStatus == BankStatementLineReviewStatus.Unreviewed);
        db.Set<JournalEntry>().Should().BeEmpty();
        db.Set<SupplierPayment>().Should().BeEmpty();
        db.Set<Payment>().Should().BeEmpty();
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "treasury.bank_statement.imported");
    }

    [Fact]
    public async Task StatementImport_Should_RejectDuplicateIdentityWrongCurrencyAndSensitiveMetadata()
    {
        await using var db = BankTreasuryTestDbContext.Create();
        var account = await SeedBankAccountAsync(db);
        var handler = new CreateBankStatementImportHandler(db, Clock());

        var duplicate = BuildStatement(account);
        duplicate.Lines[1].NormalizedIdentityKey = duplicate.Lines[0].NormalizedIdentityKey;
        var duplicateAct = async () => await handler.HandleAsync(duplicate, TestContext.Current.CancellationToken);
        await duplicateAct.Should().ThrowAsync<ArgumentException>().WithMessage("*BankStatementLineDuplicateIdentity*");

        var wrongCurrency = BuildStatement(account);
        wrongCurrency.Lines[0].Currency = "USD";
        var currencyAct = async () => await handler.HandleAsync(wrongCurrency, TestContext.Current.CancellationToken);
        await currencyAct.Should().ThrowAsync<ArgumentException>().WithMessage("*BankStatementLineCurrencyMismatch*");

        var sensitive = BuildStatement(account);
        sensitive.Lines[0].MetadataJson = "{\"refreshToken\":\"value\"}";
        var sensitiveAct = async () => await handler.HandleAsync(sensitive, TestContext.Current.CancellationToken);
        await sensitiveAct.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
    }

    [Fact]
    public async Task CancelStatementImport_Should_MarkImportCancelledAndIgnoreLines()
    {
        await using var db = BankTreasuryTestDbContext.Create();
        var account = await SeedBankAccountAsync(db);
        var id = await new CreateBankStatementImportHandler(db, Clock()).HandleAsync(BuildStatement(account), TestContext.Current.CancellationToken);
        var import = await db.Set<BankStatementImport>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        import.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await new CancelBankStatementImportHandler(db, Clock()).HandleAsync(new BankStatementImportLifecycleActionDto { Id = id, RowVersion = [1, 2, 3] }, TestContext.Current.CancellationToken);

        var cancelled = await db.Set<BankStatementImport>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        cancelled.Status.Should().Be(BankStatementImportStatus.Cancelled);
        cancelled.Lines.Should().OnlyContain(x => x.ReviewStatus == BankStatementLineReviewStatus.Ignored);
    }

    private static BankStatementImportCreateDto BuildStatement(BankAccount account)
        => new()
        {
            BusinessId = account.BusinessId,
            BankAccountId = account.Id,
            StatementReference = " STMT-1 ",
            PeriodStartUtc = Now.AddDays(-7),
            PeriodEndUtc = Now,
            MetadataJson = "{}",
            Lines =
            [
                new BankStatementLineDto
                {
                    TransactionDateUtc = Now.AddDays(-2),
                    Direction = BankStatementLineDirection.Debit,
                    AmountMinor = 1500,
                    Currency = " eur ",
                    CounterpartyName = "Supplier",
                    CounterpartyReference = "INV-1",
                    RemittanceInformation = "Payment",
                    NormalizedIdentityKey = "line-1",
                    MetadataJson = "{}"
                },
                new BankStatementLineDto
                {
                    TransactionDateUtc = Now.AddDays(-1),
                    Direction = BankStatementLineDirection.Credit,
                    AmountMinor = 2500,
                    Currency = "EUR",
                    CounterpartyName = "Customer",
                    CounterpartyReference = "PAY-1",
                    RemittanceInformation = "Receipt",
                    NormalizedIdentityKey = "line-2",
                    MetadataJson = "{}"
                }
            ]
        };

    private static async Task<BankAccount> SeedBankAccountAsync(BankTreasuryTestDbContext db)
    {
        var account = new BankAccount
        {
            BusinessId = Guid.NewGuid(),
            Code = "BANK",
            DisplayName = "Bank",
            Currency = "EUR",
            Status = BankAccountStatus.Active,
            MetadataJson = "{}"
        };
        db.Set<BankAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return account;
    }

    private static FinancialAccount SeedFinancialAccount(BankTreasuryTestDbContext db, Guid businessId, AccountType type)
    {
        var account = new FinancialAccount
        {
            BusinessId = businessId,
            Code = type.ToString().ToUpperInvariant(),
            Name = type.ToString(),
            Type = type
        };
        db.Set<FinancialAccount>().Add(account);
        return account;
    }

    private static BusinessEventService Events(BankTreasuryTestDbContext db) => new(db, Clock());
    private static FixedClock Clock() => new(Now);

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class BankTreasuryTestDbContext : DbContext, IAppDbContext
    {
        private BankTreasuryTestDbContext(DbContextOptions<BankTreasuryTestDbContext> options) : base(options) { }
        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BankTreasuryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BankTreasuryTestDbContext>()
                .UseInMemoryDatabase($"darwin_bank_treasury_tests_{Guid.NewGuid()}")
                .Options;
            return new BankTreasuryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BankAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementImport>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankStatementImportId);
            });
            modelBuilder.Entity<BankStatementLine>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId);
            });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
            modelBuilder.Entity<SupplierPayment>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.SupplierPaymentId);
            });
            modelBuilder.Entity<SupplierPaymentAllocation>().HasKey(x => x.Id);
            modelBuilder.Entity<Payment>().HasKey(x => x.Id);
            modelBuilder.Entity<BusinessEvent>().HasKey(x => x.Id);
            modelBuilder.Entity<AuditTrail>().HasKey(x => x.Id);
        }
    }
}
