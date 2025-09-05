using Darwin.Application.Catalog.DTOs;
using FluentValidation;

namespace Darwin.Application.Catalog.Validators
{
    public sealed class ProductEditDtoValidator : AbstractValidator<ProductEditDto>
    {
        public ProductEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleForEach(x => x.Translations).SetValidator(new ProductTranslationDtoValidator());
            RuleForEach(x => x.Variants).SetValidator(new ProductVariantCreateDtoValidator());
        }
    }
}
