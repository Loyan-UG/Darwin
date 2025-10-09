using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Pricing;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds baseline catalog data for a computer & electronics store:
    /// - Brands (≥ 20 well-known electronics/computer brands)
    /// - Categories (≥ 20 categories with a small hierarchy)
    /// - Products (≥ 20 products) with translations, a default simple variant, and optional media links
    /// 
    /// Idempotent: skips creation when data already exists.
    /// All localized fields use "de-DE" for the initial culture.
    /// Prices are stored in integer minor units (EUR cents).
    /// </summary>
    public sealed class CatalogSeedSection
    {
        private readonly ILogger<CatalogSeedSection> _logger;

        public CatalogSeedSection(ILogger<CatalogSeedSection> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entry point called by <see cref="DataSeeder"/>. Ensures brands, categories, and products exist.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Catalog (brands/categories/products) ...");

            // 1) Brands
            if (!await db.Brands.AnyAsync(ct))
            {
                var brands = CreateBrandsDe();
                db.Brands.AddRange(brands);
                await db.SaveChangesAsync(ct);
            }

            // Map: Slug -> BrandId for quick lookup
            var brandMap = await db.Brands
                .Select(b => new { b.Id, Slug = b.Slug ?? string.Empty })
                .ToDictionaryAsync(x => x.Slug, x => x.Id, ct);

            // 2) Categories
            if (!await db.Categories.AnyAsync(ct))
            {
                var cats = CreateCategoriesDe();
                db.Categories.AddRange(cats);
                await db.SaveChangesAsync(ct);
            }

            // Map: Slug -> CategoryId
            var catMap = await db.CategoryTranslations
                .Where(t => t.Culture == "de-DE")
                .Select(t => new { t.CategoryId, t.Slug })
                .ToDictionaryAsync(x => x.Slug, x => x.CategoryId, ct);

            // 3) Ensure at least one TaxCategory (19% DE standard) exists for pricing references
            var taxCat = await db.TaxCategories
                .OrderByDescending(t => t.EffectiveFromUtc)
                .FirstOrDefaultAsync(ct);

            if (taxCat == null)
            {
                taxCat = new TaxCategory
                {
                    Name = "Standard VAT 19%",
                    VatRate = 0.19m,
                    EffectiveFromUtc = DateTime.UtcNow.Date,
                    Notes = "Dev seed – default German VAT 19%."
                };
                db.TaxCategories.Add(taxCat);
                await db.SaveChangesAsync(ct);
            }

            // 4) Products (each with translation + a single simple variant)
            if (!await db.Products.AnyAsync(ct))
            {
                var products = CreateProductsDe(brandMap, catMap, taxCat.Id);
                db.Products.AddRange(products);
                await db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Catalog seeding done.");
        }

        #region Helpers (Factories)

        /// <summary>
        /// Creates >=20 brands with de-DE translations. Slug is culture-invariant at Brand level.
        /// </summary>
        private static List<Brand> CreateBrandsDe()
        {
            // Popular brands for computers/phones/accessories
            var brandInfos = new (string Slug, string Name)[]
            {
                ("apple", "Apple"),
                ("samsung", "Samsung"),
                ("lenovo", "Lenovo"),
                ("dell", "Dell"),
                ("hp", "HP"),
                ("asus", "ASUS"),
                ("acer", "Acer"),
                ("msi", "MSI"),
                ("huawei", "Huawei"),
                ("xiaomi", "Xiaomi"),
                ("google", "Google"),
                ("sony", "Sony"),
                ("lg", "LG"),
                ("oneplus", "OnePlus"),
                ("nokia", "Nokia"),
                ("razer", "Razer"),
                ("logitech", "Logitech"),
                ("corsair", "Corsair"),
                ("sandisk", "SanDisk"),
                ("seagate", "Seagate"),
                ("western-digital", "Western Digital"),
                ("kingston", "Kingston")
            };

            var list = new List<Brand>(brandInfos.Length);
            foreach (var b in brandInfos)
            {
                list.Add(new Brand
                {
                    Slug = b.Slug,
                    Translations = new()
                    {
                        new BrandTranslation
                        {
                            Culture = "de-DE",
                            Name = b.Name,
                            DescriptionHtml = $"<p>{b.Name} – führende Marke für Elektronik und Computerprodukte.</p>"
                        }
                    }
                });
            }
            return list;
        }

        /// <summary>
        /// Creates a hierarchical category tree (≥20) with de-DE translations.
        /// Parent is set via ParentId using in-memory references.
        /// </summary>
        private static List<Category> CreateCategoriesDe()
        {
            // Define root categories with children (slugs are per-translation)
            var data = new[]
            {
                new {
                    Name="Smartphones", Slug="smartphones", Children = new[]{
                        ("Android Phones","android-phones"),
                        ("iPhones","iphones"),
                        ("Refurbished Phones","refurbished-phones"),
                        ("Phone Accessories","phone-accessories"),
                        ("Power Banks","power-banks"),
                    }
                },
                new {
                    Name="Laptops", Slug="laptops", Children = new[]{
                        ("Ultrabooks","ultrabooks"),
                        ("Gaming Laptops","gaming-laptops"),
                        ("Business Laptops","business-laptops"),
                        ("2-in-1 Convertibles","convertibles"),
                        ("Laptop Accessories","laptop-accessories"),
                    }
                },
                new {
                    Name="Tablets", Slug="tablets", Children = new[]{
                        ("Android Tablets","android-tablets"),
                        ("iPad","ipad"),
                        ("Windows Tablets","windows-tablets"),
                        ("E-Reader","e-reader"),
                    }
                },
                new {
                    Name="PC & Komponenten", Slug="pc-komponenten", Children = new[]{
                        ("Desktops","desktops"),
                        ("Monitors","monitors"),
                        ("Keyboards","keyboards"),
                        ("Mice","mice"),
                        ("Storage","storage"),
                        ("Networking","networking")
                    }
                }
            };

            var result = new List<Category>();

            foreach (var root in data)
            {
                var rootCat = new Category
                {
                    SortOrder = result.Count * 10,
                    IsActive = true,
                    Translations = new()
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE",
                            Name = root.Name,
                            Slug = root.Slug,
                            Description = $"{root.Name} – Auswahl für alle Anforderungen.",
                            MetaTitle = root.Name,
                            MetaDescription = $"{root.Name} online kaufen – große Auswahl, schnelle Lieferung."
                        }
                    }
                };
                result.Add(rootCat);

                // Children
                var order = 0;
                foreach (var ch in root.Children)
                {
                    var child = new Category
                    {
                        ParentId = rootCat.Id, // will be set by EF as GUID, but ParentId links by value; Id is empty here; EF will handle after track.
                        SortOrder = order++,
                        IsActive = true,
                        Translations = new()
                        {
                            new CategoryTranslation
                            {
                                Culture = "de-DE",
                                Name = ch.Item1,
                                Slug = ch.Item2,
                                Description = $"{ch.Item1} – Top-Angebote.",
                                MetaTitle = ch.Item1,
                                MetaDescription = $"{ch.Item1} jetzt entdecken."
                            }
                        }
                    };
                    result.Add(child);
                }
            }

            // Add some extra leaf categories to exceed 20
            var extraLeaves = new (string Name, string Slug)[]
            {
                ("Chargers & Cables","chargers-cables"),
                ("Cases & Covers","cases-covers"),
                ("Headphones","headphones"),
                ("Smartwatches","smartwatches"),
                ("VR Headsets","vr-headsets")
            };
            foreach (var leaf in extraLeaves)
            {
                result.Add(new Category
                {
                    SortOrder = 999,
                    IsActive = true,
                    Translations = new()
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE",
                            Name = leaf.Name,
                            Slug = leaf.Slug,
                            Description = $"{leaf.Name} in großer Auswahl.",
                            MetaTitle = leaf.Name,
                            MetaDescription = $"{leaf.Name} – günstig online bestellen."
                        }
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Creates ≥20 products referencing existing brand/category, each with de-DE translation and one variant.
        /// </summary>
        private static List<Product> CreateProductsDe(
            IDictionary<string, Guid> brandBySlug,
            IDictionary<string, Guid> catBySlug,
            Guid taxCategoryId)
        {
            // Helper to map brand/category safely
            Guid B(string slug) => brandBySlug.TryGetValue(slug, out var id) ? id : (Guid?)null ?? Guid.Empty;
            Guid C(string slug) => catBySlug.TryGetValue(slug, out var id) ? id : (Guid?)null ?? Guid.Empty;

            // Seed set: phones, laptops, components, accessories
            var items = new[]
            {
                // Smartphones
                new { Name="iPhone 15 Pro 128GB", Slug="iphone-15-pro-128", Brand="apple", Cat="iphones", Price=1199m, Compare=(decimal?)null, SKU="APL-IP15P-128-001"},
                new { Name="Samsung Galaxy S24 256GB", Slug="galaxy-s24-256", Brand="samsung", Cat="android-phones", Price=999m, Compare=1099m, SKU="SMS-S24-256-001"},
                new { Name="Google Pixel 9 128GB", Slug="pixel-9-128", Brand="google", Cat="android-phones", Price=799m, Compare=869m, SKU="GGL-PX9-128-001"},
                new { Name="Xiaomi 14 Pro 256GB", Slug="xiaomi-14-pro-256", Brand="xiaomi", Cat="android-phones", Price=799m, Compare=(decimal?)null, SKU="XMI-14P-256-001"},
                new { Name="OnePlus 12 256GB", Slug="oneplus-12-256", Brand="oneplus", Cat="android-phones", Price=869m, Compare=949m, SKU="1P-12-256-001"},

                // Laptops
                new { Name="MacBook Air 13 M3 8/256", Slug="macbook-air-13-m3", Brand="apple", Cat="ultrabooks", Price=1299m, Compare=1399m, SKU="APL-MBA13-M3-001"},
                new { Name="Dell XPS 13 16/512", Slug="dell-xps-13-2025", Brand="dell", Cat="ultrabooks", Price=1599m, Compare=(decimal?)null, SKU="DEL-XPS13-001"},
                new { Name="HP Envy 15", Slug="hp-envy-15", Brand="hp", Cat="business-laptops", Price=1199m, Compare=1299m, SKU="HP-ENVY15-001"},
                new { Name="Lenovo ThinkPad X1 Carbon", Slug="thinkpad-x1-carbon", Brand="lenovo", Cat="business-laptops", Price=1899m, Compare=1999m, SKU="LNV-X1C-001"},
                new { Name="ASUS ROG Zephyrus G14", Slug="rog-g14-2025", Brand="asus", Cat="gaming-laptops", Price=1799m, Compare=1899m, SKU="ASU-G14-001"},

                // Components & Peripherals
                new { Name="Logitech MX Master 3S", Slug="logitech-mx-master-3s", Brand="logitech", Cat="mice", Price=119m, Compare=139m, SKU="LOG-MX3S-001"},
                new { Name="Corsair K70 RGB Pro", Slug="corsair-k70-rgb-pro", Brand="corsair", Cat="keyboards", Price=179m, Compare=(decimal?)null, SKU="COR-K70-RGBP-001"},
                new { Name="Samsung 980 Pro 1TB NVMe", Slug="samsung-980-pro-1tb", Brand="samsung", Cat="storage", Price=129m, Compare=159m, SKU="SMS-980PRO-1TB-001"},
                new { Name="Seagate BarraCuda 4TB", Slug="seagate-barracuda-4tb", Brand="seagate", Cat="storage", Price=99m, Compare=(decimal?)null, SKU="SEA-BC-4TB-001"},
                new { Name="WD Black SN850X 2TB", Slug="wd-black-sn850x-2tb", Brand="western-digital", Cat="storage", Price=229m, Compare=259m, SKU="WD-SN850X-2TB-001"},

                // Monitors & Accessories
                new { Name="LG 27UL850 27\" 4K", Slug="lg-27ul850-4k", Brand="lg", Cat="monitors", Price=349m, Compare=399m, SKU="LG-27UL850-001"},
                new { Name="Sony WH-1000XM5", Slug="sony-wh-1000xm5", Brand="sony", Cat="headphones", Price=329m, Compare=379m, SKU="SNY-WH1000XM5-001"},
                new { Name="Anker PowerCore 20K", Slug="anker-powercore-20k", Brand="sandisk", Cat="power-banks", Price=59m, Compare=69m, SKU="ANK-PWRCR20K-001"}, // using SanDisk slot for accessory brand placeholder
                new { Name="Razer DeathAdder V3", Slug="razer-deathadder-v3", Brand="razer", Cat="mice", Price=89m, Compare=109m, SKU="RZR-DA-V3-001"},
                new { Name="Kingston Fury 32GB DDR5", Slug="kingston-fury-32gb-ddr5", Brand="kingston", Cat="pc-komponenten", Price=129m, Compare=149m, SKU="KNG-FURY-32G-001"}
            };

            var list = new List<Product>(items.Length);

            foreach (var p in items)
            {
                var brandId = brandBySlug.TryGetValue(p.Brand, out var bid) ? bid : (Guid?)null;
                var primaryCat = catBySlug.TryGetValue(p.Cat, out var cid) ? cid : (Guid?)null;

                // Variant prices as minor units
                long priceMinor = (long)Math.Round(p.Price * 100m, MidpointRounding.AwayFromZero);
                long? compareMinor = p.Compare.HasValue ? (long?)Math.Round(p.Compare.Value * 100m, MidpointRounding.AwayFromZero) : null;

                var product = new Product
                {
                    BrandId = brandId,
                    PrimaryCategoryId = primaryCat,
                    IsActive = true,
                    IsVisible = true,
                    Kind = Darwin.Domain.Enums.ProductKind.Simple,
                    Translations = new()
                    {
                        new ProductTranslation
                        {
                            Culture = "de-DE",
                            Name = p.Name,
                            Slug = p.Slug,
                            ShortDescription = "Sofort lieferbar – 2 Jahre Garantie.",
                            FullDescriptionHtml = $"<p>{p.Name} – hochwertige Verarbeitung und starke Performance.</p>",
                            MetaTitle = p.Name,
                            MetaDescription = $"{p.Name} jetzt online kaufen."
                        }
                    },
                    Media = new(), // optionally attach MediaAsset via ProductMedia after CMS media exists
                    Variants = new()
                    {
                        new ProductVariant
                        {
                            Sku = p.SKU,
                            Currency = "EUR",
                            TaxCategoryId = taxCategoryId,
                            BasePriceNetMinor = priceMinor,
                            CompareAtPriceNetMinor = compareMinor ?? 0,
                            StockOnHand = 50,
                            StockReserved = 0,
                            ReorderPoint = 5,
                            BackorderAllowed = false,
                            MinOrderQty = 1,
                            MaxOrderQty = 5,
                            StepOrderQty = 1,
                            PackageWeight = 1.2m,
                            PackageLength = 30m,
                            PackageWidth = 20m,
                            PackageHeight = 10m,
                            IsDigital = false
                        }
                    }
                };

                list.Add(product);
            }

            return list;
        }

        #endregion
    }
}
