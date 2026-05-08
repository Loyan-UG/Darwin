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
    /// - Storefront products (>= 20) with one default variant each.
    /// - A demo Add-on group (Extended Warranty) attached to relevant categories.
    /// 
    /// All money is persisted as NET amounts in minor units (long), see ProductVariant fields.
    /// Dimensions are stored as SI units (mm / g) per domain guidance.
    /// </summary>
public sealed class CatalogSeedSection
{
    private const string EnglishCulture = "en-US";

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

        await EnsureSeededStorefrontProductsAsync(db, ct);

        await EnsureEnglishCatalogTranslationsAsync(db, ct);

        await EnsureStorefrontSeedMediaAsync(db, ct);

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
                // Anker line is represented directly; the retired Sandisk slot is intentionally not seeded
            };

            foreach (var b in brands)
            {
                var brand = new Brand { Slug = b.Slug };
                brand.Translations.Add(new BrandTranslation
                {
                    Culture = DomainDefaults.DefaultCulture,
                    Name = b.Name
                });
                brand.Translations.Add(new BrandTranslation
                {
                    Culture = EnglishCulture,
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
        var cats = new (string Slug, string Name, string? Desc, string MetaTitle, string? MetaDescription, string EnName, string? EnDesc, string EnMetaTitle, string? EnMetaDescription)[]
        {
                ("iphones",            "iPhones",                 "Apple iPhone Modelle",                             "iPhones online kaufen",                  "Apple iPhone Modelle mit aktuellen Angeboten entdecken",                      "iPhones",              "Apple iPhone models",                                      "Shop iPhones online",                  "Discover Apple iPhone models with current offers"),
                ("android-phones",     "Android-Smartphones",     "Android Geraete von fuehrenden Herstellern",         "Android-Smartphones kaufen",             "Android Geraete von Samsung, Google, Xiaomi und weiteren Marken vergleichen", "Android phones",       "Android devices from leading manufacturers",              "Shop Android phones",                  "Compare Android devices from Samsung, Google, Xiaomi, and more"),
                ("ultrabooks",         "Ultrabooks",              "Leichte, mobile Notebooks",                        "Ultrabooks fuer mobiles Arbeiten",       "Leichte Notebooks fuer Alltag, Reisen und produktives Arbeiten",              "Ultrabooks",           "Lightweight, mobile notebooks",                          "Ultrabooks for mobile work",           "Lightweight notebooks for everyday work, travel, and productivity"),
                ("business-laptops",   "Business-Laptops",        "Zuverlaessige Laptops fuer den Arbeitsalltag",       "Business-Laptops fuer Teams",            "Zuverlaessige Business-Notebooks fuer Buero, Homeoffice und unterwegs",        "Business laptops",     "Reliable laptops for everyday work",                     "Business laptops for teams",           "Reliable business notebooks for office, home office, and travel"),
                ("gaming-laptops",     "Gaming-Laptops",          "Leistungsstarke Laptops fuer Gaming",               "Gaming-Laptops mit starker Leistung",    "Gaming-Notebooks mit performanter Grafik, Displays und Kuehlung",             "Gaming laptops",       "High-performance laptops for gaming",                    "High-performance gaming laptops",      "Gaming notebooks with strong graphics, displays, and cooling"),
                ("mice",               "Maeuse",                   "Praezise Computer-Maeuse",                           "Computer-Maeuse fuer Arbeit und Gaming", "Praezise Maeuse fuer Office, Kreativarbeit und Gaming-Setups",                 "Mice",                 "Precise computer mice",                                  "Computer mice for work and gaming",    "Precise mice for office, creative work, and gaming setups"),
                ("keyboards",          "Tastaturen",              "Mechanische und Office-Tastaturen",                "Tastaturen fuer jedes Setup",            "Mechanische Tastaturen und Office-Keyboards fuer produktives Arbeiten",        "Keyboards",            "Mechanical and office keyboards",                        "Keyboards for every setup",            "Mechanical and office keyboards for productive work"),
                ("storage",            "Speicher",                "NVMe/SSD/HDD Speicherloesungen",                    "SSD, NVMe und HDD Speicher",             "Schnelle Speicherloesungen fuer PCs, Laptops und Backups",                     "Storage",              "NVMe/SSD/HDD storage solutions",                         "SSD, NVMe, and HDD storage",           "Fast storage solutions for PCs, laptops, and backups"),
                ("monitors",           "Monitore",                "4K/144Hz/Ultrawide Monitore",                      "Monitore fuer Arbeit und Gaming",        "4K-, 144Hz- und Ultrawide-Monitore fuer moderne Arbeitsplaetze",              "Monitors",             "4K, 144Hz, and ultrawide monitors",                      "Monitors for work and gaming",         "4K, 144Hz, and ultrawide monitors for modern workspaces"),
                ("headphones",         "Kopfhoerer",               "Over-Ear/ANC/BT Kopfhoerer",                        "Kopfhoerer mit ANC und Bluetooth",       "Over-Ear-, ANC- und Bluetooth-Kopfhoerer fuer Alltag und Reisen",              "Headphones",           "Over-ear, ANC, and Bluetooth headphones",                "ANC and Bluetooth headphones",         "Over-ear, ANC, and Bluetooth headphones for everyday use and travel"),
                ("power-banks",        "Powerbanks",              "Mobile Ladegeraete",                                "Powerbanks fuer unterwegs",              "Mobile Ladegeraete fuer Smartphones, Tablets und Zubehoer",                    "Power banks",          "Portable chargers",                                      "Power banks for travel",               "Portable chargers for phones, tablets, and accessories"),
                ("pc-komponenten",     "PC-Komponenten",          "RAM, Mainboards, Netzteile u. a.",                 "PC-Komponenten fuer Builds",             "RAM, Mainboards, Netzteile und weitere Komponenten fuer PC-Builds",            "PC components",        "RAM, motherboards, power supplies, and more",            "PC components for builds",             "RAM, motherboards, power supplies, and more components for PC builds")
        };

            foreach (var c in cats)
            {
                var cat = new Category { IsActive = true, SortOrder = 0 };
                cat.Translations.Add(new CategoryTranslation
                {
                    Culture = DomainDefaults.DefaultCulture,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Desc,
                    MetaTitle = c.MetaTitle,
                    MetaDescription = c.MetaDescription
                });
                cat.Translations.Add(new CategoryTranslation
                {
                    Culture = EnglishCulture,
                    Name = c.EnName,
                    Slug = c.Slug,
                    Description = c.EnDesc,
                    MetaTitle = c.EnMetaTitle,
                    MetaDescription = c.EnMetaDescription
                });
                db.Add(cat);
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Creates >= 20 storefront products (German locale), each with one simple variant.
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
                        .Where(t => t.Culture == DomainDefaults.DefaultCulture)
                        .Select(t => t.Slug)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToDictionaryAsync(x => x.Slug, x => x.Cat, ct);

            var standardTax = await EnsureStandardTaxCategoryAsync(db, ct);

            var items = GetStorefrontProductSeeds();

            foreach (var it in items)
            {
                if (!brandMap.TryGetValue(it.Brand, out var brand)) continue;
                if (!catMap.TryGetValue(it.Cat, out var cat)) continue;

                var germanCopy = BuildProductSeedCopy(it.Name, it.Cat, english: false);
                var englishCopy = BuildProductSeedCopy(it.EnName, it.Cat, english: true);

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
                    Culture = DomainDefaults.DefaultCulture,
                    Name = it.Name,
                    Slug = it.Slug,
                    ShortDescription = germanCopy.ShortDescription,
                    FullDescriptionHtml = germanCopy.FullDescriptionHtml,
                    MetaTitle = germanCopy.MetaTitle,
                    MetaDescription = germanCopy.MetaDescription
                });
                p.Translations.Add(new ProductTranslation
                {
                    Culture = EnglishCulture,
                    Name = it.EnName,
                    Slug = it.Slug,
                    ShortDescription = englishCopy.ShortDescription,
                    FullDescriptionHtml = englishCopy.FullDescriptionHtml,
                    MetaTitle = it.EnName,
                    MetaDescription = englishCopy.MetaDescription
                });

                db.Add(p);
                await db.SaveChangesAsync(ct); // ensure Product.Id for the variant FK

                // Map major (decimal NET) ? minor (long) using domain Money helper.
                long priceMinor = Money.FromMajor(it.Price, DomainDefaults.DefaultCurrency).AmountMinor;
                long? compareMinor = it.Compare.HasValue
                    ? Money.FromMajor(it.Compare.Value, DomainDefaults.DefaultCurrency).AmountMinor
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
                    Currency = DomainDefaults.DefaultCurrency,
                    TaxCategoryId = standardTax.Id,
                    BasePriceNetMinor = priceMinor,
                    CompareAtPriceNetMinor = compareMinor,

                    // Inventory is managed in StockLevel (multi-warehouse model).

                    // Packaging: rough, realistic dimensions in mm/g
                    PackageWeight = 350,  // 350 g phones / accessories default
                    PackageLength = 180,  // 180 mm
                    PackageWidth = 90,   // 90  mm
                    PackageHeight = 60,   // 60  mm
                    IsDigital = false
                };

                // Adjust dimensions for laptops/monitors/storage to avoid decimal?int? issues.
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

        private static async Task EnsureSeededStorefrontProductsAsync(DarwinDbContext db, CancellationToken ct)
        {
            if (!await db.Products.AnyAsync(ct))
            {
                await CreateProductsDeAsync(db, ct);
                return;
            }

            var brandMap = await db.Brands
                .Include(b => b.Translations)
                .ToDictionaryAsync(b => b.Slug ?? string.Empty, b => b, ct);

            var catMap = await db.Categories
                .Include(c => c.Translations)
                .Select(c => new
                {
                    Cat = c,
                    Slug = c.Translations
                        .Where(t => t.Culture == DomainDefaults.DefaultCulture)
                        .Select(t => t.Slug)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToDictionaryAsync(x => x.Slug, x => x.Cat, ct);

            var standardTax = await EnsureStandardTaxCategoryAsync(db, ct);
            var existingProducts = await db.Products
                .Include(x => x.Translations)
                .Where(x => x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture))
                .ToListAsync(ct);
            var variantsBySku = await db.ProductVariants
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.Sku, x => x, ct);
            var variantsById = await db.ProductVariants
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x, ct);

            foreach (var item in GetStorefrontProductSeeds())
            {
                if (!brandMap.TryGetValue(item.Brand, out var brand)) continue;
                if (!catMap.TryGetValue(item.Cat, out var category)) continue;

                var product = existingProducts.FirstOrDefault(x =>
                    x.Translations.Any(t =>
                        t.Culture == DomainDefaults.DefaultCulture &&
                        string.Equals(t.Slug, item.Slug, StringComparison.OrdinalIgnoreCase)));

                if (product == null)
                {
                    var germanCopy = BuildProductSeedCopy(item.Name, item.Cat, english: false);
                    var englishCopy = BuildProductSeedCopy(item.EnName, item.Cat, english: true);
                    product = new Product
                    {
                        BrandId = brand.Id,
                        PrimaryCategoryId = category.Id,
                        IsActive = true,
                        IsVisible = true,
                        Kind = Darwin.Domain.Enums.ProductKind.Simple
                    };
                    product.Translations.Add(new ProductTranslation
                    {
                        Culture = DomainDefaults.DefaultCulture,
                        Name = item.Name,
                        Slug = item.Slug,
                        ShortDescription = germanCopy.ShortDescription,
                        FullDescriptionHtml = germanCopy.FullDescriptionHtml,
                        MetaTitle = germanCopy.MetaTitle,
                        MetaDescription = germanCopy.MetaDescription
                    });
                    product.Translations.Add(new ProductTranslation
                    {
                        Culture = EnglishCulture,
                        Name = item.EnName,
                        Slug = item.Slug,
                        ShortDescription = englishCopy.ShortDescription,
                        FullDescriptionHtml = englishCopy.FullDescriptionHtml,
                        MetaTitle = item.EnName,
                        MetaDescription = englishCopy.MetaDescription
                    });
                    db.Add(product);
                    existingProducts.Add(product);
                    await db.SaveChangesAsync(ct);
                }

                product.BrandId = brand.Id;
                product.PrimaryCategoryId = category.Id;
                product.IsActive = true;
                product.IsVisible = true;
                product.Kind = Darwin.Domain.Enums.ProductKind.Simple;

                var forcedVariantId = item.Slug == "iphone-15-pro-128"
                    ? Guid.Parse("11111111-1111-1111-1111-111111111111")
                    : item.Slug == "galaxy-s24-256"
                        ? Guid.Parse("22222222-2222-2222-2222-222222222222")
                        : (Guid?)null;

                if (!variantsBySku.TryGetValue(item.SKU, out var variant) &&
                    (!forcedVariantId.HasValue || !variantsById.TryGetValue(forcedVariantId.Value, out variant)))
                {
                    variant = new ProductVariant
                    {
                        Id = forcedVariantId ?? Guid.NewGuid(),
                        ProductId = product.Id,
                        Sku = item.SKU
                    };
                    db.Add(variant);
                    variantsById[variant.Id] = variant;
                }

                variant.ProductId = product.Id;
                variant.Sku = item.SKU;
                variantsBySku[item.SKU] = variant;
                variant.Currency = DomainDefaults.DefaultCurrency;
                variant.TaxCategoryId = standardTax.Id;
                variant.BasePriceNetMinor = Money.FromMajor(item.Price, DomainDefaults.DefaultCurrency).AmountMinor;
                variant.CompareAtPriceNetMinor = item.Compare.HasValue
                    ? Money.FromMajor(item.Compare.Value, DomainDefaults.DefaultCurrency).AmountMinor
                    : null;
                variant.PackageWeight ??= item.Cat is "ultrabooks" or "business-laptops" or "gaming-laptops"
                    ? 1800
                    : item.Cat is "monitors"
                        ? 5500
                        : item.Cat is "storage" or "pc-komponenten"
                            ? 200
                            : 350;
                variant.PackageLength ??= item.Cat is "monitors" ? 720 : 180;
                variant.PackageWidth ??= item.Cat is "monitors" ? 500 : 90;
                variant.PackageHeight ??= item.Cat is "monitors" ? 180 : 60;
                variant.IsDigital = false;
            }

            await db.SaveChangesAsync(ct);
        }

        private static (string Name, string EnName, string Slug, string Brand, string Cat, decimal Price, decimal? Compare, string SKU)[] GetStorefrontProductSeeds()
        {
            return new (string Name, string EnName, string Slug, string Brand, string Cat, decimal Price, decimal? Compare, string SKU)[]
            {
                ("iPhone 15 Pro 128GB", "iPhone 15 Pro 128GB", "iphone-15-pro-128", "apple", "iphones", 1199m, null, "APL-IP15P-128-001"),
                ("Samsung Galaxy S24 256GB", "Samsung Galaxy S24 256GB", "galaxy-s24-256", "samsung", "android-phones", 999m, 1099m, "SMS-S24-256-001"),
                ("Google Pixel 9 128GB", "Google Pixel 9 128GB", "pixel-9-128", "google", "android-phones", 799m, 869m, "GGL-PX9-128-001"),
                ("Xiaomi 14 Pro 256GB", "Xiaomi 14 Pro 256GB", "xiaomi-14-pro-256", "xiaomi", "android-phones", 799m, null, "XMI-14P-256-001"),
                ("OnePlus 12 256GB", "OnePlus 12 256GB", "oneplus-12-256", "oneplus", "android-phones", 869m, 949m, "1P-12-256-001"),
                ("MacBook Air 13 M3 8/256", "MacBook Air 13 M3 8/256", "macbook-air-13-m3", "apple", "ultrabooks", 1299m, 1399m, "APL-MBA13-M3-001"),
                ("Dell XPS 13 16/512", "Dell XPS 13 16/512", "dell-xps-13-2025", "dell", "ultrabooks", 1599m, null, "DEL-XPS13-001"),
                ("HP Envy 15", "HP Envy 15", "hp-envy-15", "hp", "business-laptops", 1199m, 1299m, "HP-ENVY15-001"),
                ("Lenovo ThinkPad X1 Carbon", "Lenovo ThinkPad X1 Carbon", "thinkpad-x1-carbon", "lenovo", "business-laptops", 1899m, 1999m, "LNV-X1C-001"),
                ("ASUS ROG Zephyrus G14", "ASUS ROG Zephyrus G14", "rog-g14-2025", "asus", "gaming-laptops", 1799m, 1899m, "ASU-G14-001"),
                ("Logitech MX Master 3S", "Logitech MX Master 3S", "logitech-mx-master-3s", "logitech", "mice", 119m, 139m, "LOG-MX3S-001"),
                ("Corsair K70 RGB Pro", "Corsair K70 RGB Pro", "corsair-k70-rgb-pro", "corsair", "keyboards", 179m, null, "COR-K70-RGBP-001"),
                ("Samsung 980 Pro 1TB NVMe", "Samsung 980 Pro 1TB NVMe", "samsung-980-pro-1tb", "samsung", "storage", 129m, 159m, "SMS-980PRO-1TB-001"),
                ("Seagate BarraCuda 4TB", "Seagate BarraCuda 4TB", "seagate-barracuda-4tb", "seagate", "storage", 99m, null, "SEA-BC-4TB-001"),
                ("WD Black SN850X 2TB", "WD Black SN850X 2TB", "wd-black-sn850x-2tb", "western-digital", "storage", 229m, 259m, "WD-SN850X-2TB-001"),
                ("LG 27UL850 27\" 4K", "LG 27UL850 27\" 4K", "lg-27ul850-4k", "lg", "monitors", 349m, 399m, "LG-27UL850-001"),
                ("Sony WH-1000XM5", "Sony WH-1000XM5", "sony-wh-1000xm5", "sony", "headphones", 329m, 379m, "SNY-WH1000XM5-001"),
                ("Anker PowerCore 20K", "Anker PowerCore 20K", "anker-powercore-20k", "anker", "power-banks", 59m, 69m, "ANK-PWRCR20K-001"),
                ("Razer DeathAdder V3", "Razer DeathAdder V3", "razer-deathadder-v3", "razer", "mice", 89m, 109m, "RZR-DA-V3-001"),
                ("Kingston Fury 32GB DDR5", "Kingston Fury 32GB DDR5", "kingston-fury-32gb-ddr5", "kingston", "pc-komponenten", 129m, 149m, "KNG-FURY-32G-001")
            };
        }

        private static async Task EnsureEnglishCatalogTranslationsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var categoryMap = new Dictionary<string, (string Name, string? Description, string MetaTitle, string? MetaDescription)>(StringComparer.OrdinalIgnoreCase)
            {
                ["iphones"] = ("iPhones", "Apple iPhone models", "Shop iPhones online", "Discover Apple iPhone models with current offers"),
                ["android-phones"] = ("Android phones", "Android devices from leading manufacturers", "Shop Android phones", "Compare Android devices from Samsung, Google, Xiaomi, and more"),
                ["ultrabooks"] = ("Ultrabooks", "Lightweight, mobile notebooks", "Ultrabooks for mobile work", "Lightweight notebooks for everyday work, travel, and productivity"),
                ["business-laptops"] = ("Business laptops", "Reliable laptops for everyday work", "Business laptops for teams", "Reliable business notebooks for office, home office, and travel"),
                ["gaming-laptops"] = ("Gaming laptops", "High-performance laptops for gaming", "High-performance gaming laptops", "Gaming notebooks with strong graphics, displays, and cooling"),
                ["mice"] = ("Mice", "Precise computer mice", "Computer mice for work and gaming", "Precise mice for office, creative work, and gaming setups"),
                ["keyboards"] = ("Keyboards", "Mechanical and office keyboards", "Keyboards for every setup", "Mechanical and office keyboards for productive work"),
                ["storage"] = ("Storage", "NVMe/SSD/HDD storage solutions", "SSD, NVMe, and HDD storage", "Fast storage solutions for PCs, laptops, and backups"),
                ["monitors"] = ("Monitors", "4K, 144Hz, and ultrawide monitors", "Monitors for work and gaming", "4K, 144Hz, and ultrawide monitors for modern workspaces"),
                ["headphones"] = ("Headphones", "Over-ear, ANC, and Bluetooth headphones", "ANC and Bluetooth headphones", "Over-ear, ANC, and Bluetooth headphones for everyday use and travel"),
                ["power-banks"] = ("Power banks", "Portable chargers", "Power banks for travel", "Portable chargers for phones, tablets, and accessories"),
                ["pc-komponenten"] = ("PC components", "RAM, motherboards, power supplies, and more", "PC components for builds", "RAM, motherboards, power supplies, and more components for PC builds")
            };

            var categories = await db.Categories
                .Include(x => x.Translations)
                .Where(x => x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture))
                .ToListAsync(ct);

            foreach (var category in categories)
            {
                var defaultTranslation = category.Translations.FirstOrDefault(t => t.Culture == DomainDefaults.DefaultCulture);
                if (defaultTranslation == null || string.IsNullOrWhiteSpace(defaultTranslation.Slug))
                {
                    continue;
                }

                if (!categoryMap.TryGetValue(defaultTranslation.Slug, out var english))
                {
                    english = (defaultTranslation.Name, defaultTranslation.Description, defaultTranslation.MetaTitle ?? defaultTranslation.Name, defaultTranslation.MetaDescription ?? defaultTranslation.Description);
                }

                var englishTranslation = category.Translations.FirstOrDefault(t => t.Culture == EnglishCulture);
                if (englishTranslation == null)
                {
                    englishTranslation = new CategoryTranslation { Culture = EnglishCulture };
                    category.Translations.Add(englishTranslation);
                }

                englishTranslation.Name = string.IsNullOrWhiteSpace(englishTranslation.Name) ? english.Name : englishTranslation.Name;
                englishTranslation.Slug = string.IsNullOrWhiteSpace(englishTranslation.Slug) ? defaultTranslation.Slug : englishTranslation.Slug;
                englishTranslation.Description = string.IsNullOrWhiteSpace(englishTranslation.Description) ? english.Description : englishTranslation.Description;
                englishTranslation.MetaTitle = string.IsNullOrWhiteSpace(englishTranslation.MetaTitle) ? english.MetaTitle : englishTranslation.MetaTitle;
                englishTranslation.MetaDescription = string.IsNullOrWhiteSpace(englishTranslation.MetaDescription) ? english.MetaDescription : englishTranslation.MetaDescription;

                defaultTranslation.MetaTitle = string.IsNullOrWhiteSpace(defaultTranslation.MetaTitle) ? defaultTranslation.Name : defaultTranslation.MetaTitle;
                defaultTranslation.MetaDescription = string.IsNullOrWhiteSpace(defaultTranslation.MetaDescription) ? defaultTranslation.Description : defaultTranslation.MetaDescription;
            }

            var products = await db.Products
                .Include(x => x.Translations)
                .Where(x => x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture))
                .ToListAsync(ct);

            var primaryCategoryIds = products
                .Where(x => x.PrimaryCategoryId.HasValue)
                .Select(x => x.PrimaryCategoryId!.Value)
                .Distinct()
                .ToList();
            var categorySlugsById = await db.Categories
                .Include(x => x.Translations)
                .Where(x => primaryCategoryIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Translations
                        .Where(t => t.Culture == DomainDefaults.DefaultCulture)
                        .Select(t => t.Slug)
                        .FirstOrDefault() ?? string.Empty,
                    ct);

            foreach (var product in products)
            {
                var defaultTranslation = product.Translations.FirstOrDefault(t => t.Culture == DomainDefaults.DefaultCulture);
                if (defaultTranslation == null || string.IsNullOrWhiteSpace(defaultTranslation.Slug))
                {
                    continue;
                }

                var englishTranslation = product.Translations.FirstOrDefault(t => t.Culture == EnglishCulture);
                if (englishTranslation == null)
                {
                    englishTranslation = new ProductTranslation { Culture = EnglishCulture };
                    product.Translations.Add(englishTranslation);
                }

                var productCategorySlug =
                    product.PrimaryCategoryId.HasValue &&
                    categorySlugsById.TryGetValue(product.PrimaryCategoryId.Value, out var categorySlug)
                        ? categorySlug
                        : string.Empty;
                var germanCopy = BuildProductSeedCopy(defaultTranslation.Name, productCategorySlug, english: false);
                defaultTranslation.ShortDescription = string.IsNullOrWhiteSpace(defaultTranslation.ShortDescription) ? germanCopy.ShortDescription : defaultTranslation.ShortDescription;
                defaultTranslation.FullDescriptionHtml = string.IsNullOrWhiteSpace(defaultTranslation.FullDescriptionHtml) ? germanCopy.FullDescriptionHtml : defaultTranslation.FullDescriptionHtml;
                defaultTranslation.MetaTitle = string.IsNullOrWhiteSpace(defaultTranslation.MetaTitle) ? germanCopy.MetaTitle : defaultTranslation.MetaTitle;
                defaultTranslation.MetaDescription = string.IsNullOrWhiteSpace(defaultTranslation.MetaDescription) ? germanCopy.MetaDescription : defaultTranslation.MetaDescription;

                englishTranslation.Name = string.IsNullOrWhiteSpace(englishTranslation.Name) ? defaultTranslation.Name : englishTranslation.Name;
                englishTranslation.Slug = string.IsNullOrWhiteSpace(englishTranslation.Slug) ? defaultTranslation.Slug : englishTranslation.Slug;
                var englishCopy = BuildProductSeedCopy(englishTranslation.Name, productCategorySlug, english: true);
                englishTranslation.ShortDescription = string.IsNullOrWhiteSpace(englishTranslation.ShortDescription) ? englishCopy.ShortDescription : englishTranslation.ShortDescription;
                englishTranslation.FullDescriptionHtml = string.IsNullOrWhiteSpace(englishTranslation.FullDescriptionHtml) ? englishCopy.FullDescriptionHtml : englishTranslation.FullDescriptionHtml;
                englishTranslation.MetaTitle = string.IsNullOrWhiteSpace(englishTranslation.MetaTitle) ? englishCopy.MetaTitle : englishTranslation.MetaTitle;
                englishTranslation.MetaDescription = string.IsNullOrWhiteSpace(englishTranslation.MetaDescription) ? englishCopy.MetaDescription : englishTranslation.MetaDescription;
            }

            await db.SaveChangesAsync(ct);
        }

        private static (string ShortDescription, string FullDescriptionHtml, string MetaTitle, string MetaDescription) BuildProductSeedCopy(
            string name,
            string category,
            bool english)
        {
            var categoryHint = category switch
            {
                "iphones" => english
                    ? "Apple iPhone with a balanced mix of performance, camera quality, and everyday reliability."
                    : "Apple iPhone mit ausgewogener Leistung, starker Kamera und zuverlaessiger Alltagstauglichkeit.",
                "android-phones" => english
                    ? "Android smartphone for fast apps, modern photography, and flexible mobile workflows."
                    : "Android-Smartphone fuer schnelle Apps, moderne Fotografie und flexible mobile Workflows.",
                "ultrabooks" or "business-laptops" => english
                    ? "Portable notebook for productive work, travel, meetings, and everyday business use."
                    : "Mobiles Notebook fuer produktives Arbeiten, Reisen, Meetings und den Business-Alltag.",
                "gaming-laptops" => english
                    ? "Performance laptop for gaming, streaming, and demanding creative workloads."
                    : "Leistungsstarker Laptop fuer Gaming, Streaming und anspruchsvolle Kreativarbeit.",
                "mice" or "keyboards" => english
                    ? "Reliable peripheral for precise control, comfortable input, and a cleaner desk setup."
                    : "Zuverlaessiges Peripheriegeraet fuer praezise Kontrolle, komfortable Eingabe und ein sauberes Setup.",
                "storage" or "pc-komponenten" => english
                    ? "PC component for upgrades, fast storage, and dependable system builds."
                    : "PC-Komponente fuer Upgrades, schnellen Speicher und zuverlaessige System-Builds.",
                "monitors" => english
                    ? "Display for sharp visuals, focused work, entertainment, and multi-window productivity."
                    : "Monitor fuer scharfe Darstellung, fokussiertes Arbeiten, Entertainment und Multi-Window-Produktivitaet.",
                "headphones" => english
                    ? "Headphones for focused listening, calls, commuting, and everyday media."
                    : "Kopfhoerer fuer fokussiertes Hoeren, Calls, Pendeln und taegliche Mediennutzung.",
                "power-banks" => english
                    ? "Portable charging companion for phones, tablets, and accessories on the go."
                    : "Mobiler Ladebegleiter fuer Smartphones, Tablets und Zubehoer unterwegs.",
                _ => english
                    ? "Curated electronics product for the Darwin storefront catalog."
                    : "Kuratierter Elektronikartikel fuer den Darwin Storefront-Katalog."
            };

            if (english)
            {
                var shortDescription = $"{name}: {categoryHint}";
                return (
                    shortDescription,
                    $"<p>{shortDescription}</p><p>Seeded with localized storefront copy so catalog browsing, product detail pages, and SEO previews have representative English content.</p>",
                    $"{name} | Darwin Store",
                    $"{name} in the Darwin catalog. {categoryHint}");
            }

            var germanShortDescription = $"{name}: {categoryHint}";
            return (
                germanShortDescription,
                $"<p>{germanShortDescription}</p><p>Mit lokalisiertem Storefront-Text fuer Katalog, Produktdetailseite und SEO-Vorschau im deutschen Seed-Datenbestand.</p>",
                $"{name} | Darwin Store",
                $"{name} im Darwin Katalog. {categoryHint}");
        }

        /// <summary>
        /// Ensures a representative subset of public products has primary media attached so storefront browsing
        /// has real image data even before a content team curates the full catalog.
        /// </summary>
        private static async Task EnsureStorefrontSeedMediaAsync(DarwinDbContext db, CancellationToken ct)
        {
            var desiredAssignments = new[]
            {
                new { ProductSlug = "iphone-15-pro-128", MediaUrl = "/media/storefront/hero-electronics.jpg", Role = "Primary" },
                new { ProductSlug = "galaxy-s24-256", MediaUrl = "/media/storefront/smartphones.jpg", Role = "Primary" },
                new { ProductSlug = "pixel-9-128", MediaUrl = "/media/storefront/smartphones.jpg", Role = "Gallery" },
                new { ProductSlug = "macbook-air-13-m3", MediaUrl = "/media/storefront/laptops-collection.jpg", Role = "Primary" },
                new { ProductSlug = "dell-xps-13-2025", MediaUrl = "/media/storefront/laptops-collection.jpg", Role = "Gallery" },
                new { ProductSlug = "logitech-mx-master-3s", MediaUrl = "/media/storefront/accessories.jpg", Role = "Primary" },
                new { ProductSlug = "sony-wh-1000xm5", MediaUrl = "/media/storefront/accessories.jpg", Role = "Gallery" },
                new { ProductSlug = "anker-powercore-20k", MediaUrl = "/media/storefront/accessories.jpg", Role = "Gallery" }
            };

            var mediaUrls = desiredAssignments.Select(x => x.MediaUrl).Distinct().ToArray();
            var productSlugs = desiredAssignments.Select(x => x.ProductSlug).Distinct().ToArray();

            var mediaByUrl = await db.MediaAssets
                .Where(x => mediaUrls.Contains(x.Url))
                .ToDictionaryAsync(x => x.Url, x => x, ct);

            if (mediaByUrl.Count == 0)
            {
                return;
            }

            var productLookup = await db.Products
                .Include(x => x.Translations)
                .Include(x => x.Media)
                .Where(x => x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture && productSlugs.Contains(t.Slug)))
                .ToListAsync(ct);

            foreach (var assignment in desiredAssignments)
            {
                var product = productLookup.FirstOrDefault(
                    x => x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture && t.Slug == assignment.ProductSlug));
                if (product == null || !mediaByUrl.TryGetValue(assignment.MediaUrl, out var media))
                {
                    continue;
                }

                var alreadyLinked = product.Media.Any(x => x.MediaAssetId == media.Id);
                if (alreadyLinked)
                {
                    continue;
                }

                product.Media.Add(new ProductMedia
                {
                    ProductId = product.Id,
                    MediaAssetId = media.Id,
                    SortOrder = product.Media.Count,
                    Role = assignment.Role
                });
            }

            await db.SaveChangesAsync(ct);
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
            var group = await db.Set<AddOnGroup>()
                .Include(g => g.Translations)
                .FirstOrDefaultAsync(g => g.Name == "Extended Warranty" && !g.IsDeleted, ct);
            if (group == null)
            {
                group = new AddOnGroup
                {
                    Name = "Extended Warranty",
                    Currency = DomainDefaults.DefaultCurrency,
                    SelectionMode = Domain.Enums.AddOnSelectionMode.Single
                };
                db.Add(group);
            }

            group.Currency = DomainDefaults.DefaultCurrency;
            group.SelectionMode = Domain.Enums.AddOnSelectionMode.Single;
            UpsertAddOnGroupTranslation(group, DomainDefaults.DefaultCulture, "Erweiterte Garantie");
            UpsertAddOnGroupTranslation(group, EnglishCulture, "Extended Warranty");
            await db.SaveChangesAsync(ct);

            var option = await db.Set<AddOnOption>()
                .Include(o => o.Translations)
                .FirstOrDefaultAsync(o => o.AddOnGroupId == group.Id && o.SortOrder == 0 && !o.IsDeleted, ct);
            if (option == null)
            {
                option = new AddOnOption
                {
                    AddOnGroupId = group.Id,
                    SortOrder = 0
                };
                db.Add(option);
            }

            option.Label = "Duration";
            UpsertAddOnOptionTranslation(option, DomainDefaults.DefaultCulture, "Laufzeit");
            UpsertAddOnOptionTranslation(option, EnglishCulture, "Duration");
            await db.SaveChangesAsync(ct);

            // Values: price delta in minor units (NET)
            await UpsertAddOnValueAsync(db, option.Id, 0, "1 Jahr", "1 Jahr", "1 year", 0m, ct);
            await UpsertAddOnValueAsync(db, option.Id, 1, "2 Jahre", "2 Jahre", "2 years", 49.99m, ct);
            await UpsertAddOnValueAsync(db, option.Id, 2, "3 Jahre", "3 Jahre", "3 years", 89.99m, ct);
            await db.SaveChangesAsync(ct);
            // Attach to relevant categories
            var attachSlugs = new[] { "iphones", "android-phones", "ultrabooks", "business-laptops", "gaming-laptops" };
            var categories = await db.Categories
                .Include(c => c.Translations)
                .Where(c => c.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture && attachSlugs.Contains(t.Slug)))
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

        private static void UpsertAddOnGroupTranslation(AddOnGroup group, string culture, string name)
        {
            var translation = group.Translations.FirstOrDefault(x => x.Culture == culture);
            if (translation == null)
            {
                translation = new AddOnGroupTranslation { Culture = culture };
                group.Translations.Add(translation);
            }

            translation.Name = name;
        }

        private static void UpsertAddOnOptionTranslation(AddOnOption option, string culture, string label)
        {
            var translation = option.Translations.FirstOrDefault(x => x.Culture == culture);
            if (translation == null)
            {
                translation = new AddOnOptionTranslation { Culture = culture };
                option.Translations.Add(translation);
            }

            translation.Label = label;
        }

        private static void UpsertAddOnOptionValueTranslation(AddOnOptionValue value, string culture, string label)
        {
            var translation = value.Translations.FirstOrDefault(x => x.Culture == culture);
            if (translation == null)
            {
                translation = new AddOnOptionValueTranslation { Culture = culture };
                value.Translations.Add(translation);
            }

            translation.Label = label;
        }

        private static async Task UpsertAddOnValueAsync(
            DarwinDbContext db,
            Guid optionId,
            int sortOrder,
            string fallbackLabel,
            string germanLabel,
            string englishLabel,
            decimal priceDeltaMajor,
            CancellationToken ct)
        {
            var value = await db.Set<AddOnOptionValue>()
                .Include(v => v.Translations)
                .FirstOrDefaultAsync(v => v.AddOnOptionId == optionId && v.SortOrder == sortOrder && !v.IsDeleted, ct);
            if (value == null)
            {
                value = new AddOnOptionValue
                {
                    AddOnOptionId = optionId,
                    SortOrder = sortOrder
                };
                db.Add(value);
            }

            value.Label = fallbackLabel;
            value.PriceDeltaMinor = Money.FromMajor(priceDeltaMajor, DomainDefaults.DefaultCurrency).AmountMinor;
            value.IsActive = true;
            UpsertAddOnOptionValueTranslation(value, DomainDefaults.DefaultCulture, germanLabel);
            UpsertAddOnOptionValueTranslation(value, EnglishCulture, englishLabel);
        }
    }
}
