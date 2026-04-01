using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Updates the quantity of a specific cart line identified by (CartId, VariantId, SelectedAddOnValueIdsJson).
    /// If Quantity == 0, the line will be soft-deleted (removed from the cart).
    /// </summary>
    public sealed class UpdateCartItemQuantityHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        public UpdateCartItemQuantityHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(CartUpdateQtyDto dto, CancellationToken ct = default)
        {
            var line = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(i =>
                    !i.IsDeleted &&
                    i.CartId == dto.CartId &&
                    i.VariantId == dto.VariantId &&
                    (dto.SelectedAddOnValueIdsJson == null || i.SelectedAddOnValueIdsJson == dto.SelectedAddOnValueIdsJson),
                    ct);

            if (line == null) throw new InvalidOperationException(_localizer["CartLineNotFound"]);

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
                throw new InvalidOperationException(_localizer["QuantityMustBeZeroOrPositive"]);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
