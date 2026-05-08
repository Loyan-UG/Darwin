using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Pricing;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds pricing-related entities: tax categories and a few promotions.
    /// </summary>
    public sealed class PricingSeedSection
    {
        /// <summary>
        /// Ensures base tax categories and sample promotions exist for testing.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            // Tax categories
            if (!await db.Set<TaxCategory>().AnyAsync(ct))
            {
                db.AddRange(
                    new TaxCategory { Name = "Standard", VatRate = 0.19m },
                    new TaxCategory { Name = "Reduced", VatRate = 0.07m },
                    new TaxCategory { Name = "SuperReduced", VatRate = 0.00m }
                );
            }

            await EnsurePromotionAsync(db, "WELCOME10", "WELCOME10", Darwin.Domain.Enums.PromotionType.Percentage, 10m, null, null, 1000, 2, ct);
            await EnsurePromotionAsync(db, "FIVER", "FIVER", Darwin.Domain.Enums.PromotionType.Amount, null, 500, 2500, null, null, ct);
            await EnsurePromotionAsync(db, "SEASONAL", "SEASONAL", Darwin.Domain.Enums.PromotionType.Percentage, 15m, null, null, null, null, ct);

            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsurePromotionAsync(
            DarwinDbContext db,
            string name,
            string code,
            Darwin.Domain.Enums.PromotionType type,
            decimal? percent,
            long? amountMinor,
            long? minSubtotalNetMinor,
            int? maxRedemptions,
            int? perCustomerLimit,
            CancellationToken ct)
        {
            var promotion = await db.Set<Promotion>()
                .FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);

            if (promotion == null)
            {
                promotion = new Promotion { Code = code };
                db.Add(promotion);
            }

            promotion.Name = name;
            promotion.Type = type;
            promotion.Percent = percent;
            promotion.AmountMinor = amountMinor;
            promotion.Currency = DomainDefaults.DefaultCurrency;
            promotion.StartsAtUtc = DateTime.UtcNow.AddDays(-7);
            promotion.EndsAtUtc = DateTime.UtcNow.AddMonths(6);
            promotion.MinSubtotalNetMinor = minSubtotalNetMinor;
            promotion.MaxRedemptions = maxRedemptions;
            promotion.PerCustomerLimit = perCustomerLimit;
            promotion.IsActive = true;
        }
    }
}
