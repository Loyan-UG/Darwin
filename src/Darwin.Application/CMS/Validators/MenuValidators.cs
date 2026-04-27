using System;
using Darwin.Application.CMS.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Validators
{
    /// <summary>
    /// Validation for creating a menu with items and per-culture labels.
    /// </summary>
    public sealed class MenuCreateDtoValidator : AbstractValidator<MenuCreateDto>
    {
        public MenuCreateDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleForEach(x => x.Items).SetValidator(new MenuItemDtoValidator(localizer));
        }
    }

    /// <summary>
    /// Validation for editing a menu.
    /// </summary>
    public sealed class MenuEditDtoValidator : AbstractValidator<MenuEditDto>
    {
        public MenuEditDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleForEach(x => x.Items).SetValidator(new MenuItemDtoValidator(localizer));
        }
    }

    /// <summary>
    /// Validation for a single menu item with translations.
    /// </summary>
    public sealed class MenuItemDtoValidator : AbstractValidator<MenuItemDto>
    {
        public MenuItemDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(i => i.Url)
                .NotEmpty()
                .MaximumLength(1024)
                .Must(BeSupportedMenuUrl)
                .WithMessage(localizer["MenuUrlMustBeSupported"]);

            RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
            RuleFor(i => i.Translations)
                .NotNull().WithMessage(localizer["AtLeastOneTranslationRequired"])
                .Must(t => t.Count > 0).WithMessage(localizer["AtLeastOneTranslationRequired"]);

            RuleForEach(i => i.Translations).ChildRules(tr =>
            {
                tr.RuleFor(t => t.Culture).NotEmpty().MaximumLength(16);
                tr.RuleFor(t => t.Label).NotEmpty().MaximumLength(256);
                tr.RuleFor(t => t.Url)
                    .MaximumLength(1024)
                    .Must(url => string.IsNullOrWhiteSpace(url) || BeSupportedMenuUrl(url))
                    .WithMessage(localizer["MenuUrlMustBeSupported"]);
            });
        }

        private static bool BeSupportedMenuUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var candidate = url.Trim();
            if (candidate.StartsWith("/", StringComparison.Ordinal))
            {
                return candidate.Length == 1 || !candidate.StartsWith("//", StringComparison.Ordinal);
            }

            return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
