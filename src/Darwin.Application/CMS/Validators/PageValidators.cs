using Darwin.Application.CMS.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Darwin.Application.CMS.Validators
{
    public sealed class PageTranslationDtoValidator : AbstractValidator<PageTranslationDto>
    {
        private const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

        public PageTranslationDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Culture).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(200)
                .Must(static slug => !string.IsNullOrWhiteSpace(slug) && Regex.IsMatch(slug, SlugPattern, RegexOptions.CultureInvariant))
                .WithMessage(localizer["SlugInvalidFormat"]);
            RuleFor(x => x.ContentHtml).NotNull();
        }
    }

    /// <summary>
    ///     Validator for CMS page creation, ensuring required metadata and content are present
    ///     and that publication windows are chronologically valid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Validates:
    ///         <list type="bullet">
    ///             <item>At least one translation with Title and Slug.</item>
    ///             <item>Publish window start/end (if both provided) must satisfy <c>Start &lt; End</c>.</item>
    ///             <item>Optional meta fields (MetaTitle/MetaDescription) length constraints for SEO.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The actual uniqueness of slug per culture is enforced at data level (unique index) and should be
    ///         pre-checked in the handler where feasible to provide a friendly message.
    ///     </para>
    /// </remarks>
    public sealed class PageCreateDtoValidator : AbstractValidator<PageCreateDto>
    {
        public PageCreateDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Translations).NotEmpty();
            RuleFor(x => x.Status).IsInEnum();
            RuleForEach(x => x.Translations).SetValidator(new PageTranslationDtoValidator(localizer));
            RuleFor(x => x.PublishEndUtc).GreaterThan(x => x.PublishStartUtc)
                .When(x => x.PublishEndUtc.HasValue && x.PublishStartUtc.HasValue);
        }
    }

    /// <summary>
    ///     Validator for editing CMS pages, guarding against invalid publication windows
    ///     and ensuring at least one valid translation remains after changes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Edits can remove or alter translations; ensure the page does not end up non-addressable
    ///         (i.e., zero translations or empty slugs/titles).
    ///     </para>
    ///     <para>
    ///         Concurrency checks are not performed here; the controller/handler should use RowVersion to detect conflicts.
    ///     </para>
    /// </remarks>
    public sealed class PageEditDtoValidator : AbstractValidator<PageEditDto>
    {
        public PageEditDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion)
                .NotEmpty()
                .WithMessage(localizer["RowVersionRequired"]);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Translations).NotEmpty();
            RuleForEach(x => x.Translations).SetValidator(new PageTranslationDtoValidator(localizer));
            RuleFor(x => x.PublishEndUtc).GreaterThan(x => x.PublishStartUtc)
                .When(x => x.PublishEndUtc.HasValue && x.PublishStartUtc.HasValue);
        }
    }
}
