using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for Billing command handlers covering RowVersion concurrency guards,
/// status-transition policy, and create/update boundary validation.
/// Handlers: <see cref="CreatePaymentHandler"/>, <see cref="UpdatePaymentHandler"/>,
/// <see cref="CreateFinancialAccountHandler"/>, <see cref="UpdateFinancialAccountHandler"/>,
/// <see cref="CreateExpenseHandler"/>, <see cref="UpdateExpenseHandler"/>,
/// <see cref="CreateJournalEntryHandler"/>, <see cref="UpdateJournalEntryHandler"/>,
/// and <see cref="BillingStatusTransitionPolicy"/>.
/// </summary>
public sealed class BillingManagementCommandHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> Loc()
    {
        var mock = new Moq.Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[Moq.It.IsAny<string>()])
            .Returns<string>(n => new LocalizedString(n, n));
        mock.Setup(l => l[Moq.It.IsAny<string>(), Moq.It.IsAny<object[]>()])
            .Returns<string, object[]>((n, _) => new LocalizedString(n, n));
        return mock.Object;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BillingStatusTransitionPolicy
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Authorized, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Captured, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Voided, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Captured, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Voided, true)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Refunded, true)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Refunded, true)]
    public void BillingStatusTransitionPolicy_Should_AllowValidTransitions(
        PaymentStatus current, PaymentStatus target, bool expected)
    {
        var result = BillingStatusTransitionPolicy.IsPaymentTransitionAllowed(current, target);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Voided, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Voided, PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Authorized)]
    public void BillingStatusTransitionPolicy_Should_RejectInvalidTransitions(
        PaymentStatus current, PaymentStatus target)
    {
        var result = BillingStatusTransitionPolicy.IsPaymentTransitionAllowed(current, target);
        result.Should().BeFalse($"{current} → {target} must not be allowed");
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Voided)]
    public void BillingStatusTransitionPolicy_Should_AllowSameStatusIdentity(PaymentStatus status)
    {
        BillingStatusTransitionPolicy.IsPaymentTransitionAllowed(status, status)
            .Should().BeTrue($"same-status identity must always be allowed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreatePaymentHandler (billing layer)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePaymentHandler_Should_Throw_WhenBusinessIdIsEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var act = () => handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.Empty,
            AmountMinor = 100,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty BusinessId fails validation");
    }

    [Fact]
    public async Task CreatePaymentHandler_Should_Throw_WhenCurrencyIsInvalid()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var act = () => handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EU",   // too short
            Provider = "Cash",
            Status = PaymentStatus.Pending
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("currency must be 3 characters");
    }

    [Fact]
    public async Task CreatePaymentHandler_Should_PersistPayment_WhenInputIsValid()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var businessId = Guid.NewGuid();
        var id = await handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = businessId,
            AmountMinor = 5000,
            Currency = "EUR",
            Provider = "Stripe",
            Status = PaymentStatus.Pending,
            ProviderTransactionRef = "ch_test_abc"
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();
        var saved = await db.Set<Payment>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.BusinessId.Should().Be(businessId);
        saved.AmountMinor.Should().Be(5000);
        saved.Currency.Should().Be("EUR");
        saved.Provider.Should().Be("Stripe");
        saved.Status.Should().Be(PaymentStatus.Pending);
        saved.ProviderTransactionRef.Should().Be("ch_test_abc");
    }

    [Fact]
    public async Task CreatePaymentHandler_Should_UppercaseCurrency_WhenLowercaseSupplied()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var id = await handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            AmountMinor = 1000,
            Currency = "eur",
            Provider = "Cash",
            Status = PaymentStatus.Pending
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Payment>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Currency.Should().Be("EUR", "currency must be normalized to uppercase");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdatePaymentHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePaymentHandler_Should_Throw_WhenPaymentNotFound()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), Loc());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent payment must raise not-found");
    }

    [Fact]
    public async Task UpdatePaymentHandler_Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), Loc());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = []   // empty
        }, TestContext.Current.CancellationToken);

        // The edit validator rejects empty RowVersion before reaching the handler concurrency check.
        await act.Should().ThrowAsync<ValidationException>("empty RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdatePaymentHandler_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), Loc());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = [9, 9, 9]   // stale
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must raise concurrency conflict");
    }

    [Fact]
    public async Task UpdatePaymentHandler_Should_Throw_WhenStatusTransitionIsForbidden()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Failed,
            RowVersion = [5]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), Loc());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = paymentId,
            BusinessId = Guid.NewGuid(),
            AmountMinor = 500,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,   // Failed → Pending is forbidden
            RowVersion = [5]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Failed → Pending must be rejected by transition policy");
    }

    [Fact]
    public async Task UpdatePaymentHandler_Should_PersistChanges_WhenRowVersionMatches()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            BusinessId = businessId,
            AmountMinor = 1000,
            Currency = "EUR",
            Provider = "Cash",
            Status = PaymentStatus.Pending,
            RowVersion = [7]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), Loc());

        await handler.HandleAsync(new PaymentEditDto
        {
            Id = paymentId,
            BusinessId = businessId,
            AmountMinor = 2000,
            Currency = "USD",
            Provider = "Stripe",
            Status = PaymentStatus.Captured,
            RowVersion = [7]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        updated.AmountMinor.Should().Be(2000);
        updated.Currency.Should().Be("USD");
        updated.Provider.Should().Be("Stripe");
        updated.Status.Should().Be(PaymentStatus.Captured);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateFinancialAccountHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFinancialAccountHandler_Should_Throw_WhenBusinessIdIsEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());

        var act = () => handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = Guid.Empty,
            Name = "Cash Account",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty BusinessId fails validation");
    }

    [Fact]
    public async Task CreateFinancialAccountHandler_Should_Throw_WhenNameIsEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());

        var act = () => handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Name fails validation");
    }

    [Fact]
    public async Task CreateFinancialAccountHandler_Should_PersistAccount_WhenInputIsValid()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());
        var businessId = Guid.NewGuid();

        var id = await handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = businessId,
            Name = "  Revenue Account  ",
            Type = AccountType.Revenue,
            Code = " REV-001 "
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();
        var saved = await db.Set<FinancialAccount>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.BusinessId.Should().Be(businessId);
        saved.Name.Should().Be("Revenue Account", "name should be trimmed");
        saved.Code.Should().Be("REV-001", "code should be trimmed");
        saved.Type.Should().Be(AccountType.Revenue);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateFinancialAccountHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateFinancialAccountHandler_Should_Throw_WhenAccountNotFound()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new UpdateFinancialAccountHandler(db, new FinancialAccountEditValidator(), Loc());

        var act = () => handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Test",
            Type = AccountType.Asset,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent account must raise not-found");
    }

    [Fact]
    public async Task UpdateFinancialAccountHandler_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var accountId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            Name = "Old Name",
            Type = AccountType.Asset,
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateFinancialAccountHandler(db, new FinancialAccountEditValidator(), Loc());

        var act = () => handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            Name = "New Name",
            Type = AccountType.Asset,
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must raise concurrency conflict");
    }

    [Fact]
    public async Task UpdateFinancialAccountHandler_Should_PersistChanges_WhenRowVersionMatches()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var accountId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Id = accountId,
            BusinessId = businessId,
            Name = "Old Name",
            Type = AccountType.Asset,
            Code = "OLD-001",
            RowVersion = [4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateFinancialAccountHandler(db, new FinancialAccountEditValidator(), Loc());

        await handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = accountId,
            BusinessId = businessId,
            Name = "  New Name  ",
            Type = AccountType.Revenue,
            Code = "  REV-002  ",
            RowVersion = [4]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<FinancialAccount>().SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("New Name", "name should be trimmed");
        updated.Type.Should().Be(AccountType.Revenue);
        updated.Code.Should().Be("REV-002", "code should be trimmed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateExpenseHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateExpenseHandler_Should_Throw_WhenCategoryIsEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());

        var act = () => handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Category = "",
            Description = "Office supplies",
            AmountMinor = 5000
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Category must be rejected");
    }

    [Fact]
    public async Task CreateExpenseHandler_Should_PersistExpense_WhenInputIsValid()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var id = await handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Category = "  Travel  ",
            Description = "  Conference tickets  ",
            AmountMinor = 12000,
            ExpenseDateUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();
        var saved = await db.Set<Expense>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.BusinessId.Should().Be(businessId);
        saved.SupplierId.Should().Be(supplierId);
        saved.Category.Should().Be("Travel", "category should be trimmed");
        saved.Description.Should().Be("Conference tickets", "description should be trimmed");
        saved.AmountMinor.Should().Be(12000);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateExpenseHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateExpenseHandler_Should_Throw_WhenExpenseNotFound()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), Loc());

        var act = () => handler.HandleAsync(new ExpenseEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Category = "Utilities",
            Description = "Electric bill",
            AmountMinor = 3000,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent expense must raise not-found");
    }

    [Fact]
    public async Task UpdateExpenseHandler_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var expenseId = Guid.NewGuid();
        db.Set<Expense>().Add(new Expense
        {
            Id = expenseId,
            BusinessId = Guid.NewGuid(),
            Category = "Travel",
            Description = "Train tickets",
            AmountMinor = 8000,
            RowVersion = [2, 3, 4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), Loc());

        var act = () => handler.HandleAsync(new ExpenseEditDto
        {
            Id = expenseId,
            BusinessId = Guid.NewGuid(),
            Category = "Travel",
            Description = "Taxi",
            AmountMinor = 3000,
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must raise concurrency conflict");
    }

    [Fact]
    public async Task UpdateExpenseHandler_Should_PersistChanges_WhenRowVersionMatches()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var expenseId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Expense>().Add(new Expense
        {
            Id = expenseId,
            BusinessId = businessId,
            Category = "Travel",
            Description = "Old description",
            AmountMinor = 5000,
            RowVersion = [6]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), Loc());

        await handler.HandleAsync(new ExpenseEditDto
        {
            Id = expenseId,
            BusinessId = businessId,
            Category = "  Utilities  ",
            Description = "  Electric bill  ",
            AmountMinor = 9000,
            RowVersion = [6]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Expense>().SingleAsync(x => x.Id == expenseId, TestContext.Current.CancellationToken);
        updated.Category.Should().Be("Utilities", "category should be trimmed");
        updated.Description.Should().Be("Electric bill", "description should be trimmed");
        updated.AmountMinor.Should().Be(9000);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateJournalEntryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateJournalEntryHandler_Should_Throw_WhenLinesAreEmpty()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var loc = Loc();
        var handler = new CreateJournalEntryHandler(db, new JournalEntryCreateValidator(loc));

        var act = () => handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Description = "Empty entry",
            Lines = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Lines must be rejected");
    }

    [Fact]
    public async Task CreateJournalEntryHandler_Should_Throw_WhenLinesAreUnbalanced()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var loc = Loc();
        var handler = new CreateJournalEntryHandler(db, new JournalEntryCreateValidator(loc));

        var act = () => handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Description = "Unbalanced",
            Lines =
            [
                new JournalEntryLineDto { AccountId = Guid.NewGuid(), DebitMinor = 1000, CreditMinor = 0 },
                new JournalEntryLineDto { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 500 }
            ]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("debits != credits must be rejected by balance validator");
    }

    [Fact]
    public async Task CreateJournalEntryHandler_Should_PersistEntryWithLines_WhenInputIsValid()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var loc = Loc();
        var handler = new CreateJournalEntryHandler(db, new JournalEntryCreateValidator(loc));
        var businessId = Guid.NewGuid();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var id = await handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = businessId,
            EntryDateUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Description = "  April entry  ",
            Lines =
            [
                new JournalEntryLineDto { AccountId = accountA, DebitMinor = 2000, CreditMinor = 0, Memo = "  Debit memo  " },
                new JournalEntryLineDto { AccountId = accountB, DebitMinor = 0, CreditMinor = 2000, Memo = null }
            ]
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();
        var saved = await db.Set<JournalEntry>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        saved.BusinessId.Should().Be(businessId);
        saved.Description.Should().Be("April entry", "description should be trimmed");
        saved.Lines.Should().HaveCount(2);
        saved.Lines.First(l => l.AccountId == accountA).DebitMinor.Should().Be(2000);
        saved.Lines.First(l => l.AccountId == accountA).Memo.Should().Be("Debit memo", "memo should be trimmed");
        saved.Lines.First(l => l.AccountId == accountB).CreditMinor.Should().Be(2000);
        saved.Lines.First(l => l.AccountId == accountB).Memo.Should().BeNull("null memo stays null");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateJournalEntryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateJournalEntryHandler_Should_Throw_WhenEntryNotFound()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var loc = Loc();
        var handler = new UpdateJournalEntryHandler(db, new JournalEntryEditValidator(loc), loc);
        var accountId = Guid.NewGuid();

        var act = () => handler.HandleAsync(new JournalEntryEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Description = "Missing entry",
            RowVersion = [1],
            Lines =
            [
                new JournalEntryLineDto { AccountId = accountId, DebitMinor = 1000, CreditMinor = 0 },
                new JournalEntryLineDto { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 1000 }
            ]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent entry must raise not-found");
    }

    [Fact]
    public async Task UpdateJournalEntryHandler_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var entryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var entry = new JournalEntry
        {
            Id = entryId,
            BusinessId = Guid.NewGuid(),
            Description = "Old",
            RowVersion = [1, 2, 3],
            Lines = [new JournalEntryLine { AccountId = accountId, DebitMinor = 1000, CreditMinor = 0 }]
        };
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loc = Loc();
        var handler = new UpdateJournalEntryHandler(db, new JournalEntryEditValidator(loc), loc);

        var act = () => handler.HandleAsync(new JournalEntryEditDto
        {
            Id = entryId,
            BusinessId = Guid.NewGuid(),
            Description = "Updated",
            RowVersion = [9, 9, 9],
            Lines =
            [
                new JournalEntryLineDto { AccountId = accountId, DebitMinor = 1000, CreditMinor = 0 },
                new JournalEntryLineDto { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 1000 }
            ]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must raise concurrency conflict");
    }

    [Fact]
    public async Task UpdateJournalEntryHandler_Should_ReplaceLines_WhenRowVersionMatches()
    {
        await using var db = BillingCmdTestDbContext.Create();
        var entryId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var oldAccountId = Guid.NewGuid();
        var newAccountA = Guid.NewGuid();
        var newAccountB = Guid.NewGuid();

        var entry = new JournalEntry
        {
            Id = entryId,
            BusinessId = businessId,
            Description = "Original",
            RowVersion = [8],
            Lines = [new JournalEntryLine { AccountId = oldAccountId, DebitMinor = 500, CreditMinor = 0 }]
        };
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loc = Loc();
        var handler = new UpdateJournalEntryHandler(db, new JournalEntryEditValidator(loc), loc);

        await handler.HandleAsync(new JournalEntryEditDto
        {
            Id = entryId,
            BusinessId = businessId,
            Description = "  Updated entry  ",
            RowVersion = [8],
            Lines =
            [
                new JournalEntryLineDto { AccountId = newAccountA, DebitMinor = 3000, CreditMinor = 0 },
                new JournalEntryLineDto { AccountId = newAccountB, DebitMinor = 0, CreditMinor = 3000 }
            ]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<JournalEntry>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == entryId, TestContext.Current.CancellationToken);

        updated.Description.Should().Be("Updated entry", "description should be trimmed");
        updated.Lines.Should().HaveCount(2, "old line replaced by two new lines");
        updated.Lines.Should().NotContain(l => l.AccountId == oldAccountId, "old line must be removed");
        updated.Lines.Sum(l => l.DebitMinor).Should().Be(updated.Lines.Sum(l => l.CreditMinor), "entry must remain balanced");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test DbContext
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class BillingCmdTestDbContext : DbContext, IAppDbContext
    {
        private BillingCmdTestDbContext(DbContextOptions<BillingCmdTestDbContext> opts)
            : base(opts)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingCmdTestDbContext Create()
        {
            var opts = new DbContextOptionsBuilder<BillingCmdTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_cmd_tests_{Guid.NewGuid()}")
                .Options;
            return new BillingCmdTestDbContext(opts);
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            mb.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            mb.Entity<Expense>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Category).IsRequired();
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            mb.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.JournalEntryId);
            });

            mb.Entity<JournalEntryLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.AccountId).IsRequired();
            });
        }
    }
}
