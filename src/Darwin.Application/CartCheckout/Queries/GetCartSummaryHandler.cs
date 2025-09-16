using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Queries
{
    /// <summary>
    /// Resolves a cart by (UserId or AnonymousId) and computes line totals and cart totals in minor units.
    /// </summary>
    public sealed class GetCartSummaryHandler
    {
        private readonly IAppDbContext _db;
        public GetCartSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<CartSummaryDto?> HandleAsync(CartKeyDto key, CancellationToken ct = default)
        {
            // Resolve cart
            var query = _db.Set<Cart>().AsNoTracking().Include(c => c.Items).AsQueryable();

            if (key.UserId.HasValue)
                query = query.Where(c => c.UserId == key.UserId);
            else if (!string.IsNullOrWhiteSpace(key.AnonymousId))
                query = query.Where(c => c.AnonymousId == key.AnonymousId);
            else
                return null;

            var cart = await query.FirstOrDefaultAsync(ct);
            if (cart is null) return null;

            long subtotalNet = 0;
            long vatTotal = 0;

            var items = cart.Items.Select(i =>
            {
                var lineNet = checked(i.UnitPriceNetMinor * i.Quantity);
                // VAT = round(lineNet * rate) — integer rounding rule can be tuned later (banker's rounding, etc.)
                var lineVat = (long)Math.Round(lineNet * (double)i.VatRate, MidpointRounding.AwayFromZero);
                var lineGross = checked(lineNet + lineVat);

                subtotalNet += lineNet;
                vatTotal += lineVat;

                return new CartItemRowDto
                {
                    VariantId = i.VariantId,
                    Quantity = i.Quantity,
                    UnitPriceNetMinor = i.UnitPriceNetMinor,
                    VatRate = i.VatRate,
                    LineNetMinor = lineNet,
                    LineVatMinor = lineVat,
                    LineGrossMinor = lineGross
                };
            }).ToList();

            return new CartSummaryDto
            {
                CartId = cart.Id,
                Currency = cart.Currency,
                Items = items,
                SubtotalNetMinor = subtotalNet,
                VatTotalMinor = vatTotal,
                GrandTotalGrossMinor = checked(subtotalNet + vatTotal),
                CouponCode = cart.CouponCode
            };
        }
    }
}
