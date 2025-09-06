using Darwin.Application.CMS.DTOs;
using FluentValidation;

namespace Darwin.Application.CMS.Validators
{
    public sealed class PageTranslationDtoValidator : AbstractValidator<PageTranslationDto>
    {
        public PageTranslationDtoValidator()
        {
            RuleFor(x => x.Culture).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContentHtml).NotNull();
        }
    }

    public sealed class PageCreateDtoValidator : AbstractValidator<PageCreateDto>
    {
        public PageCreateDtoValidator()
        {
            RuleFor(x => x.Translations).NotEmpty();
            RuleForEach(x => x.Translations).SetValidator(new PageTranslationDtoValidator());
            RuleFor(x => x.PublishEndUtc).GreaterThan(x => x.PublishStartUtc)
                .When(x => x.PublishEndUtc.HasValue && x.PublishStartUtc.HasValue);
        }
    }

    public sealed class PageEditDtoValidator : AbstractValidator<PageEditDto>
    {
        public PageEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Translations).NotEmpty();
            RuleForEach(x => x.Translations).SetValidator(new PageTranslationDtoValidator());
            RuleFor(x => x.PublishEndUtc).GreaterThan(x => x.PublishStartUtc)
                .When(x => x.PublishEndUtc.HasValue && x.PublishStartUtc.HasValue);
        }
    }
}
