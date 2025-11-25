using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates requests to suspend a loyalty account.
    /// </summary>
    public sealed class SuspendLoyaltyAccountValidator : AbstractValidator<SuspendLoyaltyAccountDto>
    {
        public SuspendLoyaltyAccountValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();
        }
    }

    /// <summary>
    /// Validates requests to activate a loyalty account.
    /// </summary>
    public sealed class ActivateLoyaltyAccountValidator : AbstractValidator<ActivateLoyaltyAccountDto>
    {
        public ActivateLoyaltyAccountValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();
        }
    }
}
