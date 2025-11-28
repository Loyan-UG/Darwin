using System;
using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmAccrualFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmAccrualFromSessionDtoValidator : AbstractValidator<ConfirmAccrualFromSessionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmAccrualFromSessionDtoValidator"/> class.
        /// </summary>
        public ConfirmAccrualFromSessionDtoValidator()
        {
            RuleFor(x => x.ScanSessionId)
                .NotEqual(Guid.Empty)
                .WithMessage("ScanSessionId is required.");

            RuleFor(x => x.Points)
                .GreaterThan(0)
                .WithMessage("Points must be a positive integer.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);
        }
    }
}
