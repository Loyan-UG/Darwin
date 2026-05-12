using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for financial query handlers:
/// <see cref="GetFinancialAccountsPageHandler"/>,
/// <see cref="GetFinancialAccountForEditHandler"/>,
/// <see cref="GetExpensesPageHandler"/>,
/// <see cref="GetExpenseForEditHandler"/>,
/// <see cref="GetJournalEntriesPageHandler"/>, and
/// <see cref="GetJournalEntryForEditHandler"/>.
/// </summary>
public sealed class BillingFinancialQueryHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static readonly Guid DefaultBusinessId = Guid.NewGuid();
    private static readonly DateTime FixedNow = new(2030, 8, 1, 9, 0, 0, DateTimeKind.Utc);

    private static FinancialAccount MakeAccount(
        Guid? businessId = null,
        string name = "Cash",
        AccountType type = AccountType.Asset,
        string? code = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId ?? DefaultBusinessId,
            Name = name,
            Type = type,
            Code = code,
            IsDeleted = isDeleted
        };

    private static Expense MakeExpense(
        Guid? businessId = null,
        string category = "Office Supplies",
        string description = "Pens and notebooks",
        long amountMinor = 5_00L,
        bool isDeleted = false,
        Guid? supplierId = null,
        DateTime? expenseDateUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId ?? DefaultBusinessId,
            Category = category,
            Description = description,
            AmountMinor = amountMinor,
            SupplierId = supplierId,
            ExpenseDateUtc = expenseDateUtc ?? FixedNow.AddDays(-3),
            IsDeleted = isDeleted
        };

    private static JournalEntry MakeJournalEntry(
        Guid? businessId = null,
        string description = "Test entry",
        bool isDeleted = false,
        DateTime? entryDateUtc = null,
        List<JournalEntryLine>? lines = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId ?? DefaultBusinessId,
            Description = description,
            EntryDateUtc = entryDateUtc ?? FixedNow.AddDays(-1),
            IsDeleted = isDeleted,
            Lines = lines ?? new List<JournalEntryLine>()
        };

    private static JournalEntryLine MakeLine(
        Guid journalEntryId,
        long debitMinor = 1000L,
        long creditMinor = 0L,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            AccountId = Guid.NewGuid(),
            DebitMinor = debitMinor,
            CreditMinor = creditMinor,
            IsDeleted = isDeleted
        };

    private static FinancialQueryTestDbContext CreateDb() =>
        FinancialQueryTestDbContext.Create();

    private static GetFinancialAccountsPageHandler CreateAccountsPageHandler(
        FinancialQueryTestDbContext db) => new(db);

    private static GetFinancialAccountForEditHandler CreateAccountForEditHandler(
        FinancialQueryTestDbContext db) => new(db);

    private static GetExpensesPageHandler CreateExpensesPageHandler(
        FinancialQueryTestDbContext db) =>
        new(db, new FinancialQueryFixedClock(FixedNow));

    private static GetExpenseForEditHandler CreateExpenseForEditHandler(
        FinancialQueryTestDbContext db) => new(db);

    private static GetJournalEntriesPageHandler CreateJournalEntriesPageHandler(
        FinancialQueryTestDbContext db) =>
        new(db, new FinancialQueryFixedClock(FixedNow));

    private static GetJournalEntryForEditHandler CreateJournalEntryForEditHandler(
        FinancialQueryTestDbContext db) => new(db);

    // ═══════════════════════════════════════════════════════════════════════
    // GetFinancialAccountsPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFinancialAccountsPage_Should_ReturnEmpty_WhenNoAccountsExist()
    {
        await using var db = CreateDb();
        var handler = CreateAccountsPageHandler(db);

        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetFinancialAccountsPage_Should_ExcludeSoftDeletedAccounts()
    {
        await using var db = CreateDb();
        db.Set<FinancialAccount>().AddRange(
            MakeAccount(name: "Cash"),
            MakeAccount(name: "Deleted Account", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.Single().Name.Should().Be("Cash");
    }

    [Fact]
    public async Task GetFinancialAccountsPage_Should_ExcludeOtherBusinessAccounts()
    {
        await using var db = CreateDb();
        db.Set<FinancialAccount>().AddRange(
            MakeAccount(businessId: DefaultBusinessId),
            MakeAccount(businessId: Guid.NewGuid()));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetFinancialAccountsPage_Should_FilterByType()
    {
        await using var db = CreateDb();
        db.Set<FinancialAccount>().AddRange(
            MakeAccount(name: "Cash", type: AccountType.Asset),
            MakeAccount(name: "Revenue", type: AccountType.Revenue),
            MakeAccount(name: "Expense Acc", type: AccountType.Expense));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, type: AccountType.Asset,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Type.Should().Be(AccountType.Asset);
    }

    [Fact]
    public async Task GetFinancialAccountsPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = CreateDb();
        db.Set<FinancialAccount>().Add(MakeAccount());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountsPageHandler(db);

        var (items0, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);
        items0.Should().HaveCount(1, "page < 1 clamped to 1");

        var (items1, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 1, pageSize: 0, ct: TestContext.Current.CancellationToken);
        items1.Should().HaveCount(1, "pageSize < 1 clamped to 20");
    }

    [Fact]
    public async Task GetFinancialAccountsPage_GetSummary_Should_ReturnCorrectCounts()
    {
        await using var db = CreateDb();
        db.Set<FinancialAccount>().AddRange(
            MakeAccount(name: "Cash", type: AccountType.Asset),
            MakeAccount(name: "Bank", type: AccountType.Asset, code: "1000"),
            MakeAccount(name: "Sales Revenue", type: AccountType.Revenue, code: "4000"),
            MakeAccount(name: "Office Expense", type: AccountType.Expense),
            MakeAccount(name: "Deleted", type: AccountType.Asset, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountsPageHandler(db);
        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(4, "soft-deleted excluded");
        summary.AssetCount.Should().Be(2);
        summary.RevenueCount.Should().Be(1);
        summary.ExpenseCount.Should().Be(1);
        summary.MissingCodeCount.Should().Be(2, "Cash and Office Expense have no code");
    }

    [Fact]
    public async Task GetFinancialAccountsPage_GetSummary_Should_ReturnZeroCounts_WhenEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateAccountsPageHandler(db);

        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.AssetCount.Should().Be(0);
        summary.RevenueCount.Should().Be(0);
        summary.ExpenseCount.Should().Be(0);
        summary.MissingCodeCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetFinancialAccountForEditHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFinancialAccountForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = CreateDb();
        var handler = CreateAccountForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFinancialAccountForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = CreateDb();
        var account = MakeAccount(isDeleted: true);
        db.Set<FinancialAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountForEditHandler(db);
        var result = await handler.HandleAsync(account.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFinancialAccountForEdit_Should_ReturnCorrectProjection()
    {
        await using var db = CreateDb();
        var account = MakeAccount(name: "Accounts Receivable", type: AccountType.Asset, code: "1100");
        db.Set<FinancialAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAccountForEditHandler(db);
        var result = await handler.HandleAsync(account.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.BusinessId.Should().Be(DefaultBusinessId);
        result.Name.Should().Be("Accounts Receivable");
        result.Type.Should().Be(AccountType.Asset);
        result.Code.Should().Be("1100");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetExpensesPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExpensesPage_Should_ReturnEmpty_WhenNoExpensesExist()
    {
        await using var db = CreateDb();
        var handler = CreateExpensesPageHandler(db);

        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetExpensesPage_Should_ExcludeSoftDeletedExpenses()
    {
        await using var db = CreateDb();
        db.Set<Expense>().AddRange(
            MakeExpense(),
            MakeExpense(isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpensesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetExpensesPage_Should_ExcludeOtherBusinessExpenses()
    {
        await using var db = CreateDb();
        db.Set<Expense>().AddRange(
            MakeExpense(businessId: DefaultBusinessId),
            MakeExpense(businessId: Guid.NewGuid()));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpensesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetExpensesPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = CreateDb();
        db.Set<Expense>().Add(MakeExpense());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpensesPageHandler(db);

        var (items0, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 0, pageSize: 20, ct: TestContext.Current.CancellationToken);
        items0.Should().HaveCount(1, "page < 1 clamped to 1");

        var (items1, _) = await handler.HandleAsync(
            DefaultBusinessId, page: 1, pageSize: 0, ct: TestContext.Current.CancellationToken);
        items1.Should().HaveCount(1, "pageSize < 1 clamped to 20");
    }

    [Fact]
    public async Task GetExpensesPage_GetSummary_Should_ReturnCorrectCounts()
    {
        await using var db = CreateDb();
        var supplierId = Guid.NewGuid();
        db.Set<Expense>().AddRange(
            MakeExpense(amountMinor: 5_00L, expenseDateUtc: FixedNow.AddDays(-1)),
            MakeExpense(amountMinor: 200_00L, supplierId: supplierId,
                expenseDateUtc: FixedNow.AddDays(-20)),
            MakeExpense(amountMinor: 1_00L, expenseDateUtc: FixedNow.AddDays(-40)),
            MakeExpense(isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpensesPageHandler(db);
        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3, "soft-deleted excluded");
        summary.SupplierLinkedCount.Should().Be(1, "one has SupplierId set");
        summary.RecentCount.Should().Be(2, "two expenses within last 30 days");
        summary.HighValueCount.Should().Be(1, "one expense >= 100.00 (10000 minor)");
    }

    [Fact]
    public async Task GetExpensesPage_GetSummary_Should_ReturnZeroCounts_WhenEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateExpensesPageHandler(db);

        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.SupplierLinkedCount.Should().Be(0);
        summary.RecentCount.Should().Be(0);
        summary.HighValueCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetExpenseForEditHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExpenseForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = CreateDb();
        var handler = CreateExpenseForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpenseForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = CreateDb();
        var expense = MakeExpense(isDeleted: true);
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpenseForEditHandler(db);
        var result = await handler.HandleAsync(expense.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpenseForEdit_Should_ReturnCorrectProjection()
    {
        await using var db = CreateDb();
        var supplierId = Guid.NewGuid();
        var expense = MakeExpense(
            category: "Travel",
            description: "Conference ticket",
            amountMinor: 45_000L,
            supplierId: supplierId,
            expenseDateUtc: FixedNow.AddDays(-10));
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateExpenseForEditHandler(db);
        var result = await handler.HandleAsync(expense.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(expense.Id);
        result.BusinessId.Should().Be(DefaultBusinessId);
        result.SupplierId.Should().Be(supplierId);
        result.Category.Should().Be("Travel");
        result.Description.Should().Be("Conference ticket");
        result.AmountMinor.Should().Be(45_000L);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetJournalEntriesPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJournalEntriesPage_Should_ReturnEmpty_WhenNoEntriesExist()
    {
        await using var db = CreateDb();
        var handler = CreateJournalEntriesPageHandler(db);

        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetJournalEntriesPage_Should_ExcludeSoftDeletedEntries()
    {
        await using var db = CreateDb();
        db.Set<JournalEntry>().AddRange(
            MakeJournalEntry(description: "Active Entry"),
            MakeJournalEntry(description: "Deleted Entry", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.Single().Description.Should().Be("Active Entry");
    }

    [Fact]
    public async Task GetJournalEntriesPage_Should_ExcludeOtherBusinessEntries()
    {
        await using var db = CreateDb();
        db.Set<JournalEntry>().AddRange(
            MakeJournalEntry(businessId: DefaultBusinessId),
            MakeJournalEntry(businessId: Guid.NewGuid()));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetJournalEntriesPage_Should_FilterRecent_ByLast7Days()
    {
        await using var db = CreateDb();
        db.Set<JournalEntry>().AddRange(
            MakeJournalEntry(description: "Recent", entryDateUtc: FixedNow.AddDays(-3)),
            MakeJournalEntry(description: "Old", entryDateUtc: FixedNow.AddDays(-14)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: JournalEntryQueueFilter.Recent,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "Recent filter returns only entries within last 7 days");
        items.Single().Description.Should().Be("Recent");
    }

    [Fact]
    public async Task GetJournalEntriesPage_Should_FilterMultiLine_MoreThan2Lines()
    {
        await using var db = CreateDb();

        var entryId1 = Guid.NewGuid();
        var entryId2 = Guid.NewGuid();
        db.Set<JournalEntry>().AddRange(
            new JournalEntry
            {
                Id = entryId1,
                BusinessId = DefaultBusinessId,
                Description = "Single line",
                EntryDateUtc = FixedNow.AddDays(-1),
                Lines = new List<JournalEntryLine>
                {
                    MakeLine(entryId1),
                    MakeLine(entryId1)
                }
            },
            new JournalEntry
            {
                Id = entryId2,
                BusinessId = DefaultBusinessId,
                Description = "Multi-line",
                EntryDateUtc = FixedNow.AddDays(-1),
                Lines = new List<JournalEntryLine>
                {
                    MakeLine(entryId2),
                    MakeLine(entryId2),
                    MakeLine(entryId2)
                }
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20,
            filter: JournalEntryQueueFilter.MultiLine,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "MultiLine filter returns entries with more than 2 non-deleted lines");
        items.Single().Description.Should().Be("Multi-line");
    }

    [Fact]
    public async Task GetJournalEntriesPage_Should_ProjectLineCounts_ExcludingDeletedLines()
    {
        await using var db = CreateDb();
        var entryId = Guid.NewGuid();
        db.Set<JournalEntry>().Add(new JournalEntry
        {
            Id = entryId,
            BusinessId = DefaultBusinessId,
            Description = "Entry with mixed lines",
            EntryDateUtc = FixedNow.AddDays(-1),
            Lines = new List<JournalEntryLine>
            {
                MakeLine(entryId, debitMinor: 1000L, creditMinor: 0L),
                MakeLine(entryId, debitMinor: 0L, creditMinor: 1000L),
                MakeLine(entryId, debitMinor: 500L, isDeleted: true)
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var (items, _) = await handler.HandleAsync(
            DefaultBusinessId, 1, 20, ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.LineCount.Should().Be(2, "soft-deleted lines excluded from count");
        item.TotalDebitMinor.Should().Be(1000L, "only non-deleted debit");
        item.TotalCreditMinor.Should().Be(1000L, "only non-deleted credit");
    }

    [Fact]
    public async Task GetJournalEntriesPage_GetSummary_Should_ReturnCorrectCounts()
    {
        await using var db = CreateDb();

        var recentId = Guid.NewGuid();
        var oldId = Guid.NewGuid();
        var multiLineId = Guid.NewGuid();
        db.Set<JournalEntry>().AddRange(
            new JournalEntry
            {
                Id = recentId,
                BusinessId = DefaultBusinessId,
                Description = "Recent entry",
                EntryDateUtc = FixedNow.AddDays(-2),
                Lines = new List<JournalEntryLine>
                {
                    MakeLine(recentId),
                    MakeLine(recentId)
                }
            },
            new JournalEntry
            {
                Id = oldId,
                BusinessId = DefaultBusinessId,
                Description = "Old entry",
                EntryDateUtc = FixedNow.AddDays(-30),
                Lines = new List<JournalEntryLine>()
            },
            new JournalEntry
            {
                Id = multiLineId,
                BusinessId = DefaultBusinessId,
                Description = "Multi-line recent",
                EntryDateUtc = FixedNow.AddDays(-1),
                Lines = new List<JournalEntryLine>
                {
                    MakeLine(multiLineId),
                    MakeLine(multiLineId),
                    MakeLine(multiLineId)
                }
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntriesPageHandler(db);
        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.RecentCount.Should().Be(2, "two entries within last 7 days");
        summary.MultiLineCount.Should().Be(1, "only the 3-line entry qualifies");
    }

    [Fact]
    public async Task GetJournalEntriesPage_GetSummary_Should_ReturnZeroCounts_WhenEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateJournalEntriesPageHandler(db);

        var summary = await handler.GetSummaryAsync(
            DefaultBusinessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.RecentCount.Should().Be(0);
        summary.MultiLineCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetJournalEntryForEditHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJournalEntryForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = CreateDb();
        var handler = CreateJournalEntryForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJournalEntryForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = CreateDb();
        var entry = MakeJournalEntry(isDeleted: true);
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntryForEditHandler(db);
        var result = await handler.HandleAsync(entry.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJournalEntryForEdit_Should_ReturnCorrectProjection_WithLines()
    {
        await using var db = CreateDb();
        var accountId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        db.Set<JournalEntry>().Add(new JournalEntry
        {
            Id = entryId,
            BusinessId = DefaultBusinessId,
            Description = "Balance Sheet Adjustment",
            EntryDateUtc = FixedNow.AddDays(-2),
            Lines = new List<JournalEntryLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = entryId,
                    AccountId = accountId,
                    DebitMinor = 10_000L,
                    CreditMinor = 0L,
                    Memo = "Debit side"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = entryId,
                    AccountId = accountId,
                    DebitMinor = 0L,
                    CreditMinor = 10_000L,
                    Memo = "Credit side"
                }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntryForEditHandler(db);
        var result = await handler.HandleAsync(entryId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entryId);
        result.BusinessId.Should().Be(DefaultBusinessId);
        result.Description.Should().Be("Balance Sheet Adjustment");
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x => x.DebitMinor).Should().Be(10_000L);
        result.Lines.Sum(x => x.CreditMinor).Should().Be(10_000L);
    }

    [Fact]
    public async Task GetJournalEntryForEdit_Should_ExcludeSoftDeletedLines()
    {
        await using var db = CreateDb();
        var entryId = Guid.NewGuid();
        db.Set<JournalEntry>().Add(new JournalEntry
        {
            Id = entryId,
            BusinessId = DefaultBusinessId,
            Description = "Entry with deleted line",
            EntryDateUtc = FixedNow.AddDays(-1),
            Lines = new List<JournalEntryLine>
            {
                MakeLine(entryId, debitMinor: 5_000L),
                MakeLine(entryId, creditMinor: 3_000L, isDeleted: true)
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateJournalEntryForEditHandler(db);
        var result = await handler.HandleAsync(entryId, TestContext.Current.CancellationToken);

        result!.Lines.Should().HaveCount(1, "soft-deleted lines are excluded");
        result.Lines.Single().DebitMinor.Should().Be(5_000L);
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────

    private sealed class FinancialQueryFixedClock : IClock
    {
        public FinancialQueryFixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FinancialQueryTestDbContext : DbContext, IAppDbContext
    {
        private FinancialQueryTestDbContext(DbContextOptions<FinancialQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinancialQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinancialQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_financial_query_{Guid.NewGuid()}")
                .Options;
            return new FinancialQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Expense>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Category).IsRequired();
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.JournalEntryId);
            });

            modelBuilder.Entity<JournalEntryLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.JournalEntryId).IsRequired();
                b.Property(x => x.AccountId).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
