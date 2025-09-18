using Darwin.Application.CMS.Media.DTOs;
using FluentValidation;

namespace Darwin.Application.CMS.Media.Validators
{
    /// <summary>
    /// Validation for creating media assets. Ensures URL and basic metadata integrity.
    /// </summary>
    public sealed class MediaAssetCreateValidator : AbstractValidator<MediaAssetCreateDto>
    {
        public MediaAssetCreateValidator()
        {
            RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
            RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Alt).MaximumLength(256);
            RuleFor(x => x.Title).MaximumLength(256).When(x => x.Title != null);
            RuleFor(x => x.SizeBytes).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Width).GreaterThan(0).When(x => x.Width.HasValue);
            RuleFor(x => x.Height).GreaterThan(0).When(x => x.Height.HasValue);
            RuleFor(x => x.Role).MaximumLength(64).When(x => x.Role != null);
        }
    }

    /// <summary>
    /// Validation for editing media assets. File location remains immutable.
    /// </summary>
    public sealed class MediaAssetEditValidator : AbstractValidator<MediaAssetEditDto>
    {
        public MediaAssetEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Alt).MaximumLength(256);
            RuleFor(x => x.Title).MaximumLength(256).When(x => x.Title != null);
            RuleFor(x => x.Role).MaximumLength(64).When(x => x.Role != null);
        }
    }
}
