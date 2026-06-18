using Darwin.Application.Sales.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Sales.Validators;

public sealed class SalesQuoteLineEditValidator : AbstractValidator<SalesQuoteLineEditDto>
{
    public SalesQuoteLineEditValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(x => x.Name).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(250);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPriceGrossMinor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPriceGrossMinor).GreaterThanOrEqualTo(x => x.UnitPriceNetMinor);
        RuleFor(x => x.TaxRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SalesQuoteCreateValidator : AbstractValidator<SalesQuoteCreateDto>
{
    public SalesQuoteCreateValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(x => x.Title).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(250);
        RuleFor(x => x.Currency)
            .Must(value => !string.IsNullOrWhiteSpace(value) && value.Trim().Length == 3)
            .WithMessage("Currency must be a three-letter ISO code.");
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
        RuleForEach(x => x.Lines).SetValidator(new SalesQuoteLineEditValidator(localizer));
    }
}

public sealed class SalesQuoteEditValidator : AbstractValidator<SalesQuoteEditDto>
{
    public SalesQuoteEditValidator(IStringLocalizer<ValidationResource> localizer)
    {
        Include(new SalesQuoteCreateValidator(localizer));
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class SalesQuoteLifecycleValidator : AbstractValidator<SalesQuoteLifecycleDto>
{
    public SalesQuoteLifecycleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public sealed class SalesQuoteConvertValidator : AbstractValidator<SalesQuoteConvertDto>
{
    public SalesQuoteConvertValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ConvertedOrderId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
