using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Cart.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Cart.Queries
{
    /// <summary>
    /// Loads a cart with its non-deleted items and projects it to <see cref="CartDto"/>.
    /// </summary>
    public sealed class GetCartForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetCartForEditHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<CartDto?> HandleAsync(Guid cartId, CancellationToken ct = default)
        {
            var cart = await _db.Set<Darwin.Domain.Entities.CartCheckout.Cart>()
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId, ct);

            if (cart == null)
                return null;

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                AnonymousId = cart.AnonymousId,
                Currency = cart.Currency,
                CouponCode = cart.CouponCode,
                RowVersion = cart.RowVersion,
                Items = cart.Items
                    .Where(i => !i.IsDeleted)
                    .Select(i => new CartItemDto
                    {
                        Id = i.Id,
                        VariantId = i.VariantId,
                        Quantity = i.Quantity,
                        UnitPriceNetMinor = i.UnitPriceNetMinor,
                        VatRate = i.VatRate
                    })
                    .ToList()
            };
        }
    }
}
