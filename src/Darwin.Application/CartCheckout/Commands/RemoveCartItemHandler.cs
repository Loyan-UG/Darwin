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
    /// Removes a variant line from the cart.
    /// </summary>
    public sealed class RemoveCartItemHandler
    {
        private readonly IAppDbContext _db;
        private readonly CartRemoveItemValidator _validator = new();

        public RemoveCartItemHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartRemoveItemDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new FluentValidation.ValidationException(val.Errors);

            var cart = await _db.Set<Cart>().Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == dto.CartId, ct);

            if (cart is null) throw new InvalidOperationException("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId);
            if (item is null) return;

            cart.Items.Remove(item);
            await _db.SaveChangesAsync(ct);
        }
    }
}
