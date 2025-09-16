using Darwin.Application.Pricing.DTOs;
using Darwin.Domain.Enums;
using FluentValidation;

namespace Darwin.Application.Pricing.Validators
{
    public sealed class PromotionCreateValidator : AbstractValidator<PromotionCreateDto>
    {
        public PromotionCreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Currency).NotEmpty().Length(3);

            RuleFor(x => x.Percent)
                .InclusiveBetween(0m, 100m)
                .When(x => x.Type == PromotionType.Percentage);

            RuleFor(x => x.AmountMinor)
                .GreaterThan(0)
                .When(x => x.Type == PromotionType.Amount);

            RuleFor(x => x.Code)
                .MaximumLength(64)
                .Matches("^[A-Za-z0-9_-]+$")
                .When(x => !string.IsNullOrWhiteSpace(x.Code));

            RuleFor(x => x.EndsAtUtc)
                .GreaterThan(x => x.StartsAtUtc!.Value)
                .When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue);
        }
    }

    public sealed class PromotionEditValidator : AbstractValidator<PromotionEditDto>
    {
        public PromotionEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Currency).NotEmpty().Length(3);

            RuleFor(x => x.Percent)
                .InclusiveBetween(0m, 100m)
                .When(x => x.Type == PromotionType.Percentage);

            RuleFor(x => x.AmountMinor)
                .GreaterThan(0)
                .When(x => x.Type == PromotionType.Amount);

            RuleFor(x => x.Code)
                .MaximumLength(64)
                .Matches("^[A-Za-z0-9_-]+$")
                .When(x => !string.IsNullOrWhiteSpace(x.Code));

            RuleFor(x => x.EndsAtUtc)
                .GreaterThan(x => x.StartsAtUtc!.Value)
                .When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue);
        }
    }

    public sealed class ValidateCouponInputValidator : AbstractValidator<ValidateCouponInputDto>
    {
        public ValidateCouponInputValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
            RuleFor(x => x.SubtotalNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }
}
