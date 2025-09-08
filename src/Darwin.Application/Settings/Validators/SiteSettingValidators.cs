using Darwin.Application.Settings.DTOs;
using FluentValidation;

namespace Darwin.Application.Settings.Validators
{
    public sealed class UpdateSiteSettingDtoValidator : AbstractValidator<UpdateSiteSettingDto>
    {
        public UpdateSiteSettingDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

            RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(10);
            RuleFor(x => x.SupportedCulturesCsv)
                .NotEmpty()
                .MaximumLength(1000)
                .Must(csv => csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).Length > 0)
                .WithMessage("Supported cultures must contain at least one item.");
        }
    }
}
