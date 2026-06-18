using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.HumanResources.Commands;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.HumanResources;

public sealed class PayrollPaymentHandlersTests
{
    private static readonly DateTime Now = new(2034, 4, 5, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PayrollPayment_BankSettlement_Should_CreateBalancedClearingPosting()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var settlement = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000);
        seed.Payment.RowVersion = [12];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [12],
            BankReconciliationMatchId = settlement.MatchId,
            Notes = "Payroll statement match"
        }, TestContext.Current.CancellationToken);

        var payment = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == seed.Payment.Id, TestContext.Current.CancellationToken);
        payment.BankSettledAtUtc.Should().Be(Now);
        payment.BankSettlementReconciliationMatchId.Should().Be(settlement.MatchId);
        payment.BankSettlementJournalEntryId.Should().NotBeNull();
        payment.BankSettlementNotes.Should().Be("Payroll statement match");
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.PayrollPaymentBankSettled, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{SettlePayrollPaymentFromBankReconciliationHandler.PostingKeyPrefix}:{seed.Payment.Id}");
        entry.SourceEntityType.Should().Be("PayrollPayment");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(230000);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(230000);
        entry.Lines.Should().Contain(x => x.DebitMinor == 230000 && x.Memo == "Cash clearing release");
        entry.Lines.Should().Contain(x => x.CreditMinor == 230000 && x.AccountId == settlement.BankFinancialAccountId && x.Memo == "Bank account settlement");
    }

    [Fact]
    public async Task PayrollPayment_BankSettlement_Should_RejectPartialOrUnmappedReconciliation()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var partial = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 100000);
        seed.Payment.RowVersion = [21];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var partialAct = async () => await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [21],
            BankReconciliationMatchId = partial.MatchId
        }, TestContext.Current.CancellationToken);

        await partialAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresFullAmount*");

        var unmapped = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000, mapBankAccount: false, identitySuffix: "unmapped");
        seed.Payment.RowVersion = [22];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unmappedAct = async () => await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [22],
            BankReconciliationMatchId = unmapped.MatchId
        }, TestContext.Current.CancellationToken);

        await unmappedAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresMappedBankAccount*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.PayrollPaymentBankSettled).Should().Be(0);
    }

    [Fact]
    public async Task PayrollPayment_BankSettlement_Should_BlockRetryAndSimpleReversalAfterSettlement()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.PayrollPayable, AccountType.Liability);
        var settlement = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000);
        seed.Payment.RowVersion = [31];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [31],
            BankReconciliationMatchId = settlement.MatchId
        }, TestContext.Current.CancellationToken);
        var settled = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == seed.Payment.Id, TestContext.Current.CancellationToken);
        settled.RowVersion = [32];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retry = async () => await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [32],
            BankReconciliationMatchId = settlement.MatchId
        }, TestContext.Current.CancellationToken);
        await retry.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PayrollPaymentLifecycleUnsupportedAction*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.PayrollPaymentBankSettled && x.SourceEntityId == seed.Payment.Id).Should().Be(1);

        var reverse = async () => await CreateReverseHandler(db).HandleAsync(new PayrollPaymentLifecycleActionDto
        {
            Id = seed.Payment.Id,
            RowVersion = [32],
            Reason = "Returned after bank settlement"
        }, TestContext.Current.CancellationToken);
        await reverse.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PayrollPaymentLifecycleUnsupportedAction*");
    }

    [Fact]
    public async Task PayrollPaymentBankCorrection_ReturnedTransfer_Should_CreateDraftAndPostBalancedCorrection()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var settlement = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000);
        seed.Payment.RowVersion = [41];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto { Id = seed.Payment.Id, RowVersion = [41], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        var settled = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == seed.Payment.Id, TestContext.Current.CancellationToken);
        settled.RowVersion = [42];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var correctionEvidence = await SeedBankCorrectionReconciliationAsync(db, seed.BusinessId, settled, 230000);

        var correctionId = await CreateCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionCreateDto
        {
            PayrollPaymentId = settled.Id,
            PayrollPaymentRowVersion = [42],
            CorrectionType = PayrollPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = correctionEvidence.MatchId,
            Reason = "Salary transfer returned by bank"
        }, TestContext.Current.CancellationToken);

        var correction = await db.Set<PayrollPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        correction.Status.Should().Be(PayrollPaymentBankCorrectionStatus.Draft);
        correction.AmountMinor.Should().Be(230000);
        correction.RowVersion = [43];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await PostCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionActionDto
        {
            Id = correctionId,
            RowVersion = [43]
        }, TestContext.Current.CancellationToken);

        var posted = await db.Set<PayrollPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        posted.Status.Should().Be(PayrollPaymentBankCorrectionStatus.Posted);
        posted.CorrectionJournalEntryId.Should().NotBeNull();
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.PayrollPaymentBankCorrection, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{PostPayrollPaymentBankCorrectionHandler.PostingKeyPrefix}:{correctionId}");
        entry.SourceEntityType.Should().Be("PayrollPaymentBankCorrection");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(230000);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(230000);
        entry.Lines.Should().Contain(x => x.AccountId == correctionEvidence.BankFinancialAccountId && x.DebitMinor == 230000);
        entry.Lines.Should().Contain(x => x.CreditMinor == 230000 && x.Memo == "Cash clearing reinstatement");
        var payment = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == settled.Id, TestContext.Current.CancellationToken);
        payment.Status.Should().Be(PayrollPaymentStatus.Posted);
        payment.BankSettlementJournalEntryId.Should().NotBeNull();
    }

    [Fact]
    public async Task PayrollPaymentBankCorrection_DuplicatePayment_Should_BeAttentionOnlyAndNotPost()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var settlement = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000);
        seed.Payment.RowVersion = [51];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto { Id = seed.Payment.Id, RowVersion = [51], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        var settled = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == seed.Payment.Id, TestContext.Current.CancellationToken);
        settled.RowVersion = [52];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var duplicateEvidence = await SeedBankCorrectionReconciliationAsync(db, seed.BusinessId, settled, 230000, "duplicate");

        var correctionId = await CreateCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionCreateDto
        {
            PayrollPaymentId = settled.Id,
            PayrollPaymentRowVersion = [52],
            CorrectionType = PayrollPaymentBankCorrectionType.DuplicatePayment,
            BankReconciliationMatchId = duplicateEvidence.MatchId,
            Reason = "Duplicate salary movement"
        }, TestContext.Current.CancellationToken);

        var correction = await db.Set<PayrollPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        correction.CorrectionType.Should().Be(PayrollPaymentBankCorrectionType.DuplicatePayment);
        correction.RowVersion = [53];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var post = async () => await PostCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionActionDto
        {
            Id = correctionId,
            RowVersion = [53]
        }, TestContext.Current.CancellationToken);

        await post.Should().ThrowAsync<InvalidOperationException>().WithMessage("*AttentionOnly*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.PayrollPaymentBankCorrection).Should().Be(0);
    }

    [Fact]
    public async Task PayrollPaymentBankCorrection_Should_RejectSensitiveReasonAndPartialEvidence()
    {
        await using var db = PayrollPaymentTestDbContext.Create();
        var seed = await SeedPostedPayrollPaymentAsync(db, totalAmountMinor: 230000);
        SeedMappedAccount(db, seed.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var settlement = await SeedBankSettlementReconciliationAsync(db, seed.BusinessId, seed.Payment, 230000);
        seed.Payment.RowVersion = [61];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleHandler(db).HandleAsync(new PayrollPaymentBankSettlementActionDto { Id = seed.Payment.Id, RowVersion = [61], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        var settled = await db.Set<PayrollPayment>().SingleAsync(x => x.Id == seed.Payment.Id, TestContext.Current.CancellationToken);
        settled.RowVersion = [62];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var partialEvidence = await SeedBankCorrectionReconciliationAsync(db, seed.BusinessId, settled, 100000, "partial");

        var sensitive = async () => await CreateCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionCreateDto
        {
            PayrollPaymentId = settled.Id,
            PayrollPaymentRowVersion = [62],
            CorrectionType = PayrollPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = partialEvidence.MatchId,
            Reason = "token=secret"
        }, TestContext.Current.CancellationToken);
        await sensitive.Should().ThrowAsync<ArgumentException>();

        var partial = async () => await CreateCorrectionHandler(db).HandleAsync(new PayrollPaymentBankCorrectionCreateDto
        {
            PayrollPaymentId = settled.Id,
            PayrollPaymentRowVersion = [62],
            CorrectionType = PayrollPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = partialEvidence.MatchId,
            Reason = "Partial return"
        }, TestContext.Current.CancellationToken);
        await partial.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresFullSettlementEvidence*");
    }

    private static SettlePayrollPaymentFromBankReconciliationHandler CreateSettleHandler(IAppDbContext db)
        => new(db, new FixedClock(Now), new PayrollPaymentWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static ReversePayrollPaymentHandler CreateReverseHandler(IAppDbContext db)
        => new(db, new FixedClock(Now), new PayrollPaymentWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static CreatePayrollPaymentBankCorrectionHandler CreateCorrectionHandler(IAppDbContext db)
        => new(db, new FixedClock(Now));

    private static PostPayrollPaymentBankCorrectionHandler PostCorrectionHandler(IAppDbContext db)
        => new(db, new FixedClock(Now), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static async Task<PayrollSeed> SeedPostedPayrollPaymentAsync(PayrollPaymentTestDbContext db, long totalAmountMinor)
    {
        var businessId = Guid.NewGuid();
        var payrollRunId = Guid.NewGuid();
        var runLineId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var postingJournalId = Guid.NewGuid();
        db.Set<PayrollRun>().Add(new PayrollRun
        {
            Id = payrollRunId,
            BusinessId = businessId,
            PayrollPeriodId = Guid.NewGuid(),
            PayrollRuleSetId = Guid.NewGuid(),
            RunNumber = "PR-1",
            Status = PayrollRunStatus.Posted,
            Currency = "EUR",
            PeriodStartUtc = Now.Date.AddDays(-30),
            PeriodEndUtc = Now.Date,
            NetPayMinor = totalAmountMinor,
            PostingJournalEntryId = postingJournalId,
            MetadataJson = "{}"
        });
        db.Set<PayrollRunLine>().Add(new PayrollRunLine
        {
            Id = runLineId,
            BusinessId = businessId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            EmployeeNumber = "E-1",
            EmployeeName = "Ada Lovelace",
            NetPayMinor = totalAmountMinor
        });
        var payment = new PayrollPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollRunId = payrollRunId,
            Status = PayrollPaymentStatus.Posted,
            PaymentMethod = PayrollPaymentMethod.BankTransfer,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = totalAmountMinor,
            PostingJournalEntryId = postingJournalId,
            PostedAtUtc = Now,
            RowVersion = [1],
            MetadataJson = "{}",
            Allocations =
            [
                new PayrollPaymentAllocation
                {
                    BusinessId = businessId,
                    PayrollRunId = payrollRunId,
                    PayrollRunLineId = runLineId,
                    EmployeeId = employeeId,
                    AmountMinor = totalAmountMinor
                }
            ]
        };
        db.Set<PayrollPayment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new PayrollSeed(businessId, payment);
    }

    private static void SeedMappedAccount(PayrollPaymentTestDbContext db, Guid businessId, FinancePostingAccountRole role, AccountType accountType)
    {
        var accountId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Id = accountId,
            BusinessId = businessId,
            Code = $"{role}-1",
            Name = role.ToString(),
            Type = accountType
        });
        db.Set<FinancePostingAccountMapping>().Add(new FinancePostingAccountMapping
        {
            BusinessId = businessId,
            Role = role,
            FinancialAccountId = accountId,
            IsActive = true
        });
    }

    private static async Task<BankSettlementSeed> SeedBankSettlementReconciliationAsync(
        PayrollPaymentTestDbContext db,
        Guid businessId,
        PayrollPayment payment,
        long amountMinor,
        bool mapBankAccount = true,
        string identitySuffix = "default")
    {
        Guid? bankFinancialAccountId = null;
        if (mapBankAccount)
        {
            bankFinancialAccountId = Guid.NewGuid();
            db.Set<FinancialAccount>().Add(new FinancialAccount
            {
                Id = bankFinancialAccountId.Value,
                BusinessId = businessId,
                Code = $"BANK-{identitySuffix}",
                Name = $"Bank {identitySuffix}",
                Type = AccountType.Asset
            });
        }

        var bankAccountId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var statementLineId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        db.Set<BankAccount>().Add(new BankAccount
        {
            Id = bankAccountId,
            BusinessId = businessId,
            FinancialAccountId = bankFinancialAccountId,
            Code = $"BANK-{identitySuffix}",
            DisplayName = $"Bank {identitySuffix}",
            Currency = payment.Currency,
            Status = BankAccountStatus.Active,
            MetadataJson = "{}"
        });
        db.Set<BankStatementImport>().Add(new BankStatementImport
        {
            Id = importId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            StatementReference = $"ST-{identitySuffix}",
            PeriodStartUtc = Now.Date.AddDays(-1),
            PeriodEndUtc = Now.Date,
            ImportedAtUtc = Now,
            Status = BankStatementImportStatus.Imported,
            LineCount = 1,
            DebitTotalMinor = amountMinor,
            MetadataJson = "{}"
        });
        db.Set<BankStatementLine>().Add(new BankStatementLine
        {
            Id = statementLineId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            BankStatementImportId = importId,
            TransactionDateUtc = Now,
            Direction = BankStatementLineDirection.Debit,
            AmountMinor = amountMinor,
            Currency = payment.Currency,
            NormalizedIdentityKey = $"PAYROLL-{identitySuffix}",
            MetadataJson = "{}"
        });
        db.Set<BankReconciliationMatch>().Add(new BankReconciliationMatch
        {
            Id = matchId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            MatchNumber = $"BR-{identitySuffix}",
            Status = BankReconciliationMatchStatus.Matched,
            MatchDateUtc = Now,
            MatchedAtUtc = Now,
            Currency = payment.Currency,
            BankTotalMinor = amountMinor,
            FinanceTotalMinor = amountMinor,
            DifferenceMinor = 0,
            MetadataJson = "{}",
            Lines =
            [
                new BankReconciliationMatchLine
                {
                    BankStatementLineId = statementLineId,
                    JournalEntryId = payment.PostingJournalEntryId,
                    SourceType = BankReconciliationSourceType.JournalEntry,
                    SourceEntityType = "PayrollPayment",
                    SourceEntityId = payment.Id,
                    Direction = BankStatementLineDirection.Debit,
                    AmountMinor = amountMinor,
                    IsActive = true
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new BankSettlementSeed(matchId, bankFinancialAccountId);
    }

    private static async Task<BankSettlementSeed> SeedBankCorrectionReconciliationAsync(
        PayrollPaymentTestDbContext db,
        Guid businessId,
        PayrollPayment payment,
        long amountMinor,
        string identitySuffix = "correction")
    {
        var bankFinancialAccountId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Id = bankFinancialAccountId,
            BusinessId = businessId,
            Code = $"BANK-CORR-{identitySuffix}",
            Name = $"Correction Bank {identitySuffix}",
            Type = AccountType.Asset
        });

        var bankAccountId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var statementLineId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        db.Set<BankAccount>().Add(new BankAccount
        {
            Id = bankAccountId,
            BusinessId = businessId,
            FinancialAccountId = bankFinancialAccountId,
            Code = $"BANK-CORR-{identitySuffix}",
            DisplayName = $"Correction Bank {identitySuffix}",
            Currency = payment.Currency,
            Status = BankAccountStatus.Active,
            MetadataJson = "{}"
        });
        db.Set<BankStatementImport>().Add(new BankStatementImport
        {
            Id = importId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            StatementReference = $"ST-CORR-{identitySuffix}",
            PeriodStartUtc = Now.Date,
            PeriodEndUtc = Now.Date.AddDays(1),
            ImportedAtUtc = Now,
            Status = BankStatementImportStatus.Imported,
            LineCount = 1,
            CreditTotalMinor = amountMinor,
            MetadataJson = "{}"
        });
        db.Set<BankStatementLine>().Add(new BankStatementLine
        {
            Id = statementLineId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            BankStatementImportId = importId,
            TransactionDateUtc = Now,
            Direction = BankStatementLineDirection.Credit,
            AmountMinor = amountMinor,
            Currency = payment.Currency,
            NormalizedIdentityKey = $"PAYROLL-CORR-{identitySuffix}",
            MetadataJson = "{}"
        });
        db.Set<BankReconciliationMatch>().Add(new BankReconciliationMatch
        {
            Id = matchId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            MatchNumber = $"BR-CORR-{identitySuffix}",
            Status = BankReconciliationMatchStatus.Matched,
            MatchDateUtc = Now,
            MatchedAtUtc = Now,
            Currency = payment.Currency,
            BankTotalMinor = amountMinor,
            FinanceTotalMinor = amountMinor,
            DifferenceMinor = 0,
            MetadataJson = "{}",
            Lines =
            [
                new BankReconciliationMatchLine
                {
                    BankStatementLineId = statementLineId,
                    JournalEntryId = payment.BankSettlementJournalEntryId,
                    SourceType = BankReconciliationSourceType.JournalEntry,
                    SourceEntityType = "PayrollPayment",
                    SourceEntityId = payment.Id,
                    Direction = BankStatementLineDirection.Credit,
                    AmountMinor = amountMinor,
                    IsActive = true
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new BankSettlementSeed(matchId, bankFinancialAccountId);
    }

    private sealed record PayrollSeed(Guid BusinessId, PayrollPayment Payment);
    private sealed record BankSettlementSeed(Guid MatchId, Guid? BankFinancialAccountId);

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class PayrollPaymentTestDbContext : DbContext, IAppDbContext
    {
        private PayrollPaymentTestDbContext(DbContextOptions<PayrollPaymentTestDbContext> options) : base(options) { }
        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PayrollPaymentTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PayrollPaymentTestDbContext>()
                .UseInMemoryDatabase($"darwin_payroll_payment_tests_{Guid.NewGuid()}")
                .Options;
            return new PayrollPaymentTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PayrollRun>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PayrollRunId); });
            modelBuilder.Entity<PayrollRunLine>().HasKey(x => x.Id);
            modelBuilder.Entity<PayrollPayment>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.PayrollPaymentId); });
            modelBuilder.Entity<PayrollPaymentAllocation>().HasKey(x => x.Id);
            modelBuilder.Entity<PayrollPaymentBankCorrection>().HasKey(x => x.Id);
            modelBuilder.Entity<BankAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementImport>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementLine>().HasKey(x => x.Id);
            modelBuilder.Entity<BankReconciliationMatch>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankReconciliationMatchId); });
            modelBuilder.Entity<BankReconciliationMatchLine>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId); });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancePostingAccountMapping>().HasKey(x => x.Id);
        }
    }
}
