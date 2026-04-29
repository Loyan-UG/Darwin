using System;
using System.Linq;
using Darwin.Application.Catalog.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

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
        public ProductEditDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion)
                .NotEmpty()
                .WithMessage(localizer["RowVersionRequired"]);
            RuleFor(x => x.Translations).NotEmpty();
            RuleFor(x => x.Variants).NotEmpty();
            RuleFor(x => x.Translations)
                .Must(x => x is not null && x.Select(t => t.Culture?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count);
            RuleFor(x => x.Variants)
                .Must(x => x is not null && x.Select(v => v.Sku?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count);
            RuleForEach(x => x.Translations).SetValidator(new ProductTranslationDtoValidator());
            RuleForEach(x => x.Variants).SetValidator(new ProductVariantCreateDtoValidator());
        }
    }
}
