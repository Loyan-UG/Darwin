using Darwin.Application.Pricing.DTOs;
using FluentValidation;

namespace Darwin.Application.Pricing.Validators
{
    public sealed class TaxCategoryCreateValidator : AbstractValidator<TaxCategoryCreateDto>
    {
        public TaxCategoryCreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        }
    }

    public sealed class TaxCategoryEditValidator : AbstractValidator<TaxCategoryEditDto>
    {
        public TaxCategoryEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        }
    }
}
