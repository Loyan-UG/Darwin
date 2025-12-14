using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmRedemptionFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmRedemptionFromSessionDtoValidator : AbstractValidator<ConfirmRedemptionFromSessionDto>
    {
        public ConfirmRedemptionFromSessionDtoValidator()
        {
            RuleFor(x => x.ScanSessionToken)
                .NotEmpty()
                .WithMessage("ScanSessionToken is required.")
                .MaximumLength(4000);
        }
    }
}
