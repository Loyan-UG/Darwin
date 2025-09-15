using Darwin.Application.CMS.DTOs;
using FluentValidation;

namespace Darwin.Application.CMS.Validators
{
    /// <summary>
    /// Validation for creating a menu with items and per-culture labels.
    /// </summary>
    public sealed class MenuCreateDtoValidator : AbstractValidator<MenuCreateDto>
    {
        public MenuCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleForEach(x => x.Items).SetValidator(new MenuItemDtoValidator());
        }
    }

    /// <summary>
    /// Validation for editing a menu (replace items strategy).
    /// </summary>
    public sealed class MenuEditDtoValidator : AbstractValidator<MenuEditDto>
    {
        public MenuEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleForEach(x => x.Items).SetValidator(new MenuItemDtoValidator());
        }
    }

    /// <summary>
    /// Validation for a single menu item with translations.
    /// </summary>
    public sealed class MenuItemDtoValidator : AbstractValidator<MenuItemDto>
    {
        public MenuItemDtoValidator()
        {
            RuleFor(i => i.Url).NotEmpty().MaximumLength(1024);
            RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
            RuleFor(i => i.Translations)
                .NotNull().WithMessage("At least one translation is required.")
                .Must(t => t.Count > 0).WithMessage("At least one translation is required.");

            RuleForEach(i => i.Translations).ChildRules(tr =>
            {
                tr.RuleFor(t => t.Culture).NotEmpty().MaximumLength(16);
                tr.RuleFor(t => t.Label).NotEmpty().MaximumLength(256);
            });
        }
    }
}
