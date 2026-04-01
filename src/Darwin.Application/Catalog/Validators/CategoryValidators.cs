using Darwin.Application.Catalog.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Catalog.Validators
{
    public sealed class CategoryTranslationDtoValidator : AbstractValidator<CategoryTranslationDto>
    {
        public CategoryTranslationDtoValidator()
        {
            RuleFor(x => x.Culture).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
    {
        public CategoryCreateDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleForEach(x => x.Translations).SetValidator(new CategoryTranslationDtoValidator());
            RuleFor(x => x.Translations).NotEmpty().WithMessage(localizer["AtLeastOneTranslationRequired"]);
        }
    }

    public sealed class CategoryEditDtoValidator : AbstractValidator<CategoryEditDto>
    {
        public CategoryEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleForEach(x => x.Translations).SetValidator(new CategoryTranslationDtoValidator());
        }
    }
}
