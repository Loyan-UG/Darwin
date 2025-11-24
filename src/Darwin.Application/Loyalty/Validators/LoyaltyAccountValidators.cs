using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validator for creating loyalty accounts.
    /// </summary>
    public sealed class LoyaltyAccountCreateDtoValidator : AbstractValidator<LoyaltyAccountCreateDto>
    {
        public LoyaltyAccountCreateDtoValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.ConsumerUserId).NotEmpty();
            RuleFor(x => x.LoyaltyProgramId).NotEmpty();

            RuleFor(x => x.CurrentPoints).GreaterThanOrEqualTo(0);
            RuleFor(x => x.LifetimePoints).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Validator for editing loyalty accounts.
    /// </summary>
    public sealed class LoyaltyAccountEditDtoValidator : AbstractValidator<LoyaltyAccountEditDto>
    {
        public LoyaltyAccountEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.ConsumerUserId).NotEmpty();
            RuleFor(x => x.LoyaltyProgramId).NotEmpty();

            RuleFor(x => x.CurrentPoints).GreaterThanOrEqualTo(0);
            RuleFor(x => x.LifetimePoints).GreaterThanOrEqualTo(0);
        }
    }
}
