using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Shipping;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds shipping methods and rates (Germany-focused).
    /// </summary>
    public sealed class ShippingSeedSection
    {
        private readonly ILogger<ShippingSeedSection> _logger;

        public ShippingSeedSection(ILogger<ShippingSeedSection> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Shipping (methods/rates) ...");

            if (await db.ShippingMethods.AnyAsync(ct))
            {
                var existingMethods = await db.ShippingMethods
                    .Where(x => !x.IsDeleted)
                    .ToListAsync(ct);

                var changed = false;
                foreach (var method in existingMethods)
                {
                    if (!method.IsActive)
                    {
                        method.IsActive = true;
                        changed = true;
                    }
                }

                var existingRates = await db.ShippingRates
                    .Where(x => !x.IsDeleted)
                    .ToListAsync(ct);

                foreach (var rate in existingRates)
                {
                    if (!rate.MaxSubtotalNetMinor.HasValue || rate.MaxSubtotalNetMinor.Value < 500000)
                    {
                        rate.MaxSubtotalNetMinor = 500000;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await db.SaveChangesAsync(ct);
                }

                return;
            }

            var methods = new List<ShippingMethod>
            {
                new() { Name = "DHL Standard", Carrier = "DHL", Service = "Standard", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "DHL Express", Carrier = "DHL", Service = "Express", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "Hermes Standard", Carrier = "Hermes", Service = "Standard", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "DPD Standard", Carrier = "DPD", Service = "Standard", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "UPS Standard", Carrier = "UPS", Service = "Standard", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "UPS Express", Carrier = "UPS", Service = "Express", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "GLS Standard", Carrier = "GLS", Service = "Standard", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "DHL Economy", Carrier = "DHL", Service = "Economy", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "DPD Pickup", Carrier = "DPD", Service = "Pickup", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true },
                new() { Name = "Hermes ParcelShop", Carrier = "Hermes", Service = "ParcelShop", CountriesCsv = DomainDefaults.DefaultCountryCode, Currency = DomainDefaults.DefaultCurrency, IsActive = true }
            };

            db.AddRange(methods);
            await db.SaveChangesAsync(ct);

            var rates = new List<ShippingRate>();
            for (var i = 0; i < methods.Count; i++)
            {
                rates.Add(new ShippingRate
                {
                    ShippingMethodId = methods[i].Id,
                    MaxShipmentMass = 5000 + (i * 500),
                    MaxSubtotalNetMinor = 500000,
                    PriceMinor = 490 + (i * 50),
                    SortOrder = 0
                });
            }

            db.AddRange(rates);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Shipping seeding done.");
        }
    }
}
