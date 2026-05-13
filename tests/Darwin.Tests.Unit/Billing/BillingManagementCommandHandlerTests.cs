using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Moq;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Handler-level unit tests for billing management command handlers.
/// Covers <see cref="BillingStatusTransitionPolicy"/>,
/// <see cref="CreatePaymentHandler"/>, <see cref="UpdatePaymentHandler"/>,
/// <see cref="CreateFinancialAccountHandler"/>, <see cref="UpdateFinancialAccountHandler"/>,
/// <see cref="CreateExpenseHandler"/>, <see cref="UpdateExpenseHandler"/>,
/// <see cref="CreateJournalEntryHandler"/>, and <see cref="UpdateJournalEntryHandler"/>.
/// </summary>
public sealed class BillingManagementCommandHandlerTests
{
    // ─── Shared helpers ───────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── BillingStatusTransitionPolicy ────────────────────────────────────────

    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Authorized, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Captured, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Voided, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Refunded, false)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Captured, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Voided, true)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Refunded, false)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Completed, true)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Refunded, true)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Authorized, false)]
    [InlineData(PaymentStatus.Captured, PaymentStatus.Failed, false)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Refunded, true)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Captured, false)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Authorized, false)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Captured, false)]
    [InlineData(PaymentStatus.Voided, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Voided, PaymentStatus.Authorized, false)]
    public void BillingStatusTransitionPolicy_Should_EnforceTransitionRules(
        PaymentStatus current, PaymentStatus target, bool expectedAllowed)
    {
        var result = BillingStatusTransitionPolicy.IsPaymentTransitionAllowed(current, target);
        result.Should().Be(expectedAllowed,
            $"transition {current} → {target} should be {(expectedAllowed ? "allowed" : "forbidden")}");
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Voided)]
    public void BillingStatusTransitionPolicy_Should_AllowSameStatusTransition(PaymentStatus status)
    {
        BillingStatusTransitionPolicy.IsPaymentTransitionAllowed(status, status)
            .Should().BeTrue("a no-op transition to the same status must always be allowed");
    }

    // ─── CreatePaymentHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayment_Should_Throw_ValidationException_When_BusinessId_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var act = () => handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.Empty,
            Currency = "EUR",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty BusinessId violates the create validator");
    }

    [Fact]
    public async Task CreatePayment_Should_Throw_ValidationException_When_Currency_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var act = () => handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Currency = "",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Currency violates the create validator");
    }

    [Fact]
    public async Task CreatePayment_Should_Throw_ValidationException_When_Provider_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var act = () => handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Currency = "EUR",
            Provider = ""
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Provider violates the create validator");
    }

    [Fact]
    public async Task CreatePayment_Should_Persist_Payment_And_Return_Id()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        var id = await handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = businessId,
            AmountMinor = 5000,
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            Provider = "Stripe"
        }, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);
        var saved = await db.Set<Payment>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Id.Should().Be(id);
        saved.BusinessId.Should().Be(businessId);
        saved.AmountMinor.Should().Be(5000);
        saved.Status.Should().Be(PaymentStatus.Pending);
        saved.Provider.Should().Be("Stripe");
    }

    [Fact]
    public async Task CreatePayment_Should_Uppercase_Currency()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        await handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Currency = "eur",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Payment>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Currency.Should().Be("EUR", "currency must be uppercased on persist");
    }

    [Fact]
    public async Task CreatePayment_Should_Normalize_Optional_Provider_Refs()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreatePaymentHandler(db, new PaymentCreateValidator());

        await handler.HandleAsync(new PaymentCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Currency = "USD",
            Provider = "Stripe",
            ProviderTransactionRef = "  txn_123  ",
            ProviderPaymentIntentRef = "   ",
            ProviderCheckoutSessionRef = null
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Payment>().SingleAsync(TestContext.Current.CancellationToken);
        saved.ProviderTransactionRef.Should().Be("txn_123", "whitespace should be trimmed");
        saved.ProviderPaymentIntentRef.Should().BeNull("whitespace-only value should be stored as null");
        saved.ProviderCheckoutSessionRef.Should().BeNull("null remains null");
    }

    // ─── UpdatePaymentHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePayment_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Currency = "EUR",
            Provider = "Stripe"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Id violates the edit validator");
    }

    [Fact]
    public async Task UpdatePayment_Should_Throw_InvalidOperationException_When_Payment_Not_Found()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Currency = "EUR",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent payment must throw");
    }

    [Fact]
    public async Task UpdatePayment_Should_Throw_ValidationException_When_RowVersion_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var payment = BuildPayment(rowVersion: new byte[] { 1 });
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = payment.Id,
            RowVersion = Array.Empty<byte>(),
            BusinessId = Guid.NewGuid(),
            Currency = "EUR",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        // The PaymentEditValidator fires first and rejects empty RowVersion with a ValidationException.
        await act.Should().ThrowAsync<ValidationException>(
            "the edit validator rejects empty RowVersion before the handler can perform a concurrency check");
    }

    [Fact]
    public async Task UpdatePayment_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Stale()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var payment = BuildPayment(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = payment.Id,
            RowVersion = new byte[] { 9, 9, 9 },
            BusinessId = Guid.NewGuid(),
            Currency = "EUR",
            Provider = "Manual"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "stale RowVersion must raise a concurrency exception");
    }

    [Fact]
    public async Task UpdatePayment_Should_Throw_ValidationException_When_Status_Transition_Forbidden()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var rowVersion = new byte[] { 1 };
        var payment = BuildPayment(rowVersion: rowVersion, status: PaymentStatus.Failed);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PaymentEditDto
        {
            Id = payment.Id,
            RowVersion = rowVersion,
            BusinessId = payment.BusinessId ?? Guid.NewGuid(),
            Currency = "EUR",
            Provider = "Manual",
            Status = PaymentStatus.Pending   // Failed → Pending is forbidden
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "a forbidden status transition must throw a validation exception");
    }

    [Fact]
    public async Task UpdatePayment_Should_Persist_Changes_When_RowVersion_Matches()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var rowVersion = new byte[] { 5 };
        var businessId = Guid.NewGuid();
        var payment = BuildPayment(rowVersion: rowVersion, status: PaymentStatus.Pending);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentHandler(db, new PaymentEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new PaymentEditDto
        {
            Id = payment.Id,
            RowVersion = rowVersion,
            BusinessId = businessId,
            AmountMinor = 9900,
            Currency = "usd",
            Provider = " Updated ",
            Status = PaymentStatus.Authorized
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Payment>().SingleAsync(TestContext.Current.CancellationToken);
        updated.AmountMinor.Should().Be(9900);
        updated.Currency.Should().Be("USD", "currency must be uppercased");
        updated.Provider.Should().Be("Updated", "provider must be trimmed");
        updated.Status.Should().Be(PaymentStatus.Authorized);
    }

    // ─── CreateFinancialAccountHandler ────────────────────────────────────────

    [Fact]
    public async Task CreateFinancialAccount_Should_Throw_ValidationException_When_Name_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());

        var act = () => handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Name violates the create validator");
    }

    [Fact]
    public async Task CreateFinancialAccount_Should_Persist_Account_And_Return_Id()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());

        var id = await handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = businessId,
            Name = "Cash",
            Type = AccountType.Asset,
            Code = "1001"
        }, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);
        var saved = await db.Set<FinancialAccount>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Id.Should().Be(id);
        saved.BusinessId.Should().Be(businessId);
        saved.Name.Should().Be("Cash");
        saved.Type.Should().Be(AccountType.Asset);
        saved.Code.Should().Be("1001");
    }

    [Fact]
    public async Task CreateFinancialAccount_Should_Trim_Name_And_Normalize_Code()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreateFinancialAccountHandler(db, new FinancialAccountCreateValidator());

        await handler.HandleAsync(new FinancialAccountCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "  Revenue Account  ",
            Type = AccountType.Revenue,
            Code = "  "    // whitespace-only code should normalize to null
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<FinancialAccount>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Revenue Account", "Name must be trimmed");
        saved.Code.Should().BeNull("whitespace-only Code must be normalized to null");
    }

    // ─── UpdateFinancialAccountHandler ────────────────────────────────────────

    [Fact]
    public async Task UpdateFinancialAccount_Should_Throw_InvalidOperationException_When_Not_Found()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new UpdateFinancialAccountHandler(
            db, new FinancialAccountEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Name = "Cash",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent account must throw");
    }

    [Fact]
    public async Task UpdateFinancialAccount_Should_Throw_ValidationException_When_RowVersion_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var account = BuildFinancialAccount(rowVersion: new byte[] { 1 });
        db.Set<FinancialAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateFinancialAccountHandler(
            db, new FinancialAccountEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = account.Id,
            RowVersion = Array.Empty<byte>(),
            BusinessId = Guid.NewGuid(),
            Name = "Cash",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        // The FinancialAccountEditValidator fires first and rejects empty RowVersion with a ValidationException.
        await act.Should().ThrowAsync<ValidationException>(
            "the edit validator rejects empty RowVersion before the handler can perform a concurrency check");
    }

    [Fact]
    public async Task UpdateFinancialAccount_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Stale()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var account = BuildFinancialAccount(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<FinancialAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateFinancialAccountHandler(
            db, new FinancialAccountEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = account.Id,
            RowVersion = new byte[] { 9, 9 },
            BusinessId = Guid.NewGuid(),
            Name = "Cash",
            Type = AccountType.Asset
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "stale RowVersion must raise a concurrency exception");
    }

    [Fact]
    public async Task UpdateFinancialAccount_Should_Persist_Changes_When_RowVersion_Matches()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var rowVersion = new byte[] { 7 };
        var account = BuildFinancialAccount(rowVersion: rowVersion);
        db.Set<FinancialAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateFinancialAccountHandler(
            db, new FinancialAccountEditValidator(), CreateLocalizer());

        var newBusinessId = Guid.NewGuid();
        await handler.HandleAsync(new FinancialAccountEditDto
        {
            Id = account.Id,
            RowVersion = rowVersion,
            BusinessId = newBusinessId,
            Name = "  Updated Account  ",
            Type = AccountType.Expense,
            Code = "EXP-01"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<FinancialAccount>().SingleAsync(TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Updated Account", "Name must be trimmed");
        updated.Type.Should().Be(AccountType.Expense);
        updated.Code.Should().Be("EXP-01");
        updated.BusinessId.Should().Be(newBusinessId);
    }

    // ─── CreateExpenseHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateExpense_Should_Throw_ValidationException_When_Category_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());

        var act = () => handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Category = "",
            Description = "Some expense",
            AmountMinor = 100
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Category violates the create validator");
    }

    [Fact]
    public async Task CreateExpense_Should_Throw_ValidationException_When_Description_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());

        var act = () => handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Category = "Office",
            Description = "",
            AmountMinor = 100
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Description violates the create validator");
    }

    [Fact]
    public async Task CreateExpense_Should_Persist_Expense_And_Return_Id()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());

        var id = await handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Category = "Office Supplies",
            Description = "Paper and pens",
            AmountMinor = 2500,
            ExpenseDateUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);
        var saved = await db.Set<Expense>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Id.Should().Be(id);
        saved.BusinessId.Should().Be(businessId);
        saved.SupplierId.Should().Be(supplierId);
        saved.Category.Should().Be("Office Supplies");
        saved.Description.Should().Be("Paper and pens");
        saved.AmountMinor.Should().Be(2500);
    }

    [Fact]
    public async Task CreateExpense_Should_Trim_Category_And_Description()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new CreateExpenseHandler(db, new ExpenseCreateValidator());

        await handler.HandleAsync(new ExpenseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Category = "  Travel  ",
            Description = "  Flight tickets  ",
            AmountMinor = 50000
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Expense>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Category.Should().Be("Travel", "Category must be trimmed");
        saved.Description.Should().Be("Flight tickets", "Description must be trimmed");
    }

    // ─── UpdateExpenseHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateExpense_Should_Throw_InvalidOperationException_When_Not_Found()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new ExpenseEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Category = "Office",
            Description = "Desc",
            AmountMinor = 100
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent expense must throw");
    }

    [Fact]
    public async Task UpdateExpense_Should_Throw_ValidationException_When_RowVersion_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var expense = BuildExpense(rowVersion: new byte[] { 1 });
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new ExpenseEditDto
        {
            Id = expense.Id,
            RowVersion = Array.Empty<byte>(),
            BusinessId = Guid.NewGuid(),
            Category = "Office",
            Description = "Desc",
            AmountMinor = 100
        }, TestContext.Current.CancellationToken);

        // The ExpenseEditValidator fires first and rejects empty RowVersion with a ValidationException.
        await act.Should().ThrowAsync<ValidationException>(
            "the edit validator rejects empty RowVersion before the handler can perform a concurrency check");
    }

    [Fact]
    public async Task UpdateExpense_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Stale()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var expense = BuildExpense(rowVersion: new byte[] { 1, 2 });
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new ExpenseEditDto
        {
            Id = expense.Id,
            RowVersion = new byte[] { 9, 9 },
            BusinessId = Guid.NewGuid(),
            Category = "Office",
            Description = "Desc",
            AmountMinor = 100
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "stale RowVersion must raise a concurrency exception");
    }

    [Fact]
    public async Task UpdateExpense_Should_Persist_Changes_When_RowVersion_Matches()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var rowVersion = new byte[] { 3 };
        var expense = BuildExpense(rowVersion: rowVersion);
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateExpenseHandler(db, new ExpenseEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new ExpenseEditDto
        {
            Id = expense.Id,
            RowVersion = rowVersion,
            BusinessId = Guid.NewGuid(),
            Category = "  Marketing  ",
            Description = "  Social media ads  ",
            AmountMinor = 75000,
            ExpenseDateUtc = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc)
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Expense>().SingleAsync(TestContext.Current.CancellationToken);
        updated.Category.Should().Be("Marketing", "Category must be trimmed");
        updated.Description.Should().Be("Social media ads", "Description must be trimmed");
        updated.AmountMinor.Should().Be(75000);
    }

    // ─── CreateJournalEntryHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateJournalEntry_Should_Throw_ValidationException_When_Lines_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var handler = new CreateJournalEntryHandler(
            db, new JournalEntryCreateValidator(localizer));

        var act = () => handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Description = "Test entry",
            Lines = new List<JournalEntryLineDto>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Lines violates the create validator");
    }

    [Fact]
    public async Task CreateJournalEntry_Should_Throw_ValidationException_When_Lines_Not_Balanced()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var handler = new CreateJournalEntryHandler(
            db, new JournalEntryCreateValidator(localizer));

        var act = () => handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Description = "Unbalanced entry",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), DebitMinor = 500, CreditMinor = 0 },
                new() { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 300 }  // 500 ≠ 300
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("unbalanced debits/credits violate double-entry rule");
    }

    [Fact]
    public async Task CreateJournalEntry_Should_Throw_ValidationException_When_Line_Has_Both_Debit_And_Credit()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var handler = new CreateJournalEntryHandler(
            db, new JournalEntryCreateValidator(localizer));

        var act = () => handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Description = "Invalid line entry",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), DebitMinor = 100, CreditMinor = 100 }
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "a line with both debit and credit violates the exclusive-or rule");
    }

    [Fact]
    public async Task CreateJournalEntry_Should_Persist_Entry_With_Balanced_Lines()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var businessId = Guid.NewGuid();
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var handler = new CreateJournalEntryHandler(
            db, new JournalEntryCreateValidator(localizer));

        var entryDate = new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var id = await handler.HandleAsync(new JournalEntryCreateDto
        {
            BusinessId = businessId,
            EntryDateUtc = entryDate,
            Description = "  Cash sale  ",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = accountId1, DebitMinor = 1000, CreditMinor = 0, Memo = "  Dr Cash  " },
                new() { AccountId = accountId2, DebitMinor = 0, CreditMinor = 1000, Memo = null }
            }
        }, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);

        var saved = await db.Set<JournalEntry>()
            .Include(e => e.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);

        saved.BusinessId.Should().Be(businessId);
        saved.Description.Should().Be("Cash sale", "Description must be trimmed");
        saved.EntryDateUtc.Should().Be(entryDate);
        saved.Lines.Should().HaveCount(2);
        saved.Lines.Sum(l => l.DebitMinor).Should().Be(saved.Lines.Sum(l => l.CreditMinor),
            "persisted lines must be balanced");
        saved.Lines.First(l => l.DebitMinor > 0).Memo.Should().Be("Dr Cash",
            "line Memo whitespace should be trimmed");
        saved.Lines.First(l => l.CreditMinor > 0).Memo.Should().BeNull(
            "null Memo should remain null");
    }

    // ─── UpdateJournalEntryHandler ────────────────────────────────────────────

    [Fact]
    public async Task UpdateJournalEntry_Should_Throw_InvalidOperationException_When_Not_Found()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var handler = new UpdateJournalEntryHandler(
            db, new JournalEntryEditValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new JournalEntryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Description = "Desc",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), DebitMinor = 500, CreditMinor = 0 },
                new() { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 500 }
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent entry must throw");
    }

    [Fact]
    public async Task UpdateJournalEntry_Should_Throw_ValidationException_When_RowVersion_Empty()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var entry = BuildJournalEntry(rowVersion: new byte[] { 1 });
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateJournalEntryHandler(
            db, new JournalEntryEditValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new JournalEntryEditDto
        {
            Id = entry.Id,
            RowVersion = Array.Empty<byte>(),
            BusinessId = Guid.NewGuid(),
            Description = "Updated",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), DebitMinor = 100, CreditMinor = 0 },
                new() { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 100 }
            }
        }, TestContext.Current.CancellationToken);

        // The JournalEntryEditValidator fires first and rejects empty RowVersion with a ValidationException.
        await act.Should().ThrowAsync<ValidationException>(
            "the edit validator rejects empty RowVersion before the handler can perform a concurrency check");
    }

    [Fact]
    public async Task UpdateJournalEntry_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Stale()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var entry = BuildJournalEntry(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateJournalEntryHandler(
            db, new JournalEntryEditValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new JournalEntryEditDto
        {
            Id = entry.Id,
            RowVersion = new byte[] { 9, 9, 9 },
            BusinessId = Guid.NewGuid(),
            Description = "Updated",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = Guid.NewGuid(), DebitMinor = 100, CreditMinor = 0 },
                new() { AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 100 }
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "stale RowVersion must raise a concurrency exception");
    }

    [Fact]
    public async Task UpdateJournalEntry_Should_Replace_Lines_When_RowVersion_Matches()
    {
        await using var db = BillingCommandTestDbContext.Create();
        var localizer = CreateLocalizer();
        var rowVersion = new byte[] { 4 };
        var entry = BuildJournalEntry(rowVersion: rowVersion);
        var oldAccountId = Guid.NewGuid();
        entry.Lines = new List<JournalEntryLine>
        {
            new() { Id = Guid.NewGuid(), AccountId = oldAccountId, DebitMinor = 200, CreditMinor = 0 },
            new() { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), DebitMinor = 0, CreditMinor = 200 }
        };
        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateJournalEntryHandler(
            db, new JournalEntryEditValidator(localizer), localizer);

        var newAccountId1 = Guid.NewGuid();
        var newAccountId2 = Guid.NewGuid();
        await handler.HandleAsync(new JournalEntryEditDto
        {
            Id = entry.Id,
            RowVersion = rowVersion,
            BusinessId = entry.BusinessId,
            Description = "  Updated entry  ",
            Lines = new List<JournalEntryLineDto>
            {
                new() { AccountId = newAccountId1, DebitMinor = 750, CreditMinor = 0, Memo = "  Dr  " },
                new() { AccountId = newAccountId2, DebitMinor = 0, CreditMinor = 750, Memo = "   " }
            }
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<JournalEntry>()
            .Include(e => e.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);

        updated.Description.Should().Be("Updated entry", "Description must be trimmed");
        updated.Lines.Should().HaveCount(2, "old lines must be replaced by new lines");
        updated.Lines.Should().NotContain(l => l.AccountId == oldAccountId,
            "old account must not appear after replacement");
        updated.Lines.First(l => l.DebitMinor > 0).Memo.Should().Be("Dr",
            "line memo whitespace must be trimmed");
        updated.Lines.First(l => l.CreditMinor > 0).Memo.Should().BeNull(
            "whitespace-only memo must normalize to null");
    }

    // ─── Entity builders ──────────────────────────────────────────────────────

    private static Payment BuildPayment(
        byte[]? rowVersion = null,
        PaymentStatus status = PaymentStatus.Pending) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            AmountMinor = 1000,
            Currency = "EUR",
            Provider = "Manual",
            Status = status,
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    private static FinancialAccount BuildFinancialAccount(byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Test Account",
            Type = AccountType.Asset,
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    private static Expense BuildExpense(byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Category = "Office",
            Description = "Office supplies",
            AmountMinor = 500,
            ExpenseDateUtc = DateTime.UtcNow,
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    private static JournalEntry BuildJournalEntry(byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            EntryDateUtc = DateTime.UtcNow,
            Description = "Test Entry",
            Lines = new List<JournalEntryLine>(),
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    // ─── DbContext ────────────────────────────────────────────────────────────

    private sealed class BillingCommandTestDbContext : DbContext, IAppDbContext
    {
        private BillingCommandTestDbContext(DbContextOptions<BillingCommandTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingCommandTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingCommandTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_cmd_tests_{Guid.NewGuid()}")
                .Options;
            return new BillingCommandTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Expense>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Category).IsRequired();
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.JournalEntryId);
            });

            modelBuilder.Entity<JournalEntryLine>(b =>
            {
                b.HasKey(x => x.Id);
            });
        }
    }
}
