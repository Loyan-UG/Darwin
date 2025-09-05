using Darwin.Application.Catalog.DTOs;
using FluentValidation;

namespace Darwin.Application.Catalog.Validators
{
    public sealed class ProductTranslationDtoValidator : AbstractValidator<ProductTranslationDto>
    {
        public ProductTranslationDtoValidator()
        {
            RuleFor(x => x.Culture).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class ProductVariantCreateDtoValidator : AbstractValidator<ProductVariantCreateDto>
    {
        public ProductVariantCreateDtoValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.BasePriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.StockOnHand).GreaterThanOrEqualTo(0);
            RuleFor(x => x.StockReserved).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TaxCategoryId).NotEmpty();
        }
    }

    public sealed class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
    {
        public ProductCreateDtoValidator()
        {
            RuleForEach(x => x.Translations).SetValidator(new ProductTranslationDtoValidator());
            RuleFor(x => x.Translations).NotEmpty().WithMessage("At least one translation is required.");

            RuleForEach(x => x.Variants).SetValidator(new ProductVariantCreateDtoValidator());
            RuleFor(x => x.Variants).NotEmpty().WithMessage("At least one variant is required.");
        }
    }
}
