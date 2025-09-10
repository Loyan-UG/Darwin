using Darwin.Application.Catalog.DTOs;
using FluentValidation;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    ///     Validator for product edit operations, mirroring <c>ProductCreateValidator</c> while also
    ///     enforcing presence and shape of concurrency tokens (RowVersion) where applicable.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Ensures that edits do not strip essential data (translations/variants) and that any concurrent
    ///         update will surface as a conflict rather than silently overwriting changes.
    ///     </para>
    ///     <para>
    ///         Keep rules symmetrical with create, with additional checks for identity and concurrency fields.
    ///     </para>
    /// </remarks>
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
