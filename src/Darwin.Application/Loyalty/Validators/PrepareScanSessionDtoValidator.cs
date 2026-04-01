using System;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Localization;

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
        public PrepareScanSessionDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.BusinessId)
                .NotEqual(Guid.Empty)
                .WithMessage(localizer["BusinessIdRequired"]);

            RuleFor(x => x.Mode)
                .IsInEnum()
                .WithMessage(localizer["ModeInvalid"]);

            When(x => x.Mode == LoyaltyScanMode.Redemption, () =>
            {
                RuleFor(x => x.SelectedRewardTierIds)
                    .NotNull()
                    .WithMessage(localizer["AtLeastOneRewardRequiredForRedemption"])
                    .Must(list => list.Count > 0)
                    .WithMessage(localizer["AtLeastOneRewardRequiredForRedemption"]);

                RuleForEach(x => x.SelectedRewardTierIds)
                    .NotEqual(Guid.Empty)
                    .WithMessage(localizer["RewardTierIdCannotBeEmpty"]);
            });

            RuleFor(x => x.DeviceId)
                .MaximumLength(200)
                .When(x => x.DeviceId != null);
        }
    }
}
