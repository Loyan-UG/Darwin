using Darwin.Application.Billing.DTOs;
using FluentValidation;

namespace Darwin.Application.Billing.Validators
{
    public sealed class PaymentCreateValidator : AbstractValidator<PaymentCreateDto>
    {
        public PaymentCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.AmountMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(120);
            RuleFor(x => x.ProviderTransactionRef).MaximumLength(200);
        }
    }

    public sealed class PaymentEditValidator : AbstractValidator<PaymentEditDto>
    {
        public PaymentEditValidator()
        {
            Include(new PaymentCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class FinancialAccountCreateValidator : AbstractValidator<FinancialAccountCreateDto>
    {
        public FinancialAccountCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Code).MaximumLength(64);
        }
    }

    public sealed class FinancialAccountEditValidator : AbstractValidator<FinancialAccountEditDto>
    {
        public FinancialAccountEditValidator()
        {
            Include(new FinancialAccountCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class ExpenseCreateValidator : AbstractValidator<ExpenseCreateDto>
    {
        public ExpenseCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Category).NotEmpty().MaximumLength(120);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.AmountMinor).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class ExpenseEditValidator : AbstractValidator<ExpenseEditDto>
    {
        public ExpenseEditValidator()
        {
            Include(new ExpenseCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class JournalEntryLineValidator : AbstractValidator<JournalEntryLineDto>
    {
        public JournalEntryLineValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.DebitMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CreditMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Memo).MaximumLength(500);
            RuleFor(x => x)
                .Must(x => (x.DebitMinor > 0 && x.CreditMinor == 0) || (x.CreditMinor > 0 && x.DebitMinor == 0))
                .WithMessage("Each journal line must contain either a debit or a credit amount.");
        }
    }

    public sealed class JournalEntryCreateValidator : AbstractValidator<JournalEntryCreateDto>
    {
        public JournalEntryCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new JournalEntryLineValidator());
            RuleFor(x => x)
                .Must(x => x.Lines.Sum(l => l.DebitMinor) == x.Lines.Sum(l => l.CreditMinor))
                .WithMessage("Journal entry must be balanced.");
        }
    }

    public sealed class JournalEntryEditValidator : AbstractValidator<JournalEntryEditDto>
    {
        public JournalEntryEditValidator()
        {
            Include(new JournalEntryCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }
}
