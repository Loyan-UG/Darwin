using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Enums;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates creation of <see cref="Darwin.Domain.Entities.Loyalty.LoyaltyProgram"/>.
    /// </summary>
    public sealed class LoyaltyProgramCreateValidator : AbstractValidator<LoyaltyProgramCreateDto>
    {
        public LoyaltyProgramCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            RuleFor(x => x.AccrualMode).IsInEnum();

            When(x => x.AccrualMode == LoyaltyAccrualMode.AmountBased, () =>
            {
                RuleFor(x => x.PointsPerCurrencyUnit)
                    .NotNull()
                    .GreaterThan(0);
            });
        }
    }

    /// <summary>
    /// Validates editing of <see cref="Darwin.Domain.Entities.Loyalty.LoyaltyProgram"/>.
    /// </summary>
    public sealed class LoyaltyProgramEditValidator : AbstractValidator<LoyaltyProgramEditDto>
    {
        public LoyaltyProgramEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AccrualMode).IsInEnum();

            When(x => x.AccrualMode == LoyaltyAccrualMode.AmountBased, () =>
            {
                RuleFor(x => x.PointsPerCurrencyUnit)
                    .NotNull()
                    .GreaterThan(0);
            });
        }
    }

    /// <summary>
    /// Validates deletion DTO for loyalty programs.
    /// </summary>
    public sealed class LoyaltyProgramDeleteValidator : AbstractValidator<LoyaltyProgramDeleteDto>
    {
        public LoyaltyProgramDeleteValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
