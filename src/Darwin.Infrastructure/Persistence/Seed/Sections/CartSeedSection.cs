using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds and repairs sample carts for both guest and signed-in storefront flows.
    /// </summary>
    public sealed class CartSeedSection
    {
        /// <summary>
        /// Creates sample carts with coherent VAT and unit price snapshots.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            var variants = await db.ProductVariants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Sku)
                .ToListAsync(ct);

            if (variants.Count == 0)
            {
                return;
            }

            var taxCategoryIds = variants.Select(x => x.TaxCategoryId).Distinct().ToArray();
            var taxRatesById = await db.TaxCategories
                .Where(x => taxCategoryIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.VatRate, ct);

            var member = await db.Users.FirstOrDefaultAsync(u => u.Email == "cons1@darwin.de" && !u.IsDeleted, ct);
            if (member != null)
            {
                var firstVariant = variants.FirstOrDefault(x => x.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"))
                    ?? variants[0];
                var secondVariant = variants.FirstOrDefault(x => x.Id == Guid.Parse("22222222-2222-2222-2222-222222222222"))
                    ?? variants[Math.Min(1, variants.Count - 1)];

                var memberCart = await EnsureCartAsync(db, member.Id, null, "WELCOME10", ct);
                await EnsureCartItemAsync(db, memberCart.Id, firstVariant, quantity: 1, ResolveVatRate(firstVariant, taxRatesById), ct);
                await EnsureCartItemAsync(db, memberCart.Id, secondVariant, quantity: 1, ResolveVatRate(secondVariant, taxRatesById), ct);
            }

            var guestVariant = variants.FirstOrDefault(x => x.Id == Guid.Parse("22222222-2222-2222-2222-222222222222"))
                ?? variants[Math.Min(1, variants.Count - 1)];
            var guestCart = await EnsureCartAsync(db, null, "anon-123", null, ct);
            await EnsureCartItemAsync(db, guestCart.Id, guestVariant, quantity: 2, ResolveVatRate(guestVariant, taxRatesById), ct);
        }

        private static async Task<Cart> EnsureCartAsync(
            DarwinDbContext db,
            Guid? userId,
            string? anonymousId,
            string? couponCode,
            CancellationToken ct)
        {
            var cart = userId.HasValue
                ? await db.Set<Cart>().FirstOrDefaultAsync(x => x.UserId == userId.Value && !x.IsDeleted, ct)
                : await db.Set<Cart>().FirstOrDefaultAsync(x => x.AnonymousId == anonymousId && !x.IsDeleted, ct);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    AnonymousId = anonymousId
                };
                db.Add(cart);
            }

            cart.Currency = DomainDefaults.DefaultCurrency;
            cart.CouponCode = couponCode;

            await db.SaveChangesAsync(ct);
            return cart;
        }

        private static async Task EnsureCartItemAsync(
            DarwinDbContext db,
            Guid cartId,
            ProductVariant variant,
            int quantity,
            decimal vatRate,
            CancellationToken ct)
        {
            var item = await db.Set<CartItem>()
                .FirstOrDefaultAsync(x => x.CartId == cartId && x.VariantId == variant.Id && !x.IsDeleted, ct);

            if (item == null)
            {
                item = new CartItem
                {
                    CartId = cartId,
                    VariantId = variant.Id
                };
                db.Add(item);
            }

            item.Quantity = quantity;
            item.UnitPriceNetMinor = variant.BasePriceNetMinor;
            item.VatRate = vatRate;
            item.SelectedAddOnValueIdsJson = "[]";
            item.AddOnPriceDeltaMinor = 0;

            await db.SaveChangesAsync(ct);
        }

        private static decimal ResolveVatRate(
            ProductVariant variant,
            IReadOnlyDictionary<Guid, decimal> taxRatesById)
        {
            return taxRatesById.TryGetValue(variant.TaxCategoryId, out var rate)
                ? rate
                : 0.19m;
        }
    }
}
