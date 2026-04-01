using Darwin.Application.Loyalty.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmAccrualFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmAccrualFromSessionDtoValidator : AbstractValidator<ConfirmAccrualFromSessionDto>
    {
        public ConfirmAccrualFromSessionDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.ScanSessionToken)
                .NotEmpty()
                .WithMessage(localizer["ScanSessionTokenRequired"])
                .MaximumLength(4000);

            RuleFor(x => x.Points)
                .GreaterThan(0)
                .WithMessage(localizer["PointsPositiveInteger"]);

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);
        }
    }
}
