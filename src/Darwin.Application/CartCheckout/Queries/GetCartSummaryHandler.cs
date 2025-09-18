using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Queries
{
    /// <summary>
    /// Returns a computed summary of a cart by either UserId or AnonymousId.
    /// Uses stored snapshots (unit net, VAT, add-on deltas) to produce totals.
    /// </summary>
    public sealed class GetCartSummaryHandler
    {
        private readonly IAppDbContext _db;
        public GetCartSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<CartSummaryDto?> HandleAsync(Guid? userId, string? anonId, CancellationToken ct = default)
        {
            var cart = await _db.Set<Cart>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c =>
                    !c.IsDeleted &&
                    ((userId != null && c.UserId == userId) ||
                     (userId == null && c.AnonymousId == anonId)),
                    ct);

            if (cart == null) return null;

            var rows = new List<CartItemRowDto>();
            long subtotalNet = 0;
            long vatTotal = 0;

            foreach (var i in cart.Items.Where(x => !x.IsDeleted))
            {
                var unitNetPlusAddOn = i.UnitPriceNetMinor + i.AddOnPriceDeltaMinor;
                var lineNet = unitNetPlusAddOn * i.Quantity;

                // VAT is computed from snapshot vat rate for simplicity:
                var lineVat = (long)Math.Round(lineNet * (double)i.VatRate, MidpointRounding.AwayFromZero);
                var lineGross = lineNet + lineVat;

                rows.Add(new CartItemRowDto
                {
                    VariantId = i.VariantId,
                    Quantity = i.Quantity,
                    UnitPriceNetMinor = i.UnitPriceNetMinor,
                    AddOnPriceDeltaMinor = i.AddOnPriceDeltaMinor,
                    VatRate = i.VatRate,
                    LineNetMinor = lineNet,
                    LineVatMinor = lineVat,
                    LineGrossMinor = lineGross,
                    SelectedAddOnValueIdsJson = i.SelectedAddOnValueIdsJson
                });

                subtotalNet += lineNet;
                vatTotal += lineVat;
            }

            return new CartSummaryDto
            {
                CartId = cart.Id,
                Currency = cart.Currency,
                Items = rows,
                SubtotalNetMinor = subtotalNet,
                VatTotalMinor = vatTotal,
                GrandTotalGrossMinor = subtotalNet + vatTotal,
                CouponCode = cart.CouponCode
            };
        }
    }
}
