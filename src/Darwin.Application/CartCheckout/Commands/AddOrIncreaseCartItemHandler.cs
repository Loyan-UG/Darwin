using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.Catalog.Services;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Adds a new line to the cart or increases quantity when the same variant+add-ons already exists.
    /// Validates add-on selections and computes the add-on price delta before persistence.
    /// </summary>
    public sealed class AddOrIncreaseCartItemHandler
    {
        private readonly IAppDbContext _db;
        private readonly IAddOnPricingService _addOnPricing;

        public AddOrIncreaseCartItemHandler(IAppDbContext db, IAddOnPricingService addOnPricing)
        {
            _db = db;
            _addOnPricing = addOnPricing;
        }

        public async Task<Guid> HandleAsync(CartAddItemDto dto, CancellationToken ct = default)
        {
            if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.AnonymousId))
                throw new InvalidOperationException("Either UserId or AnonymousId is required.");

            // Locate or create cart
            var cart = await _db.Set<Cart>()
                .FirstOrDefaultAsync(c =>
                        !c.IsDeleted &&
                        ((dto.UserId != null && c.UserId == dto.UserId) ||
                         (dto.UserId == null && c.AnonymousId == dto.AnonymousId)),
                    ct);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = dto.UserId,
                    AnonymousId = dto.UserId == null ? dto.AnonymousId : null,
                    Currency = dto.Currency ?? "EUR"
                };
                _db.Set<Cart>().Add(cart);
                await _db.SaveChangesAsync(ct);
            }

            // Validate variant exists and is sellable (basic check)
            var variant = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VariantId && !v.IsDeleted, ct)
                ?? throw new InvalidOperationException("Variant not found.");

            // Validate add-on selections and compute delta
            var selIds = dto.SelectedAddOnValueIds?.Distinct().ToList() ?? new List<Guid>();
            await _addOnPricing.ValidateSelectionsForVariantAsync(dto.VariantId, selIds, ct);
            var deltaMinor = await _addOnPricing.SumPriceDeltasAsync(selIds, ct);

            // Snapshot pricing (server is the source of truth; override if UI sent values)
            var unitNetMinor = dto.UnitPriceNetMinor ?? variant.BasePriceNetMinor;
            var vatRate = dto.VatRate ?? await _db.Set<Product>()
                               .Where(p => p.Id == variant.ProductId)
                               .Join(_db.Set<ProductVariant>(), p => p.Id, v => v.ProductId, (p, v) => v)
                               .Where(v => v.Id == variant.Id)
                               .Join(_db.Set<Darwin.Domain.Entities.Pricing.TaxCategory>(), v => v.TaxCategoryId, t => t.Id, (v, t) => t.VatRate)
                               .FirstAsync(ct);

            var selJson = JsonSerializer.Serialize(selIds);

            // Try merge with existing line (same variant + same add-on configuration)
            var existing = cart.Items.FirstOrDefault(i =>
                i.VariantId == dto.VariantId &&
                i.SelectedAddOnValueIdsJson == selJson &&
                !i.IsDeleted);

            if (existing != null)
            {
                existing.Quantity += Math.Max(1, dto.Quantity);
                // keep latest pricing snapshot simple; you may choose to re-snapshot here
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    CartId = cart.Id,
                    VariantId = dto.VariantId,
                    Quantity = Math.Max(1, dto.Quantity),
                    UnitPriceNetMinor = unitNetMinor,
                    VatRate = vatRate,
                    SelectedAddOnValueIdsJson = selJson,
                    AddOnPriceDeltaMinor = deltaMinor
                });
            }

            await _db.SaveChangesAsync(ct);
            return cart.Id;
        }
    }
}
