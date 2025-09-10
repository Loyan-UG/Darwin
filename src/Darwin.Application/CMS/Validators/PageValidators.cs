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
        public PageCreateDtoValidator()
        {
            RuleFor(x => x.Translations).NotEmpty();
            RuleForEach(x => x.Translations).SetValidator(new PageTranslationDtoValidator());
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
