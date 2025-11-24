using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates creation of <see cref="Darwin.Domain.Entities.Loyalty.LoyaltyRewardTier"/>.
    /// </summary>
    public sealed class LoyaltyRewardTierCreateValidator : AbstractValidator<LoyaltyRewardTierCreateDto>
    {
        public LoyaltyRewardTierCreateValidator()
        {
            RuleFor(x => x.LoyaltyProgramId).NotEmpty();
            RuleFor(x => x.PointsRequired).GreaterThan(0);
            RuleFor(x => x.RewardType).IsInEnum();
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    /// <summary>
    /// Validates editing of <see cref="Darwin.Domain.Entities.Loyalty.LoyaltyRewardTier"/>.
    /// </summary>
    public sealed class LoyaltyRewardTierEditValidator : AbstractValidator<LoyaltyRewardTierEditDto>
    {
        public LoyaltyRewardTierEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.LoyaltyProgramId).NotEmpty();
            RuleFor(x => x.PointsRequired).GreaterThan(0);
            RuleFor(x => x.RewardType).IsInEnum();
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    /// <summary>
    /// Validates deletion DTO for reward tiers.
    /// </summary>
    public sealed class LoyaltyRewardTierDeleteValidator : AbstractValidator<LoyaltyRewardTierDeleteDto>
    {
        public LoyaltyRewardTierDeleteValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
