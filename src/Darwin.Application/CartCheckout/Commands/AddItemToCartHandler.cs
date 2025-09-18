using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.Catalog.Services;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Adds or increases a cart line. Computes/validates add-on pricing and merges lines with identical configurations.
    /// </summary>
    public sealed class AddItemToCartHandler
    {
        private readonly IAppDbContext _db;
        private readonly IAddOnPricingService _addOnPricing;

        public AddItemToCartHandler(IAppDbContext db, IAddOnPricingService addOnPricing)
        {
            _db = db;
            _addOnPricing = addOnPricing;
        }

        public async Task<Guid> HandleAsync(CartAddItemDto dto, CancellationToken ct = default)
        {
            if (dto.VariantId == Guid.Empty) throw new ValidationException("Variant is required.");
            if (dto.Quantity <= 0) throw new ValidationException("Quantity must be greater than zero.");

            // Resolve or create cart by (UserId|AnonymousId)
            var cart = await _db.Set<Cart>()
                .FirstOrDefaultAsync(c =>
                        !c.IsDeleted &&
                        ((dto.UserId != null && c.UserId == dto.UserId) ||
                         (dto.UserId == null && dto.AnonymousId != null && c.AnonymousId == dto.AnonymousId)),
                    ct);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = dto.UserId,
                    AnonymousId = dto.AnonymousId,
                    Currency = dto.Currency
                };
                _db.Set<Cart>().Add(cart);
                await _db.SaveChangesAsync(ct);
            }

            // Load variant & tax
            var variant = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VariantId && !v.IsDeleted, ct)
                ?? throw new ValidationException("Variant not found.");

            var tax = await _db.Set<TaxCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == variant.TaxCategoryId && !t.IsDeleted, ct)
                ?? throw new ValidationException("Tax category not found.");

            // Validate add-on selections and compute adjusted unit price
            await _addOnPricing.ValidateSelectionsForVariantAsync(variant.Id, dto.SelectedAddOnValueIds, ct);
            var addOnDelta = await _addOnPricing.SumPriceDeltasAsync(dto.SelectedAddOnValueIds, ct);
            var adjustedUnit = variant.BasePriceNetMinor + addOnDelta;

            // Snapshot VAT from tax category
            var vatRate = tax.VatRate;

            // Merge: same variant + same add-on set → single line
            var selectedJson = JsonSerializer.Serialize(dto.SelectedAddOnValueIds.OrderBy(x => x)); // sorted for stable equality
            var existing = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(li =>
                    li.CartId == cart.Id &&
                    li.VariantId == dto.VariantId &&
                    li.SelectedAddOnValueIdsJson == selectedJson &&
                    !li.IsDeleted, ct);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
                // Keep price snapshot stable per configuration; do not recalc unless policy requires
            }
            else
            {
                _db.Set<CartItem>().Add(new CartItem
                {
                    CartId = cart.Id,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    UnitPriceNetMinor = adjustedUnit,
                    VatRate = vatRate,
                    SelectedAddOnValueIdsJson = selectedJson
                });
            }

            await _db.SaveChangesAsync(ct);
            return cart.Id;
        }
    }
}
