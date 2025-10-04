using Darwin.Application.Abstractions.Persistence;
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

            // Promotions
            if (!await db.Set<Promotion>().AnyAsync(ct))
            {
                var p1 = new Promotion
                {
                    Name = "WELCOME10",
                    Code = "WELCOME10",
                    Type = Darwin.Domain.Enums.PromotionType.Percentage,
                    Percent = 10m,
                    Currency = "EUR",
                    StartsAtUtc = DateTime.UtcNow.AddDays(-7),
                    EndsAtUtc = DateTime.UtcNow.AddMonths(6),
                    MaxRedemptions = 1000,
                    PerCustomerLimit = 2,
                    IsActive = true
                };
                var p2 = new Promotion
                {
                    Name = "FIVER",
                    Code = "FIVER",
                    Type = Darwin.Domain.Enums.PromotionType.Amount,
                    AmountMinor = 500, // €5.00
                    Currency = "EUR",
                    StartsAtUtc = DateTime.UtcNow.AddDays(-7),
                    EndsAtUtc = DateTime.UtcNow.AddMonths(3),
                    MinSubtotalNetMinor = 2500,
                    IsActive = true
                };
                var p3 = new Promotion
                {
                    Name = "SEASONAL",
                    Code = "SEASONAL",
                    Type = Darwin.Domain.Enums.PromotionType.Percentage,
                    Percent = 15m,
                    Currency = "EUR",
                    StartsAtUtc = DateTime.UtcNow.AddDays(-3),
                    EndsAtUtc = DateTime.UtcNow.AddMonths(1),
                    IsActive = true
                };

                db.AddRange(p1, p2, p3);

                // Example redemption row (attach later to a real order/user if needed)
                db.Add(new PromotionRedemption
                {
                    PromotionId = p1.Id,
                    UserId = null,         // guest
                    OrderId = Guid.NewGuid()
                });
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
