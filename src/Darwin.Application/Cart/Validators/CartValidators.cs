using Darwin.Application.Cart.DTOs;
using FluentValidation;
using System.Linq;
using System.Text.RegularExpressions;

namespace Darwin.Application.Cart.Validators
{
    /// <summary>
    /// Validates a cart item (used within CartCreateDto and updates).
    /// </summary>
    public sealed class CartItemDtoValidator : AbstractValidator<CartItemDto>
    {
        public CartItemDtoValidator()
        {
            RuleFor(x => x.VariantId).NotEqual(Guid.Empty);
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero");
            RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m).WithMessage("VatRate must be between 0 and 1");
        }
    }

    /// <summary>
    /// Validates creation of a cart with optional items.
    /// </summary>
    public sealed class CartCreateDtoValidator : AbstractValidator<CartCreateDto>
    {
        public CartCreateDtoValidator()
        {
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleForEach(x => x.Items).SetValidator(new CartItemDtoValidator());

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.VariantId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate variants are not allowed in initial cart");
        }
    }

    /// <summary>
    /// Validates adding an item to an existing cart.
    /// </summary>
    public sealed class AddCartItemDtoValidator : AbstractValidator<AddCartItemDto>
    {
        public AddCartItemDtoValidator()
        {
            RuleFor(x => x.CartId).NotEqual(Guid.Empty);
            RuleFor(x => x.VariantId).NotEqual(Guid.Empty);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        }
    }

    /// <summary>
    /// Validates updating an existing cart item.
    /// </summary>
    public sealed class UpdateCartItemDtoValidator : AbstractValidator<UpdateCartItemDto>
    {
        public UpdateCartItemDtoValidator()
        {
            RuleFor(x => x.CartId).NotEqual(Guid.Empty);
            RuleFor(x => x.ItemId).NotEqual(Guid.Empty);
            RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length > 0);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        }
    }

    /// <summary>
    /// Validates removal of a cart item.
    /// </summary>
    public sealed class RemoveCartItemDtoValidator : AbstractValidator<RemoveCartItemDto>
    {
        public RemoveCartItemDtoValidator()
        {
            RuleFor(x => x.CartId).NotEqual(Guid.Empty);
            RuleFor(x => x.ItemId).NotEqual(Guid.Empty);
            RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length > 0);
        }
    }
}
