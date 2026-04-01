using Darwin.Application.Loyalty.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmRedemptionFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmRedemptionFromSessionDtoValidator : AbstractValidator<ConfirmRedemptionFromSessionDto>
    {
        public ConfirmRedemptionFromSessionDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.ScanSessionToken)
                .NotEmpty()
                .WithMessage(localizer["ScanSessionTokenRequired"])
                .MaximumLength(4000);
        }
    }
}
