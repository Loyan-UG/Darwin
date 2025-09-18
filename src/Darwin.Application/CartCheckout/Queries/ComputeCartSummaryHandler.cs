using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.CartCheckout.Queries
{
    /// <summary>
    /// Computes a cart summary including:
    /// - Base variant net prices (authoritative from catalog)
    /// - Add-on value price deltas (net, minor units)
    /// - VAT per line from the variant's TaxCategory
    /// - Promotion discount (percentage or fixed amount)
    /// 
    /// Assumptions:
    /// - Single currency per cart (phase 1), typically EUR; we validate currency consistency.
    /// - Each CartItem may have SelectedAddOnValueIdsJson holding selected add-on value IDs (JSON array of Guid).
    /// - Server is the source of truth for prices; snapshots on cart items are advisory.
    /// </summary>
    public sealed class ComputeCartSummaryHandler
    {
        private readonly IAppDbContext _db;

        public ComputeCartSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<CartSummaryDto> HandleAsync(Guid cartId, CancellationToken ct = default)
        {
            var cart = await _db.Set<Cart>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cartId && !c.IsDeleted, ct)
                ?? throw new InvalidOperationException("Cart not found.");

            // Load active lines
            var lines = await _db.Set<CartItem>()
                .AsNoTracking()
                .Where(i => i.CartId == cart.Id && !i.IsDeleted && i.Quantity > 0)
                .ToListAsync(ct);

            var result = new CartSummaryDto
            {
                CartId = cart.Id,
                Currency = cart.Currency,
                CouponCode = cart.CouponCode
            };

            if (lines.Count == 0)
                return result;

            // Collect variant ids
            var variantIds = lines.Select(l => l.VariantId).Distinct().ToList();

            // Load variants with tax info
            var variants = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
                .Select(v => new
                {
                    v.Id,
                    v.BasePriceNetMinor,
                    v.Currency,
                    v.TaxCategoryId
                })
                .ToListAsync(ct);

            if (variants.Count != variantIds.Count)
                throw new InvalidOperationException("Some variants were not found or are deleted.");

            // Currency consistency (phase 1: single currency across lines)
            var distinctVariantCurrencies = variants.Select(v => v.Currency).Distinct().ToList();
            if (distinctVariantCurrencies.Count > 1)
                throw new InvalidOperationException("Mixed currencies in a single cart are not supported.");
            if (!string.Equals(distinctVariantCurrencies[0], cart.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cart currency differs from variant currency.");

            // Load tax categories for VAT
            var taxIds = variants.Select(v => v.TaxCategoryId).Distinct().ToList();
            var taxCats = await _db.Set<TaxCategory>()
                .AsNoTracking()
                .Where(t => taxIds.Contains(t.Id) && !t.IsDeleted)
                .Select(t => new { t.Id, t.VatRate })
                .ToListAsync(ct);

            // Helper map
            var variantById = variants.ToDictionary(v => v.Id);
            var vatByTaxId = taxCats.ToDictionary(t => t.Id, t => t.VatRate);

            // Add-on price deltas
            var allSelectedAddOnValueIds = new HashSet<Guid>();
            foreach (var l in lines)
            {
                if (!string.IsNullOrWhiteSpace(l.SelectedAddOnValueIdsJson))
                {
                    try
                    {
                        var ids = JsonSerializer.Deserialize<List<Guid>>(l.SelectedAddOnValueIdsJson) ?? new();
                        foreach (var g in ids) allSelectedAddOnValueIds.Add(g);
                    }
                    catch
                    {
                        // If malformed, treat as empty; alternatively, you can throw
                    }
                }
            }

            var addOnValues = allSelectedAddOnValueIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<AddOnOptionValue>()
                    .AsNoTracking()
                    .Where(v => allSelectedAddOnValueIds.Contains(v.Id) && !v.IsDeleted && v.IsActive)
                    .Select(v => new { v.Id, v.PriceDeltaMinor })
                    .ToDictionaryAsync(v => v.Id, v => v.PriceDeltaMinor, ct);

            long subtotalNet = 0;
            long vatTotal = 0;

            foreach (var line in lines)
            {
                var v = variantById[line.VariantId];

                // Sum add-on deltas for this line
                long addOnDelta = 0;
                string addOnJson = line.SelectedAddOnValueIdsJson ?? "[]";
                try
                {
                    var ids = JsonSerializer.Deserialize<List<Guid>>(addOnJson) ?? new();
                    foreach (var id in ids)
                        if (addOnValues.TryGetValue(id, out var delta)) addOnDelta += delta;
                }
                catch { /* ignore, already defensive above */ }

                var unitNet = v.BasePriceNetMinor + addOnDelta;

                if (!vatByTaxId.TryGetValue(v.TaxCategoryId, out var vatRate))
                    throw new InvalidOperationException("Tax category missing for variant.");

                // Compute line totals
                var quantity = line.Quantity;
                var lineNet = unitNet * quantity;

                // VAT = round(net * rate)
                // We keep integer minor-units; rounding strategy can be refined later if needed.
                var lineVat = (long)Math.Round(lineNet * (double)vatRate, MidpointRounding.AwayFromZero);
                var lineGross = lineNet + lineVat;

                result.Items.Add(new CartItemRowDto
                {
                    VariantId = line.VariantId,
                    Quantity = quantity,
                    UnitPriceNetMinor = unitNet,
                    AddOnPriceDeltaMinor = addOnDelta,
                    VatRate = vatRate,
                    LineNetMinor = lineNet,
                    LineVatMinor = lineVat,
                    LineGrossMinor = lineGross,
                    SelectedAddOnValueIdsJson = addOnJson
                });

                subtotalNet += lineNet;
                vatTotal += lineVat;
            }

            long discountMinor = 0;

            // Apply promotion if any
            if (!string.IsNullOrWhiteSpace(cart.CouponCode))
            {
                var now = DateTime.UtcNow;
                var promo = await _db.Set<Promotion>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        !p.IsDeleted &&
                        p.IsActive &&
                        p.Code == cart.CouponCode &&
                        (p.StartsAtUtc == null || p.StartsAtUtc <= now) &&
                        (p.EndsAtUtc == null || p.EndsAtUtc >= now), ct);

                if (promo != null)
                {
                    // Validate currency match for amount-based rewards
                    if (promo.Type == PromotionType.Amount)
                    {
                        if (!string.Equals(promo.Currency, result.Currency, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Promotion currency does not match cart currency.");
                    }

                    // Min subtotal net gate
                    if (!promo.MinSubtotalNetMinor.HasValue || subtotalNet >= promo.MinSubtotalNetMinor.Value)
                    {
                        switch (promo.Type)
                        {
                            case PromotionType.Percentage:
                                if (promo.Percent is > 0m and <= 100m)
                                {
                                    var raw = (decimal)subtotalNet * (promo.Percent.Value / 100m);
                                    discountMinor = (long)Math.Round(raw, MidpointRounding.AwayFromZero);
                                }
                                break;

                            case PromotionType.Amount:
                                if (promo.AmountMinor.HasValue && promo.AmountMinor.Value > 0)
                                {
                                    discountMinor = promo.AmountMinor.Value;
                                }
                                break;
                        }

                        // Never exceed subtotal
                        if (discountMinor > subtotalNet) discountMinor = subtotalNet;

                        // VAT impact for discount:
                        // Simple approach: apply proportional reduction on VAT total.
                        // (For line-accurate VAT allocation you'd recalc per line; OK for phase 1.)
                        if (subtotalNet > 0 && discountMinor > 0)
                        {
                            var vatReduction = (long)Math.Round((double)vatTotal * ((double)discountMinor / subtotalNet), MidpointRounding.AwayFromZero);
                            vatTotal -= vatReduction;
                        }

                        subtotalNet -= discountMinor;
                    }
                }
            }

            result.SubtotalNetMinor = subtotalNet;
            result.VatTotalMinor = vatTotal;
            result.GrandTotalGrossMinor = subtotalNet + vatTotal;

            return result;
        }
    }
}
