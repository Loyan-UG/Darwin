using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceReceivablesPostingServiceTests
{
    private static readonly DateTime FixedNow = new(2032, 4, 5, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PostInvoicePaymentRefundAndCancellation_Should_ReconcileReceivableToZero()
    {
        await using var db = FinanceReceivablesPostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedMappedAccount(db, businessId, FinancePostingAccountRole.Receivables, AccountType.Asset);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.SalesRevenue, AccountType.Revenue);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.TaxPayable, AccountType.Liability);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceNumber = "INV-9001",
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 10000,
            TotalTaxMinor = 1900,
            TotalGrossMinor = 11900,
            DueDateUtc = FixedNow.AddDays(14)
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceId = invoice.Id,
            AmountMinor = 11900,
            Currency = "EUR",
            Status = PaymentStatus.Completed,
            PaidAtUtc = FixedNow
        };
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            AmountMinor = 11900,
            Currency = "EUR",
            Status = RefundStatus.Completed,
            CompletedAtUtc = FixedNow.AddHours(1)
        };
        var service = CreateService(db);

        var issued = await service.PostInvoiceIssuedAsync(invoice, FixedNow, TestContext.Current.CancellationToken);
        var paid = await service.PostPaymentRecordedAsync(payment, FixedNow, TestContext.Current.CancellationToken);
        var refunded = await service.PostRefundRecordedAsync(refund, payment, FixedNow.AddHours(1), TestContext.Current.CancellationToken);
        invoice.Status = InvoiceStatus.Cancelled;
        var cancelled = await service.PostInvoiceCancelledAsync(invoice, FixedNow.AddHours(1), TestContext.Current.CancellationToken);
        invoice.Status = InvoiceStatus.Open;
        var duplicateIssued = await service.PostInvoiceIssuedAsync(invoice, FixedNow, TestContext.Current.CancellationToken);

        issued.Succeeded.Should().BeTrue();
        paid.Succeeded.Should().BeTrue();
        refunded.Succeeded.Should().BeTrue();
        cancelled.Succeeded.Should().BeTrue();
        duplicateIssued.Succeeded.Should().BeTrue();
        duplicateIssued.Value!.Created.Should().BeFalse();

        var entries = await db.Set<JournalEntry>()
            .Include(x => x.Lines)
            .OrderBy(x => x.PostingKind)
            .ToListAsync(TestContext.Current.CancellationToken);

        entries.Should().HaveCount(4);
        entries.Select(x => x.PostingKind).Should().BeEquivalentTo(new[]
        {
            JournalEntryPostingKind.InvoiceIssued,
            JournalEntryPostingKind.PaymentRecorded,
            JournalEntryPostingKind.RefundRecorded,
            JournalEntryPostingKind.Reversal
        });

        var receivablesBalance = entries
            .SelectMany(x => x.Lines)
            .Where(x => x.AccountId == receivablesAccountId)
            .Sum(x => x.DebitMinor - x.CreditMinor);
        receivablesBalance.Should().Be(0);
    }

    [Fact]
    public async Task PostInvoiceIssuedAsync_Should_FailClosed_WhenRequiredMappingIsMissing()
    {
        await using var db = FinanceReceivablesPostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.Receivables, AccountType.Asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            TotalNetMinor = 10000,
            TotalTaxMinor = 1900,
            TotalGrossMinor = 11900,
            DueDateUtc = FixedNow.AddDays(14)
        };
        var service = CreateService(db);

        var result = await service.PostInvoiceIssuedAsync(invoice, FixedNow, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain(nameof(FinancePostingAccountRole.SalesRevenue));
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task PostPaymentRecordedAsync_Should_NoOp_WhenBusinessScopeIsMissing()
    {
        await using var db = FinanceReceivablesPostingTestDbContext.Create();
        var service = CreateService(db);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AmountMinor = 2500,
            Currency = "EUR",
            Status = PaymentStatus.Completed,
            PaidAtUtc = FixedNow
        };

        var result = await service.PostPaymentRecordedAsync(payment, FixedNow, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeNull();
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task PostCreditNoteIssuedAndVoidedAsync_Should_Post_Balanced_Reversal_Entries()
    {
        await using var db = FinanceReceivablesPostingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedMappedAccount(db, businessId, FinancePostingAccountRole.Receivables, AccountType.Asset);
        var revenueAccountId = SeedMappedAccount(db, businessId, FinancePostingAccountRole.SalesRevenue, AccountType.Revenue);
        var taxAccountId = SeedMappedAccount(db, businessId, FinancePostingAccountRole.TaxPayable, AccountType.Liability);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceId = Guid.NewGuid(),
            CreditNoteNumber = "CN-1001",
            Status = CreditNoteStatus.Issued,
            Currency = "EUR",
            TotalNetMinor = 10000,
            TotalTaxMinor = 1900,
            TotalGrossMinor = 11900
        };
        var service = CreateService(db);

        var issued = await service.PostCreditNoteIssuedAsync(creditNote, FixedNow, TestContext.Current.CancellationToken);
        creditNote.Status = CreditNoteStatus.Voided;
        var voided = await service.PostCreditNoteVoidedAsync(creditNote, FixedNow.AddHours(1), TestContext.Current.CancellationToken);

        issued.Succeeded.Should().BeTrue();
        issued.Value!.Created.Should().BeTrue();
        voided.Succeeded.Should().BeTrue();
        voided.Value!.Created.Should().BeTrue();

        var entries = await db.Set<JournalEntry>()
            .Include(x => x.Lines)
            .OrderBy(x => x.EntryDateUtc)
            .ToListAsync(TestContext.Current.CancellationToken);
        entries.Should().HaveCount(2);
        entries[0].PostingKind.Should().Be(JournalEntryPostingKind.CreditNoteIssued);
        entries[1].PostingKind.Should().Be(JournalEntryPostingKind.Reversal);

        entries[0].Lines.Single(x => x.AccountId == revenueAccountId).DebitMinor.Should().Be(10000);
        entries[0].Lines.Single(x => x.AccountId == taxAccountId).DebitMinor.Should().Be(1900);
        entries[0].Lines.Single(x => x.AccountId == receivablesAccountId).CreditMinor.Should().Be(11900);
        entries[1].Lines.Single(x => x.AccountId == receivablesAccountId).DebitMinor.Should().Be(11900);
    }

    private static FinanceReceivablesPostingService CreateService(IAppDbContext db)
        => new(
            new FinanceAccountMappingService(db),
            new FinancePostingService(db, new FixedClock(FixedNow)));

    private static Guid SeedMappedAccount(
        FinanceReceivablesPostingTestDbContext db,
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
            IsActive = true
        });
        return account.Id;
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class FinanceReceivablesPostingTestDbContext : DbContext, IAppDbContext
    {
        private FinanceReceivablesPostingTestDbContext(DbContextOptions<FinanceReceivablesPostingTestDbContext> options)
            : base(options)
        {
        }

        public static FinanceReceivablesPostingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceReceivablesPostingTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_receivables_posting_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceReceivablesPostingTestDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<FinancePostingAccountMapping>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.MetadataJson).IsRequired();
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
