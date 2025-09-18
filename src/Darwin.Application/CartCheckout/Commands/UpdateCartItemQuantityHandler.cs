using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Updates the quantity of a specific cart line identified by (CartId, VariantId, SelectedAddOnValueIdsJson).
    /// If Quantity == 0, the line will be soft-deleted (removed from the cart).
    /// </summary>
    public sealed class UpdateCartItemQuantityHandler
    {
        private readonly IAppDbContext _db;
        public UpdateCartItemQuantityHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartUpdateQtyDto dto, CancellationToken ct = default)
        {
            var line = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(i =>
                    !i.IsDeleted &&
                    i.CartId == dto.CartId &&
                    i.VariantId == dto.VariantId &&
                    (dto.SelectedAddOnValueIdsJson == null || i.SelectedAddOnValueIdsJson == dto.SelectedAddOnValueIdsJson),
                    ct);

            if (line == null) throw new InvalidOperationException("Cart line not found.");

            if (dto.Quantity == 0)
            {
                line.IsDeleted = true; // remove line
            }
            else if (dto.Quantity > 0)
            {
                line.Quantity = dto.Quantity;
            }
            else
            {
                throw new InvalidOperationException("Quantity must be zero or positive.");
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
