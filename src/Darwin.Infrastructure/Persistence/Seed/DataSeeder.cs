using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed
{
    public sealed class DataSeeder
    {
        private readonly DarwinDbContext _db;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(DarwinDbContext db, ILogger<DataSeeder> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting data seeding…");

            if (!await _db.Set<SiteSetting>().AnyAsync(ct))
            {
                _db.Add(new SiteSetting
                {
                    Title = "Darwin",
                    DefaultCulture = "de-DE",
                    DefaultCountry = "DE",
                    DefaultCurrency = "EUR",
                    TimeZone = "Europe/Berlin",
                    MeasurementSystem = "Metric",
                    DisplayWeightUnit = "kg",
                    DisplayLengthUnit = "cm",
                    EnableCanonical = true,
                    HreflangEnabled = true
                });
                await _db.SaveChangesAsync(ct);
            }

            if (!await _db.Set<TaxCategory>().AnyAsync(ct))
            {
                _db.Add(new TaxCategory { Name = "Standard", VatRate = 0.19m, EffectiveFromUtc = DateTime.UtcNow });
                _db.Add(new TaxCategory { Name = "Reduced", VatRate = 0.07m, EffectiveFromUtc = DateTime.UtcNow });
                await _db.SaveChangesAsync(ct);
            }

            if (!await _db.Set<Brand>().AnyAsync(ct))
            {
                _db.Add(new Brand { Name = "Darwin Generic" });
                await _db.SaveChangesAsync(ct);
            }

            if (!await _db.Set<Category>().AnyAsync(ct))
            {
                var root = new Category { IsActive = true, SortOrder = 1 };
                root.Translations.Add(new CategoryTranslation { Culture = "de-DE", Name = "Lebensmittel", Slug = "lebensmittel" });

                var fruits = new Category { ParentId = root.Id, IsActive = true, SortOrder = 2 };
                fruits.Translations.Add(new CategoryTranslation { Culture = "de-DE", Name = "Obst", Slug = "obst" });

                _db.Add(root);
                _db.Add(fruits);
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Data seeding completed.");
        }
    }
}
