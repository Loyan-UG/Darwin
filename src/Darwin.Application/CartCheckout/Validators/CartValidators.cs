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

    /// <summary>
    /// Minimal input validation for adding items to the cart.
    /// Business rules (stock/min/max) are enforced in use cases or services.
    /// </summary>
    public sealed class CartAddItemValidator : AbstractValidator<CartAddItemDto>
    {
        public CartAddItemValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => new { x.UserId, x.AnonymousId })
                .Must(x => x.UserId != null || !string.IsNullOrWhiteSpace(x.AnonymousId))
                .WithMessage("Either UserId or AnonymousId is required.");
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
