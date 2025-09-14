using Darwin.Application.Catalog.DTOs;
using FluentValidation;
using System.Linq;
using System.Text.RegularExpressions;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Validates translation objects for Brand.
    /// </summary>
    public sealed class BrandTranslationDtoValidator : AbstractValidator<BrandTranslationDto>
    {
        public BrandTranslationDtoValidator()
        {
            RuleFor(t => t.Culture)
                .NotEmpty()
                .Must(IsValidCulture).WithMessage("Culture must be in format ll-CC, e.g. de-DE");
            RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
            RuleFor(t => t.Slug).NotEmpty().MaximumLength(200);
            RuleFor(t => t.Description).MaximumLength(1000);
            RuleFor(t => t.MetaTitle).MaximumLength(100);
            RuleFor(t => t.MetaDescription).MaximumLength(200);
        }

        private static bool IsValidCulture(string c)
            => Regex.IsMatch(c, "^[a-z]{2}-[A-Z]{2}$");
    }

    /// <summary>
    /// Validates the create DTO for Brand.
    /// </summary>
    public sealed class BrandCreateDtoValidator : AbstractValidator<BrandCreateDto>
    {
        public BrandCreateDtoValidator()
        {
            RuleFor(x => x.Translations).NotEmpty().WithMessage("At least one translation is required");
            RuleForEach(x => x.Translations).SetValidator(new BrandTranslationDtoValidator());
            RuleFor(x => x.Translations)
                .Must(t => t.Select(tr => tr.Culture).Distinct().Count() == t.Count)
                .WithMessage("Duplicate cultures are not allowed in translations");
        }
    }

    /// <summary>
    /// Validates the edit DTO for Brand, including concurrency.
    /// </summary>
    public sealed class BrandEditDtoValidator : AbstractValidator<BrandEditDto>
    {
        public BrandEditDtoValidator()
        {
            RuleFor(x => x.RowVersion).NotNull().Must(v => v.Length > 0);
            RuleFor(x => x.Translations).NotEmpty();
            RuleForEach(x => x.Translations).SetValidator(new BrandTranslationDtoValidator());
            RuleFor(x => x.Translations)
                .Must(t => t.Select(tr => tr.Culture).Distinct().Count() == t.Count)
                .WithMessage("Duplicate cultures are not allowed in translations");
        }
    }
}
