using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds a few carts and items to simulate both guest and signed-in flows.
    /// </summary>
    public sealed class CartSeedSection
    {
        /// <summary>
        /// Creates several sample carts with VAT and unit price snapshots.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            if (await db.Set<Cart>().AnyAsync(ct)) return;

            var userAlice = await db.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com" && !u.IsDeleted, ct);

            // User-owned cart
            var cart1 = new Cart
            {
                UserId = userAlice?.Id,
                Currency = "EUR",
                CouponCode = "WELCOME10"
            };
            db.Add(cart1);

            db.Add(new CartItem
            {
                CartId = cart1.Id,
                VariantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Quantity = 1,
                UnitPriceNetMinor = 2999, // €29.99 net
                VatRate = 0.19m,
                SelectedAddOnValueIdsJson = "[]"
            });

            // Guest cart
            var cart2 = new Cart
            {
                UserId = null,
                AnonymousId = "anon-123",
                Currency = "EUR"
            };
            db.Add(cart2);

            db.Add(new CartItem
            {
                CartId = cart2.Id,
                VariantId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Quantity = 2,
                UnitPriceNetMinor = 999, // €9.99 net
                VatRate = 0.07m,
                SelectedAddOnValueIdsJson = "[]"
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
