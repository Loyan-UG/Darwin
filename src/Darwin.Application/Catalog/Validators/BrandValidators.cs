using System.Linq;
using Darwin.Application.Catalog.DTOs;
using FluentValidation;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Validation rules for creating a brand (requires at least one translation with Culture+Name).
    /// </summary>
    public sealed class BrandCreateDtoValidator : AbstractValidator<BrandCreateDto>
    {
        public BrandCreateDtoValidator()
        {
            RuleFor(x => x.Translations)
                .NotNull().WithMessage("Translations are required.")
                .Must(t => t.Count > 0).WithMessage("At least one translation is required.");

            RuleForEach(x => x.Translations).ChildRules(tr =>
            {
                tr.RuleFor(t => t.Culture).NotEmpty().MaximumLength(16);
                tr.RuleFor(t => t.Name).NotEmpty().MaximumLength(256);
                // DescriptionHtml length intentionally not limited; sanitized on write.
            });

            RuleFor(x => x.Slug).MaximumLength(256);
        }
    }

    /// <summary>
    /// Validation rules for editing a brand. Mirrors create, and ensures RowVersion presence.
    /// </summary>
    public sealed class BrandEditDtoValidator : AbstractValidator<BrandEditDto>
    {
        public BrandEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();

            RuleFor(x => x.Translations)
                .NotNull().WithMessage("Translations are required.")
                .Must(t => t.Count > 0).WithMessage("At least one translation is required.");

            RuleForEach(x => x.Translations).ChildRules(tr =>
            {
                tr.RuleFor(t => t.Culture).NotEmpty().MaximumLength(16);
                tr.RuleFor(t => t.Name).NotEmpty().MaximumLength(256);
            });

            RuleFor(x => x.Slug).MaximumLength(256);
        }
    }
}
