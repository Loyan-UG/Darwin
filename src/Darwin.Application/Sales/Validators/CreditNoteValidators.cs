using Darwin.Application.Sales.DTOs;
using FluentValidation;

namespace Darwin.Application.Sales.Validators;

public sealed class CreditNoteCreateValidator : AbstractValidator<CreditNoteCreateDto>
{
    public CreditNoteCreateValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.InvoiceLineId).NotEmpty();
            line.RuleFor(x => x.CreditedQuantity).GreaterThan(0);
        });
    }
}

public sealed class CreditNoteLifecycleValidator : AbstractValidator<CreditNoteLifecycleDto>
{
    public CreditNoteLifecycleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
