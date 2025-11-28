using System;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Enums;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates <see cref="PrepareScanSessionDto"/> input.
    /// </summary>
    public sealed class PrepareScanSessionDtoValidator : AbstractValidator<PrepareScanSessionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareScanSessionDtoValidator"/> class.
        /// </summary>
        public PrepareScanSessionDtoValidator()
        {
            RuleFor(x => x.BusinessId)
                .NotEqual(Guid.Empty)
                .WithMessage("BusinessId is required.");

            RuleFor(x => x.Mode)
                .IsInEnum()
                .WithMessage("Mode is invalid.");

            When(x => x.Mode == LoyaltyScanMode.Redemption, () =>
            {
                RuleFor(x => x.SelectedRewardTierIds)
                    .NotNull()
                    .WithMessage("At least one reward must be selected for redemption.")
                    .Must(list => list.Count > 0)
                    .WithMessage("At least one reward must be selected for redemption.");

                RuleForEach(x => x.SelectedRewardTierIds)
                    .NotEqual(Guid.Empty)
                    .WithMessage("Reward tier id cannot be empty.");
            });

            RuleFor(x => x.DeviceId)
                .MaximumLength(200)
                .When(x => x.DeviceId != null);
        }
    }
}
