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
    /// Updates quantity for a specific variant in a cart. If quantity becomes 0, removes the item.
    /// </summary>
    public sealed class UpdateCartItemQuantityHandler
    {
        private readonly IAppDbContext _db;
        private readonly CartUpdateQtyValidator _validator = new();

        public UpdateCartItemQuantityHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartUpdateQtyDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new FluentValidation.ValidationException(val.Errors);

            var cart = await _db.Set<Cart>().Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == dto.CartId, ct);

            if (cart is null) throw new InvalidOperationException("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId);
            if (item is null) return; // idempotent

            if (dto.Quantity <= 0)
                cart.Items.Remove(item);
            else
                item.Quantity = dto.Quantity;

            await _db.SaveChangesAsync(ct);
        }
    }
}
