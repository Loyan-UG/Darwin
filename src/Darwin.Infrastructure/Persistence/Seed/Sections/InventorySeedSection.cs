using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds inventory master data for warehouses, suppliers, stock levels, and a lightweight movement ledger.
    /// Transaction-heavy entities such as stock transfers and purchase orders remain optional and are not required
    /// for baseline development environments.
    /// </summary>
    public sealed class InventorySeedSection
    {
        private readonly ILogger<InventorySeedSection> _logger;

        private sealed record WarehouseSeed(string Name, string Location, string Description);

        private sealed record SupplierSeed(string Name, string Email, string Phone, string Address, string Notes);

        public InventorySeedSection(ILogger<InventorySeedSection> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds warehouse, supplier, stock level, and initial ledger data.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Inventory (warehouses/suppliers/stock levels) ...");

            var businesses = await db.Set<Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var variants = await db.ProductVariants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Sku)
                .ToListAsync(ct);

            if (businesses.Count == 0 || variants.Count == 0)
            {
                _logger.LogWarning("Skipping inventory seeding because businesses or product variants are missing.");
                return;
            }

            var warehouses = await EnsureWarehousesAsync(db, businesses, ct);

            if (!await db.Suppliers.AnyAsync(ct))
            {
                await SeedSuppliersAsync(db, businesses, ct);
            }

            await EnsureStockLevelsAsync(db, warehouses, variants, ct);

            if (!await db.InventoryTransactions.AnyAsync(ct))
            {
                await SeedInventoryTransactionsAsync(db, ct);
            }

            _logger.LogInformation("Inventory seeding done.");
        }

        private static async Task<List<Warehouse>> EnsureWarehousesAsync(
            DarwinDbContext db,
            IReadOnlyList<Business> businesses,
            CancellationToken ct)
        {
            var existing = await db.Warehouses.OrderBy(x => x.Name).ToListAsync(ct);
            if (existing.Count > 0)
            {
                return existing;
            }

            var seeds = GetWarehouseSeeds();
            var warehouses = new List<Warehouse>();

            for (var i = 0; i < businesses.Count && i < seeds.Length; i++)
            {
                warehouses.Add(new Warehouse
                {
                    BusinessId = businesses[i].Id,
                    Name = seeds[i].Name,
                    Description = seeds[i].Description,
                    Location = seeds[i].Location,
                    IsDefault = true
                });
            }

            db.Warehouses.AddRange(warehouses);
            await db.SaveChangesAsync(ct);

            return await db.Warehouses.OrderBy(x => x.Name).ToListAsync(ct);
        }

        private static async Task SeedSuppliersAsync(
            DarwinDbContext db,
            IReadOnlyList<Business> businesses,
            CancellationToken ct)
        {
            var seeds = GetSupplierSeeds();
            var suppliers = new List<Supplier>();

            for (var i = 0; i < businesses.Count && i < seeds.Length; i++)
            {
                suppliers.Add(new Supplier
                {
                    BusinessId = businesses[i].Id,
                    Name = seeds[i].Name,
                    Email = seeds[i].Email,
                    Phone = seeds[i].Phone,
                    Address = seeds[i].Address,
                    Notes = seeds[i].Notes
                });
            }

            db.Suppliers.AddRange(suppliers);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedStockLevelsAsync(
            DarwinDbContext db,
            IReadOnlyList<Warehouse> warehouses,
            IReadOnlyList<Domain.Entities.Catalog.ProductVariant> variants,
            CancellationToken ct)
        {
            var stockLevels = new List<StockLevel>();
            var pickedVariants = variants.Take(Math.Max(10, warehouses.Count)).ToList();

            for (var i = 0; i < warehouses.Count && i < 10; i++)
            {
                var primaryVariant = pickedVariants[i % pickedVariants.Count];
                var secondaryVariant = pickedVariants[(i + 1) % pickedVariants.Count];

                stockLevels.Add(new StockLevel
                {
                    WarehouseId = warehouses[i].Id,
                    ProductVariantId = primaryVariant.Id,
                    AvailableQuantity = 40 + (i * 3),
                    ReservedQuantity = i % 4,
                    ReorderPoint = 8,
                    ReorderQuantity = 20,
                    InTransitQuantity = i % 3
                });

                stockLevels.Add(new StockLevel
                {
                    WarehouseId = warehouses[i].Id,
                    ProductVariantId = secondaryVariant.Id,
                    AvailableQuantity = 24 + (i * 2),
                    ReservedQuantity = 0,
                    ReorderPoint = 6,
                    ReorderQuantity = 18,
                    InTransitQuantity = i % 2
                });
            }

            db.StockLevels.AddRange(stockLevels);
            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsureStockLevelsAsync(
            DarwinDbContext db,
            IReadOnlyList<Warehouse> warehouses,
            IReadOnlyList<Domain.Entities.Catalog.ProductVariant> variants,
            CancellationToken ct)
        {
            if (warehouses.Count == 0 || variants.Count == 0)
            {
                return;
            }

            if (!await db.StockLevels.AnyAsync(ct))
            {
                await SeedStockLevelsAsync(db, warehouses, variants, ct);
                return;
            }

            var defaultWarehouse = warehouses.FirstOrDefault(x => x.IsDefault) ?? warehouses[0];
            var storefrontVariants = variants.Take(20).ToList();
            var storefrontVariantIds = storefrontVariants.Select(v => v.Id).ToArray();
            var existingLevels = await db.StockLevels
                .Where(x => x.WarehouseId == defaultWarehouse.Id && storefrontVariantIds.Contains(x.ProductVariantId))
                .ToListAsync(ct);

            foreach (var variant in storefrontVariants)
            {
                var level = existingLevels.FirstOrDefault(x => x.ProductVariantId == variant.Id);
                if (level == null)
                {
                    level = new StockLevel
                    {
                        WarehouseId = defaultWarehouse.Id,
                        ProductVariantId = variant.Id,
                        ReorderPoint = 8,
                        ReorderQuantity = 20
                    };
                    db.StockLevels.Add(level);
                    existingLevels.Add(level);
                }

                if (level.AvailableQuantity < 12)
                {
                    level.AvailableQuantity = 36;
                }

                if (level.ReservedQuantity < 0)
                {
                    level.ReservedQuantity = 0;
                }

                if (level.InTransitQuantity < 0)
                {
                    level.InTransitQuantity = 0;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedInventoryTransactionsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var stockLevels = await db.StockLevels
                .OrderBy(x => x.WarehouseId)
                .ThenBy(x => x.ProductVariantId)
                .Take(10)
                .ToListAsync(ct);

            var transactions = stockLevels
                .Select((level, index) => new InventoryTransaction
                {
                    WarehouseId = level.WarehouseId,
                    ProductVariantId = level.ProductVariantId,
                    QuantityDelta = 12 + index,
                    Reason = index % 2 == 0 ? "Seed.InitialGoodsReceipt" : "Seed.OpeningBalanceAdjustment",
                    ReferenceId = null
                })
                .ToList();

            db.InventoryTransactions.AddRange(transactions);
            await db.SaveChangesAsync(ct);
        }

        private static WarehouseSeed[] GetWarehouseSeeds() => new[]
        {
            new WarehouseSeed("Berlin Zentrallager", "Berlin-Mitte, Heidestraße 22", "Zentrales Lager für Hauptstadtregion."),
            new WarehouseSeed("München Süd Lager", "München, Gmunder Straße 37", "Süddeutsches Distributionslager."),
            new WarehouseSeed("Köln Rheinlager", "Köln, Am Molenkopf 4", "Versorgung für NRW und Rheinland."),
            new WarehouseSeed("Hamburg Hafenlager", "Hamburg, Australiastraße 52", "Wareneingang über Hafen und Norddeutschland."),
            new WarehouseSeed("Frankfurt Transit Hub", "Frankfurt am Main, Gutleutstraße 310", "Transitlager für West- und Mitteldeutschland."),
            new WarehouseSeed("Stuttgart Service Depot", "Stuttgart, Pragstraße 120", "Service- und Ersatzteillager Südwest."),
            new WarehouseSeed("Düsseldorf City Depot", "Düsseldorf, Kettwiger Straße 1", "Innenstadt- und Same-Day-Versorgung."),
            new WarehouseSeed("Leipzig Ostlager", "Leipzig, Torgauer Straße 231", "Ostdeutsches Verteilzentrum."),
            new WarehouseSeed("Dresden Techniklager", "Dresden, Königsbrücker Straße 96", "Technik- und Zubehörlager Sachsen."),
            new WarehouseSeed("Nürnberg Frankenlager", "Nürnberg, Muggenhofer Straße 136", "Fränkisches Lager für Südost-Deutschland.")
        };

        private static SupplierSeed[] GetSupplierSeeds() => new[]
        {
            new SupplierSeed("Berliner Technikhandel GmbH", "einkauf@berliner-technikhandel.de", "+49 30 7001001", "Heidestraße 22, 10557 Berlin", "Rahmenlieferant für Zubehör und Ersatzteile."),
            new SupplierSeed("Münchner Gastrobedarf KG", "kontakt@muenchner-gastrobedarf.de", "+49 89 7001002", "Schwanthalerstraße 91, 80336 München", "Lieferant für Gastronomiebedarf."),
            new SupplierSeed("Rhein Office Supply GmbH", "sales@rhein-office-supply.de", "+49 221 7001003", "Subbelrather Straße 15, 50823 Köln", "Büro- und POS-Hardware."),
            new SupplierSeed("NordLogistik Partner GmbH", "info@nordlogistik-partner.de", "+49 40 7001004", "Amsinckstraße 73, 20097 Hamburg", "Verpackung und Lagerlogistik."),
            new SupplierSeed("Main Digital Systems GmbH", "vertrieb@main-digital-systems.de", "+49 69 7001005", "Hanauer Landstraße 204, 60314 Frankfurt am Main", "Displays, Scanner und Netzwerktechnik."),
            new SupplierSeed("Schwaben Service Parts GmbH", "orders@schwaben-service-parts.de", "+49 711 7001006", "Rotebühlstraße 125, 70178 Stuttgart", "Ersatzteile und Werkstattbedarf."),
            new SupplierSeed("Rheinland Retail Components GmbH", "handel@rheinland-retail-components.de", "+49 211 7001007", "Werdener Straße 8, 40227 Düsseldorf", "Ladenbau und Kassensysteme."),
            new SupplierSeed("Sachsen Elektronikhandel AG", "dispo@sachsen-elektronikhandel.de", "+49 341 7001008", "Prager Straße 38, 04103 Leipzig", "Elektronik und Schnellnachschub Ost."),
            new SupplierSeed("Elbtal Verpackung GmbH", "einkauf@elbtal-verpackung.de", "+49 351 7001009", "Hamburger Straße 23, 01067 Dresden", "Verpackungs- und Versandmaterial."),
            new SupplierSeed("Franken Retail Supply GmbH", "service@franken-retail-supply.de", "+49 911 7001010", "Fürther Straße 212, 90429 Nürnberg", "Regelmäßige Beschaffung für Filialbedarf.")
        };
    }
}
