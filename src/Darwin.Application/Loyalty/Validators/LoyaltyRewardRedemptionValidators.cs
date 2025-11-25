using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates requests to confirm a pending loyalty reward redemption.
    /// The rules are intentionally simple; more complex cross-entity checks
    /// are performed inside the command handler.
    ///
    /// AI usage guidance:
    ///  - Use this validator when building a pipeline or orchestrator that
    ///    executes ConfirmLoyaltyRewardRedemptionHandler.
    ///  - Do not re-implement business rules here; the handler is responsible
    ///    for checking account status, balances, and redemption status.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionValidator : AbstractValidator<ConfirmLoyaltyRewardRedemptionDto>
    {
        public ConfirmLoyaltyRewardRedemptionValidator()
        {
            RuleFor(x => x.RedemptionId)
                .NotEmpty();

            RuleFor(x => x.BusinessId)
                .NotEmpty();
        }
    }
}
