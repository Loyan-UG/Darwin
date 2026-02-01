using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            _logger = logger;
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Shipping (methods/rates) ...");

            if (await db.ShippingMethods.AnyAsync(ct)) return;

            var methods = new List<ShippingMethod>
            {
                new() { Name = "DHL Standard", Carrier = "DHL", Service = "Standard", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "DHL Express", Carrier = "DHL", Service = "Express", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "Hermes Standard", Carrier = "Hermes", Service = "Standard", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "DPD Standard", Carrier = "DPD", Service = "Standard", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "UPS Standard", Carrier = "UPS", Service = "Standard", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "UPS Express", Carrier = "UPS", Service = "Express", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "GLS Standard", Carrier = "GLS", Service = "Standard", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "DHL Economy", Carrier = "DHL", Service = "Economy", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "DPD Pickup", Carrier = "DPD", Service = "Pickup", CountriesCsv = "DE", Currency = "EUR" },
                new() { Name = "Hermes ParcelShop", Carrier = "Hermes", Service = "ParcelShop", CountriesCsv = "DE", Currency = "EUR" }
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
                    MaxSubtotalNetMinor = 20000 + (i * 1000),
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