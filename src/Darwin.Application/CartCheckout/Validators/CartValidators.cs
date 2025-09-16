using Darwin.Application.CartCheckout.DTOs;
using FluentValidation;

namespace Darwin.Application.CartCheckout.Validators
{
    public sealed class CartKeyValidator : AbstractValidator<CartKeyDto>
    {
        public CartKeyValidator()
        {
            RuleFor(x => x).Must(k => k.UserId.HasValue || !string.IsNullOrWhiteSpace(k.AnonymousId))
                .WithMessage("Either UserId or AnonymousId must be provided.");
        }
    }

    public sealed class CartAddItemValidator : AbstractValidator<CartAddItemDto>
    {
        public CartAddItemValidator()
        {
            RuleFor(x => x).Must(k => k.UserId.HasValue || !string.IsNullOrWhiteSpace(k.AnonymousId))
                .WithMessage("Either UserId or AnonymousId must be provided.");

            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
            RuleFor(x => x.Currency).NotEmpty().Length(3, 3);
        }
    }

    public sealed class CartUpdateQtyValidator : AbstractValidator<CartUpdateQtyDto>
    {
        public CartUpdateQtyValidator()
        {
            RuleFor(x => x.CartId).NotEmpty();
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0); // 0 => remove
        }
    }

    public sealed class CartRemoveItemValidator : AbstractValidator<CartRemoveItemDto>
    {
        public CartRemoveItemValidator()
        {
            RuleFor(x => x.CartId).NotEmpty();
            RuleFor(x => x.VariantId).NotEmpty();
        }
    }

    public sealed class CartApplyCouponValidator : AbstractValidator<CartApplyCouponDto>
    {
        public CartApplyCouponValidator()
        {
            RuleFor(x => x.CartId).NotEmpty();
            // TODO: Coupon format rule can be added later
        }
    }
}
