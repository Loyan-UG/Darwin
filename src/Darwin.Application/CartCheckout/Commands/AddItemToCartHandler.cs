using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.CartCheckout.Validators;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Adds an item to a cart (or increases quantity) for a user/anonymous key.
    /// Creates the cart if it does not exist yet.
    /// </summary>
    public sealed class AddItemToCartHandler
    {
        private readonly IAppDbContext _db;
        private readonly CartAddItemValidator _validator = new();

        public AddItemToCartHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartAddItemDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new FluentValidation.ValidationException(val.Errors);

            // load or create cart
            var cartQuery = _db.Set<Cart>().Include(c => c.Items).AsQueryable();
            if (dto.UserId.HasValue)
                cartQuery = cartQuery.Where(c => c.UserId == dto.UserId);
            else
                cartQuery = cartQuery.Where(c => c.AnonymousId == dto.AnonymousId);

            var cart = await cartQuery.FirstOrDefaultAsync(ct);
            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = dto.UserId,
                    AnonymousId = dto.AnonymousId,
                    Currency = dto.Currency
                };
                _db.Set<Cart>().Add(cart);
            }
            else
            {
                // optional: ensure currency consistency if cart exists
                cart.Currency = dto.Currency;
            }

            var existing = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId);
            if (existing is null)
            {
                cart.Items.Add(new CartItem
                {
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    UnitPriceNetMinor = dto.UnitPriceNetMinor,
                    VatRate = dto.VatRate
                });
            }
            else
            {
                // increase quantity; snapshot price remains as initially stored
                existing.Quantity = checked(existing.Quantity + dto.Quantity);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
