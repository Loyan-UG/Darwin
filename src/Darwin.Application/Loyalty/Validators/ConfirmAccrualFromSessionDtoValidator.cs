using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmAccrualFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmAccrualFromSessionDtoValidator : AbstractValidator<ConfirmAccrualFromSessionDto>
    {
        public ConfirmAccrualFromSessionDtoValidator()
        {
            RuleFor(x => x.ScanSessionToken)
                .NotEmpty()
                .WithMessage("ScanSessionToken is required.")
                .MaximumLength(4000);

            RuleFor(x => x.Points)
                .GreaterThan(0)
                .WithMessage("Points must be a positive integer.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);
        }
    }
}
