using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.Pricing;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ComputeCartSummaryHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public Task<CartSummaryDto> HandleAsync(Guid cartId, CancellationToken ct = default)
            => HandleAsync(cartId, culture: null, ct);

        public async Task<CartSummaryDto> HandleAsync(Guid cartId, string? culture = null, CancellationToken ct = default)
        {
            var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
            var defaultCulture = SiteSettingDto.DefaultCultureDefault;
            var cart = await _db.Set<Cart>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cartId && !c.IsDeleted, ct)
                ?? throw new InvalidOperationException(_localizer["CartNotFound"]);

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
                    v.ProductId,
                    v.BasePriceNetMinor,
                    v.Currency,
                    v.TaxCategoryId
                })
                .ToListAsync(ct);

            if (variants.Count != variantIds.Count)
                throw new InvalidOperationException(_localizer["CartVariantsNoLongerAvailable"]);

            // Currency consistency (phase 1: single currency across lines)
            var distinctVariantCurrencies = variants.Select(v => v.Currency).Distinct().ToList();
            if (distinctVariantCurrencies.Count > 1)
                throw new InvalidOperationException(_localizer["MixedCartCurrenciesNotSupported"]);
            if (!string.Equals(distinctVariantCurrencies[0], cart.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(_localizer["CartCurrencyDiffersFromVariantCurrency"]);

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
            var productIds = variants.Select(v => v.ProductId).Distinct().ToList();
            var categoryIds = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && p.PrimaryCategoryId.HasValue)
                .Select(p => p.PrimaryCategoryId!.Value)
                .Distinct()
                .ToListAsync(ct);

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
                ? new Dictionary<Guid, CartSelectedAddOnDto>()
                : await _db.Set<AddOnOptionValue>()
                    .AsNoTracking()
                    .Where(v => allSelectedAddOnValueIds.Contains(v.Id) && !v.IsDeleted && v.IsActive)
                    .Select(v => new CartSelectedAddOnDto
                    {
                        ValueId = v.Id,
                        OptionId = v.AddOnOptionId,
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        ValueLabel = v.Translations.Where(t => !t.IsDeleted && t.Culture == normalizedCulture).Select(t => t.Label).FirstOrDefault()
                            ?? v.Translations.Where(t => !t.IsDeleted && t.Culture == defaultCulture).Select(t => t.Label).FirstOrDefault()
                            ?? v.Label,
                        OptionLabel = _db.Set<AddOnOption>()
                            .Where(o => o.Id == v.AddOnOptionId && !o.IsDeleted)
                            .Select(o => o.Translations.Where(t => !t.IsDeleted && t.Culture == normalizedCulture).Select(t => t.Label).FirstOrDefault()
                                ?? o.Translations.Where(t => !t.IsDeleted && t.Culture == defaultCulture).Select(t => t.Label).FirstOrDefault()
                                ?? o.Label)
                            .FirstOrDefault() ?? string.Empty
                    })
                    .ToDictionaryAsync(v => v.ValueId, v => v, ct);

            long subtotalNet = 0;
            long vatTotal = 0;

            foreach (var line in lines)
            {
                var v = variantById[line.VariantId];

                // Sum add-on deltas for this line
                long addOnDelta = 0;
                string addOnJson = line.SelectedAddOnValueIdsJson ?? "[]";
                var selectedAddOns = new List<CartSelectedAddOnDto>();
                try
                {
                    var ids = JsonSerializer.Deserialize<List<Guid>>(addOnJson) ?? new();
                    foreach (var id in ids)
                    {
                        if (addOnValues.TryGetValue(id, out var addOn))
                        {
                            addOnDelta += addOn.PriceDeltaMinor;
                            selectedAddOns.Add(addOn);
                        }
                    }
                }
                catch { /* ignore, already defensive above */ }

                var unitNet = v.BasePriceNetMinor + addOnDelta;

                if (!vatByTaxId.TryGetValue(v.TaxCategoryId, out var vatRate))
                    throw new InvalidOperationException(_localizer["TaxCategoryMissingForVariant"]);

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
                    SelectedAddOnValueIdsJson = addOnJson,
                    SelectedAddOns = selectedAddOns
                });

                subtotalNet += lineNet;
                vatTotal += lineVat;
            }

            long discountMinor = 0;

            // Apply promotion if any
            if (!string.IsNullOrWhiteSpace(cart.CouponCode))
            {
                var now = DateTime.UtcNow;
                var normalizedCode = CouponEligibility.NormalizeCode(cart.CouponCode);
                var promo = await _db.Set<Promotion>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        !p.IsDeleted &&
                        p.IsActive &&
                        p.Code != null &&
                        p.Code == normalizedCode &&
                        (p.StartsAtUtc == null || p.StartsAtUtc <= now) &&
                        (p.EndsAtUtc == null || p.EndsAtUtc >= now), ct);

                if (promo != null)
                {
                    var redemptionLimitReached = false;
                    if (promo.MaxRedemptions.HasValue)
                    {
                        var totalRedemptions = await _db.Set<PromotionRedemption>()
                            .AsNoTracking()
                            .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id, ct);
                        redemptionLimitReached = totalRedemptions >= promo.MaxRedemptions.Value;
                    }

                    if (!redemptionLimitReached && promo.PerCustomerLimit.HasValue && cart.UserId.HasValue)
                    {
                        var customerRedemptions = await _db.Set<PromotionRedemption>()
                            .AsNoTracking()
                            .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id && r.UserId == cart.UserId, ct);
                        redemptionLimitReached = customerRedemptions >= promo.PerCustomerLimit.Value;
                    }

                    var eligibility = redemptionLimitReached
                        ? new CouponEligibilityResult { IsValid = false }
                        : CouponEligibility.Evaluate(
                            promo,
                            new CouponEligibilityContext
                            {
                                SubtotalNetMinor = subtotalNet,
                                Currency = result.Currency,
                                ProductIds = productIds,
                                CategoryIds = categoryIds
                            });

                    discountMinor = eligibility.IsValid ? eligibility.DiscountMinor : 0;

                    // VAT impact for discount:
                    // Simple approach: apply proportional reduction on VAT total.
                    // (For line-accurate VAT allocation you'd recalc per line; OK for phase 1.)
                    if (subtotalNet > 0 && discountMinor > 0)
                    {
                        var vatReduction = (long)Math.Round((double)vatTotal * ((double)discountMinor / subtotalNet), MidpointRounding.AwayFromZero);
                        vatTotal -= vatReduction;
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
