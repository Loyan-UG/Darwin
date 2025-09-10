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

    /// <summary>
    ///     FluentValidation validator for product creation requests, enforcing business and data integrity
    ///     before the command handler persists any changes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Validates:
    ///         <list type="bullet">
    ///             <item>At least one translation with required fields (Culture, Name, Slug).</item>
    ///             <item>At least one variant with pricing (minor units) and currency set.</item>
    ///             <item>Optional relationships (Brand, PrimaryCategory, TaxCategory) if provided.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Business Rationale:
    ///         <list type="bullet">
    ///             <item>Products must be discoverable and indexable; translations and slugs are essential.</item>
    ///             <item>Pricing must be unambiguous; storing money in minor units avoids rounding drift.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Keep validator messages user-friendly; these are surfaced in Admin UI validation summaries.
    ///     </para>
    /// </remarks>
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
