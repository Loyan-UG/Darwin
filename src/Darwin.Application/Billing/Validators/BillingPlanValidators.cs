using Darwin.Application.Billing.DTOs;
using FluentValidation;

namespace Darwin.Application.Billing.Validators;

public sealed class BillingPlanCreateValidator : AbstractValidator<BillingPlanCreateDto>
{
    public BillingPlanCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.PriceMinor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Interval).IsInEnum();
        RuleFor(x => x.IntervalCount).GreaterThan(0);
        RuleFor(x => x.TrialDays).GreaterThanOrEqualTo(0).When(x => x.TrialDays.HasValue);
        RuleFor(x => x.FeaturesJson).NotEmpty().MaximumLength(4000);
    }
}

public sealed class BillingPlanEditValidator : AbstractValidator<BillingPlanEditDto>
{
    public BillingPlanEditValidator()
    {
        Include(new BillingPlanCreateValidator());
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
