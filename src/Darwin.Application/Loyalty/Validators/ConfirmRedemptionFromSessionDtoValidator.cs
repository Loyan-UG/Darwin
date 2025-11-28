using System;
using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="ConfirmRedemptionFromSessionDto"/> input.
    /// </summary>
    public sealed class ConfirmRedemptionFromSessionDtoValidator : AbstractValidator<ConfirmRedemptionFromSessionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmRedemptionFromSessionDtoValidator"/> class.
        /// </summary>
        public ConfirmRedemptionFromSessionDtoValidator()
        {
            RuleFor(x => x.ScanSessionId)
                .NotEqual(Guid.Empty)
                .WithMessage("ScanSessionId is required.");
        }
    }
}
