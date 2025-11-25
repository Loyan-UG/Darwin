using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates requests to adjust loyalty points on an account.
    /// Enforces basic invariants such as non-empty identifiers, non-zero delta,
    /// and reasonable length of textual fields.
    /// </summary>
    public sealed class AdjustLoyaltyPointsValidator : AbstractValidator<AdjustLoyaltyPointsDto>
    {
        public AdjustLoyaltyPointsValidator()
        {
            RuleFor(x => x.LoyaltyAccountId)
                .NotEmpty();

            RuleFor(x => x.BusinessId)
                .NotEmpty();

            RuleFor(x => x.PointsDelta)
                .NotEqual(0)
                .WithMessage("PointsDelta must not be zero.");

            RuleFor(x => x.Reason)
                .MaximumLength(1000);

            RuleFor(x => x.Reference)
                .MaximumLength(200);

            When(x => x.PointsDelta < 0, () =>
            {
                RuleFor(x => x.Reason)
                    .NotEmpty()
                    .WithMessage("Reason is required when subtracting points.");
            });
        }
    }
}
