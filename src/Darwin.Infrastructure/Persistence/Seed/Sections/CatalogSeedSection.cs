using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds baseline Catalog data for a German electronics shop:
    /// - Brands (Apple, Samsung, Google, ...).
    /// - Categories (smartphones, laptops, components, accessories).
    /// - Sample products (≥ 20) with one default variant each.
    /// - A sample Add-on group (Extended Warranty) attached to relevant categories.
    /// 
    /// All money is persisted as NET amounts in minor units (long), see ProductVariant fields.
    /// Dimensions are stored as SI units (mm / g) per domain guidance.
    /// </summary>
    public sealed class CatalogSeedSection
    {
        /// <summary>
        /// Entry point for Catalog seeding. Idempotent by design.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            // Brands
            if (!await db.Brands.AnyAsync(ct))
                await CreateBrandsDeAsync(db, ct);

            // Categories
            if (!await db.Categories.AnyAsync(ct))
                await CreateCategoriesDeAsync(db, ct);

            // Products (create if none exist)
            if (!await db.Products.AnyAsync(ct))
                await CreateProductsDeAsync(db, ct);

            // Add-ons (create if none exist)
            if (!await db.Set<AddOnGroup>().AnyAsync(ct))
                await SeedExtendedWarrantyAddOnsAsync(db, ct);
        }

        /// <summary>
        /// Creates a minimal but meaningful set of German brands used by the seed products.
        /// </summary>
        private static async Task CreateBrandsDeAsync(DarwinDbContext db, CancellationToken ct)
        {
            var brands = new (string Slug, string Name)[]
            {
                ("apple","Apple"),
                ("samsung","Samsung"),
                ("google","Google"),
                ("xiaomi","Xiaomi"),
                ("oneplus","OnePlus"),
                ("dell","Dell"),
                ("hp","HP"),
                ("lenovo","Lenovo"),
                ("asus","ASUS"),
                ("logitech","Logitech"),
                ("corsair","Corsair"),
                ("seagate","Seagate"),
                ("western-digital","Western Digital"),
                ("lg","LG"),
                ("sony","Sony"),
                ("anker","Anker"),
                ("razer","Razer"),
                ("kingston","Kingston"),
                // one placeholder used in items for Anker line (brand slot 'sandisk' not used anymore)
            };

            foreach (var b in brands)
            {
                var brand = new Brand { Slug = b.Slug };
                brand.Translations.Add(new BrandTranslation
                {
                    Culture = "de-DE",
                    Name = b.Name
                });
                db.Add(brand);
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Creates a set of categories with German translations and SEO-friendly slugs.
        /// </summary>
        private static async Task CreateCategoriesDeAsync(DarwinDbContext db, CancellationToken ct)
        {
            // Flat taxonomy for now; can be extended to a tree using ParentId/SortOrder.
            var cats = new (string Slug, string Name, string? Desc)[]
            {
                ("iphones",            "iPhones",                 "Apple iPhone Modelle"),
                ("android-phones",     "Android-Smartphones",     "Android Geräte von führenden Herstellern"),
                ("ultrabooks",         "Ultrabooks",              "Leichte, mobile Notebooks"),
                ("business-laptops",   "Business-Laptops",        "Zuverlässige Laptops für den Arbeitsalltag"),
                ("gaming-laptops",     "Gaming-Laptops",          "Leistungsstarke Laptops für Gaming"),
                ("mice",               "Mäuse",                   "Präzise Computer-Mäuse"),
                ("keyboards",          "Tastaturen",              "Mechanische und Office-Tastaturen"),
                ("storage",            "Speicher",                "NVMe/SSD/HDD Speicherlösungen"),
                ("monitors",           "Monitore",                "4K/144Hz/Ultrawide Monitore"),
                ("headphones",         "Kopfhörer",               "Over-Ear/ANC/BT Kopfhörer"),
                ("power-banks",        "Powerbanks",              "Mobile Ladegeräte"),
                ("pc-komponenten",     "PC-Komponenten",          "RAM, Mainboards, Netzteile u. a.")
            };

            foreach (var c in cats)
            {
                var cat = new Category { IsActive = true, SortOrder = 0 };
                cat.Translations.Add(new CategoryTranslation
                {
                    Culture = "de-DE",
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Desc
                });
                db.Add(cat);
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Creates ≥ 20 sample products (German locale), each with one simple variant.
        /// Prices are stored in NET minor units (EUR cents).
        /// Two variants get fixed GUIDs to align with Inventory/Cart seeds.
        /// </summary>
        private static async Task CreateProductsDeAsync(DarwinDbContext db, CancellationToken ct)
        {
            // Lookup maps
            var brandMap = await db.Brands
                .Include(b => b.Translations)
                .ToDictionaryAsync(b => b.Slug ?? string.Empty, b => b, ct);

            var catMap = await db.Categories
                .Include(c => c.Translations)
                .Select(c => new
                {
                    Cat = c,
                    Slug = c.Translations
                        .Where(t => t.Culture == "de-DE")
                        .Select(t => t.Slug)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToDictionaryAsync(x => x.Slug, x => x.Cat, ct);

            var standardTax = await EnsureStandardTaxCategoryAsync(db, ct);

            // Strongly-typed tuple array to avoid CS0826 while keeping the same item set.
            var items = new (string Name, string Slug, string Brand, string Cat, decimal Price, decimal? Compare, string SKU)[]
            {
                // Smartphones
                ("iPhone 15 Pro 128GB",            "iphone-15-pro-128",     "apple",          "iphones",           1199m,  null,           "APL-IP15P-128-001"),
                ("Samsung Galaxy S24 256GB",       "galaxy-s24-256",         "samsung",        "android-phones",     999m,   1099m,         "SMS-S24-256-001"),
                ("Google Pixel 9 128GB",           "pixel-9-128",            "google",         "android-phones",     799m,   869m,          "GGL-PX9-128-001"),
                ("Xiaomi 14 Pro 256GB",            "xiaomi-14-pro-256",      "xiaomi",         "android-phones",     799m,   null,           "XMI-14P-256-001"),
                ("OnePlus 12 256GB",               "oneplus-12-256",         "oneplus",        "android-phones",     869m,   949m,          "1P-12-256-001"),

                // Laptops
                ("MacBook Air 13 M3 8/256",        "macbook-air-13-m3",      "apple",          "ultrabooks",        1299m,   1399m,         "APL-MBA13-M3-001"),
                ("Dell XPS 13 16/512",             "dell-xps-13-2025",       "dell",           "ultrabooks",        1599m,   null,          "DEL-XPS13-001"),
                ("HP Envy 15",                     "hp-envy-15",             "hp",             "business-laptops",  1199m,   1299m,         "HP-ENVY15-001"),
                ("Lenovo ThinkPad X1 Carbon",      "thinkpad-x1-carbon",     "lenovo",         "business-laptops",  1899m,   1999m,         "LNV-X1C-001"),
                ("ASUS ROG Zephyrus G14",          "rog-g14-2025",           "asus",           "gaming-laptops",    1799m,   1899m,         "ASU-G14-001"),

                // Components & Peripherals
                ("Logitech MX Master 3S",          "logitech-mx-master-3s",  "logitech",       "mice",               119m,   139m,          "LOG-MX3S-001"),
                ("Corsair K70 RGB Pro",            "corsair-k70-rgb-pro",    "corsair",        "keyboards",          179m,   null,          "COR-K70-RGBP-001"),
                ("Samsung 980 Pro 1TB NVMe",       "samsung-980-pro-1tb",    "samsung",        "storage",            129m,   159m,          "SMS-980PRO-1TB-001"),
                ("Seagate BarraCuda 4TB",          "seagate-barracuda-4tb",  "seagate",        "storage",             99m,   null,          "SEA-BC-4TB-001"),
                ("WD Black SN850X 2TB",            "wd-black-sn850x-2tb",    "western-digital","storage",            229m,   259m,          "WD-SN850X-2TB-001"),

                // Monitors & Accessories
                ("LG 27UL850 27\" 4K",             "lg-27ul850-4k",          "lg",             "monitors",           349m,   399m,          "LG-27UL850-001"),
                ("Sony WH-1000XM5",                "sony-wh-1000xm5",        "sony",           "headphones",         329m,   379m,          "SNY-WH1000XM5-001"),
                ("Anker PowerCore 20K",            "anker-powercore-20k",    "anker",          "power-banks",         59m,    69m,          "ANK-PWRCR20K-001"),
                ("Razer DeathAdder V3",            "razer-deathadder-v3",    "razer",          "mice",                89m,   109m,          "RZR-DA-V3-001"),
                ("Kingston Fury 32GB DDR5",        "kingston-fury-32gb-ddr5","kingston",       "pc-komponenten",     129m,   149m,          "KNG-FURY-32G-001")
            };

            foreach (var it in items)
            {
                if (!brandMap.TryGetValue(it.Brand, out var brand)) continue;
                if (!catMap.TryGetValue(it.Cat, out var cat)) continue;

                var p = new Product
                {
                    BrandId = brand.Id,
                    PrimaryCategoryId = cat.Id,
                    IsActive = true,
                    IsVisible = true,
                    Kind = Darwin.Domain.Enums.ProductKind.Simple
                };

                p.Translations.Add(new ProductTranslation
                {
                    Culture = "de-DE",
                    Name = it.Name,
                    Slug = it.Slug,
                    ShortDescription = null,
                    FullDescriptionHtml = null,
                    MetaTitle = null,
                    MetaDescription = null
                });

                db.Add(p);
                await db.SaveChangesAsync(ct); // ensure Product.Id for the variant FK

                // Map major (decimal NET) → minor (long) using domain Money helper.
                long priceMinor = Money.FromMajor(it.Price, "EUR").AmountMinor;
                long? compareMinor = it.Compare.HasValue
                    ? Money.FromMajor(it.Compare.Value, "EUR").AmountMinor
                    : (long?)null;

                // Use fixed IDs for first two variants to match Inventory/Cart seeds.
                Guid? forcedId = null;
                if (it.Slug == "iphone-15-pro-128") forcedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                else if (it.Slug == "galaxy-s24-256") forcedId = Guid.Parse("22222222-2222-2222-2222-222222222222");

                var v = new ProductVariant
                {
                    Id = forcedId ?? Guid.NewGuid(),
                    ProductId = p.Id,
                    Sku = it.SKU,
                    Currency = "EUR",
                    TaxCategoryId = standardTax.Id,
                    BasePriceNetMinor = priceMinor,
                    CompareAtPriceNetMinor = compareMinor,

                    // Simple stock and logistics defaults
                    StockOnHand = 100,
                    StockReserved = 0,
                    ReorderPoint = 10,
                    BackorderAllowed = false,
                    MinOrderQty = 1,
                    MaxOrderQty = null,
                    StepOrderQty = null,

                    // Packaging: rough, realistic dimensions in mm/g
                    PackageWeight = 350,  // 350 g phones / accessories default
                    PackageLength = 180,  // 180 mm
                    PackageWidth = 90,   // 90  mm
                    PackageHeight = 60,   // 60  mm
                    IsDigital = false
                };

                // Adjust dimensions for laptops/monitors/storage to avoid decimal→int? issues.
                if (it.Cat is "ultrabooks" or "business-laptops" or "gaming-laptops")
                {
                    v.PackageWeight = 1800; // 1.8 kg
                    v.PackageLength = 420;
                    v.PackageWidth = 310;
                    v.PackageHeight = 80;
                }
                else if (it.Cat is "monitors")
                {
                    v.PackageWeight = 5500;
                    v.PackageLength = 720;
                    v.PackageWidth = 500;
                    v.PackageHeight = 180;
                }
                else if (it.Cat is "storage" or "pc-komponenten")
                {
                    v.PackageWeight = 200;
                    v.PackageLength = 140;
                    v.PackageWidth = 100;
                    v.PackageHeight = 40;
                }

                db.Add(v);
                await db.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// Ensures the standard German VAT (19%) tax category exists and returns it.
        /// Uses name "Standard" to align with PricingSeedSection.
        /// </summary>
        private static async Task<TaxCategory> EnsureStandardTaxCategoryAsync(DarwinDbContext db, CancellationToken ct)
        {
            var tc = await db.TaxCategories.FirstOrDefaultAsync(x => x.Name == "Standard" && !x.IsDeleted, ct);
            if (tc != null) return tc;

            tc = new TaxCategory { Name = "Standard", VatRate = 0.19m };
            db.Add(tc);
            await db.SaveChangesAsync(ct);
            return tc;
        }

        /// <summary>
        /// Seeds a simple add-on group "Extended Warranty" with one option (duration)
        /// and attaches it to several categories. Uses db.Set&lt;T&gt; to avoid requiring DbSet properties.
        /// </summary>
        private static async Task SeedExtendedWarrantyAddOnsAsync(DarwinDbContext db, CancellationToken ct)
        {
            // Skip if a group with same name already exists.
            var anyGroup = await db.Set<AddOnGroup>()
                .AnyAsync(g => g.Name == "Extended Warranty" && !g.IsDeleted, ct);
            if (anyGroup) return;

            var group = new AddOnGroup
            {
                Name = "Extended Warranty",
                Currency = "EUR",
                SelectionMode = Domain.Enums.AddOnSelectionMode.Single
            };
            db.Add(group);
            await db.SaveChangesAsync(ct);

            var option = new AddOnOption
            {
                AddOnGroupId = group.Id,
                Label = "Duration",
                SortOrder = 0
            };
            db.Add(option);
            await db.SaveChangesAsync(ct);

            // Values: price delta in minor units (NET)
            db.AddRange(
                new AddOnOptionValue
                {
                    AddOnOptionId = option.Id,
                    Label = "1 Jahr",
                    PriceDeltaMinor = Money.FromMajor(0m, "EUR").AmountMinor,
                    SortOrder = 0,
                    IsActive = true
                },
                new AddOnOptionValue
                {
                    AddOnOptionId = option.Id,
                    Label = "2 Jahre",
                    PriceDeltaMinor = Money.FromMajor(49.99m, "EUR").AmountMinor,
                    SortOrder = 1,
                    IsActive = true
                },
                new AddOnOptionValue
                {
                    AddOnOptionId = option.Id,
                    Label = "3 Jahre",
                    PriceDeltaMinor = Money.FromMajor(89.99m, "EUR").AmountMinor,
                    SortOrder = 2,
                    IsActive = true
                }
            );
            await db.SaveChangesAsync(ct);

            // Attach to relevant categories
            var attachSlugs = new[] { "iphones", "android-phones", "ultrabooks", "business-laptops", "gaming-laptops" };
            var categories = await db.Categories
                .Include(c => c.Translations)
                .Where(c => c.Translations.Any(t => t.Culture == "de-DE" && attachSlugs.Contains(t.Slug)))
                .ToListAsync(ct);

            foreach (var c in categories)
            {
                // Idempotent join insert: uniqueness enforced by configuration (GroupId, CategoryId).
                var exists = await db.Set<AddOnGroupCategory>()
                    .AnyAsync(x => x.AddOnGroupId == group.Id && x.CategoryId == c.Id && !x.IsDeleted, ct);
                if (!exists)
                {
                    db.Add(new AddOnGroupCategory
                    {
                        AddOnGroupId = group.Id,
                        CategoryId = c.Id
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
