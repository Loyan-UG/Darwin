using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed
{
    /// <summary>
    ///     Idempotent data seeder that populates baseline records required for the Darwin platform to boot:
    ///     <c>SiteSetting</c>, <c>TaxCategory</c> (DE standard/reduced), base <c>Brand</c>, and sample <c>Category</c> hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The seeder is designed to be safe to run multiple times — it checks for existence before inserting.
    ///         It should be invoked once during application startup after migrations are applied.
    ///     </para>
    ///     <para>
    ///         Best Practices:
    ///         <list type="bullet">
    ///             <item>Use <c>WellKnownIds.SystemUserId</c> for <c>CreatedBy/ModifiedBy</c> during seeding.</item>
    ///             <item>Keep seeded data minimal and environment-agnostic; environment-specific data belongs to dedicated fixtures.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Logging:
    ///         Uses <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/> to report progress and anomalies.
    ///     </para>
    /// </remarks>
    public sealed class DataSeeder
    {
        private readonly DarwinDbContext _db;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(DarwinDbContext db, ILogger<DataSeeder> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Runs the seeding sequence in logical order:
        /// 1) SiteSetting (single row)
        /// 2) TaxCategories (DE: 19% Standard, 7% Reduced)
        /// 3) Brands (Microsoft, Samsung, Logitech, Intel, AMD, Nvidia...)
        /// 4) Categories (root + multiple children, with translations)
        /// 5) CMS Pages (Home, Impressum, Privacy) with translations
        /// 6) Sample Products with translations, variants, inventory, prices, relations
        /// </summary>
        public async Task SeedAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting data seeding…");

            await SeedSiteSettingAsync(ct);
            await SeedTaxCategoriesAsync(ct);
            var brands = await SeedBrandsAsync(ct);
            var cats = await SeedCategoriesAsync(ct);
            await SeedPagesAsync(ct);
            await SeedProductsAsync(brands, cats, ct);

            _logger.LogInformation("Data seeding completed.");
        }

        #region SiteSetting

        /// <summary>
        /// Seeds the single SiteSetting row with comprehensive defaults suitable for a DE shop.
        /// Idempotent: inserts only when no SiteSetting exists.
        /// </summary>
        private async Task SeedSiteSettingAsync(CancellationToken ct)
        {
            if (await _db.Set<SiteSetting>().AnyAsync(ct)) return;

            var s = new SiteSetting
            {
                // General
                Title = "Darwin",
                LogoUrl = "/media/logo.svg",
                ContactEmail = "info@darwin.local",
                HomeSlug = "home",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                DefaultCountry = "DE",
                DefaultCurrency = "EUR",
                TimeZone = "Europe/Berlin",
                DateFormat = "dd.MM.yyyy",
                TimeFormat = "HH:mm",

                // Display & Units
                MeasurementSystem = "Metric",
                DisplayWeightUnit = "kg",
                DisplayLengthUnit = "cm",
                // Optional JSON knobs for future UI (precision/rounding/symbol placement)
                MeasurementSettingsJson = /* example */ "{\"price\":{\"minorUnitScale\":2},\"weight\":{\"decimals\":3},\"length\":{\"decimals\":1}}",
                // Optional overrides if you need non-culture defaults for separators, etc.
                NumberFormattingOverridesJson = null,

                // SEO
                SeoTitleTemplate = "{{Title}} – Darwin",
                SeoMetaDescriptionTemplate = "{{MetaDescription}}",
                // NOTE: property name ends with 'Json' in your entity
                OpenGraphDefaultsJson = "{\"site_name\":\"Darwin\",\"type\":\"website\",\"image\":\"/media/og-default.png\"}",
                EnableCanonical = true,
                HreflangEnabled = true,

                // Analytics
                GoogleAnalyticsId = "",
                GoogleTagManagerId = "",
                GoogleSearchConsoleVerification = "",

                // Feature Flags (matches your entity name FeatureFlagsJson)
                FeatureFlagsJson = "{\"Payments\":{\"PayPal\":{\"Enabled\":true},\"SEPA\":{\"Enabled\":true}},\"Shipping\":{\"DHL\":{\"Enabled\":true}},\"Webhooks\":{\"Enabled\":false},\"WebApi\":{\"Enabled\":false}}",

                // WhatsApp (left disabled by default; fill when integrating)
                WhatsAppEnabled = false,
                WhatsAppBusinessPhoneId = null,
                WhatsAppAccessToken = null,
                WhatsAppFromPhoneE164 = null,
                WhatsAppAdminRecipientsCsv = null,

                //WebAuthn
                WebAuthnRelyingPartyId = "localhost",
                WebAuthnRelyingPartyName = "Darwin",
                WebAuthnAllowedOriginsCsv = "https://localhost:5001",
                WebAuthnRequireUserVerification = false,

                // Auditing
                CreatedByUserId = Darwin.Shared.Constants.WellKnownIds.SystemUserId,
                ModifiedByUserId = Darwin.Shared.Constants.WellKnownIds.SystemUserId

                // Email
                SmtpEnabled = false,
                SmtpHost = null,
                SmtpPort = null,
                SmtpEnableSsl = true,
                SmtpUsername = null,
                SmtpPassword = null,
                SmtpFromAddress = null,
                SmtpFromDisplayName = null,

                SmsEnabled = false,
                SmsProvider = null,
                SmsFromPhoneE164 = null,
                SmsApiKey = null,
                SmsApiSecret = null,
                SmsExtraSettingsJson = null,

                AdminAlertEmailsCsv = null,
                AdminAlertSmsRecipientsCsv = null

            };

            _db.Add(s);
            await _db.SaveChangesAsync(ct);
        }

        #endregion

        #region TaxCategories

        private async Task SeedTaxCategoriesAsync(CancellationToken ct)
        {
            if (await _db.Set<TaxCategory>().AnyAsync(ct)) return;

            var now = DateTime.UtcNow;
            _db.AddRange(
                new TaxCategory
                {
                    Name = "Standard",
                    VatRate = 0.19m,
                    EffectiveFromUtc = now,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId
                },
                new TaxCategory
                {
                    Name = "Reduced",
                    VatRate = 0.07m,
                    EffectiveFromUtc = now,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId
                }
            );

            await _db.SaveChangesAsync(ct);
        }

        #endregion

        #region Brands

        /// <summary>
        /// Seeds initial Brands with multilingual translations.
        /// Returns a name->Id map based on the default culture ("de-DE") to help seed relations.
        /// </summary>
        private async Task<Dictionary<string, Guid>> SeedBrandsAsync(CancellationToken ct)
        {
            var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            if (!await _db.Set<Brand>().AnyAsync(ct))
            {
                var seed = new[]
                {
                    new { Slug = "microsoft", Names = new Dictionary<string,string> { ["de-DE"]="Microsoft", ["en-US"]="Microsoft" } },
                    new { Slug = "samsung",   Names = new Dictionary<string,string> { ["de-DE"]="Samsung",   ["en-US"]="Samsung"   } },
                    new { Slug = "logitech",  Names = new Dictionary<string,string> { ["de-DE"]="Logitech",  ["en-US"]="Logitech"  } },
                    new { Slug = "intel",     Names = new Dictionary<string,string> { ["de-DE"]="Intel",     ["en-US"]="Intel"     } },
                    new { Slug = "amd",       Names = new Dictionary<string,string> { ["de-DE"]="AMD",       ["en-US"]="AMD"       } },
                    new { Slug = "nvidia",    Names = new Dictionary<string,string> { ["de-DE"]="NVIDIA",    ["en-US"]="NVIDIA"    } },
                };

                foreach (var b in seed)
                {
                    var brand = new Brand
                    {
                        Slug = b.Slug,
                        CreatedByUserId = WellKnownIds.SystemUserId,
                        ModifiedByUserId = WellKnownIds.SystemUserId
                    };

                    foreach (var kv in b.Names)
                    {
                        brand.Translations.Add(new BrandTranslation
                        {
                            Culture = kv.Key,
                            Name = kv.Value,
                            DescriptionHtml = null,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        });
                    }

                    _db.Add(brand);
                }

                await _db.SaveChangesAsync(ct);
            }

            // Resolve map using default culture name (de-DE) for downstream seeding convenience
            var existing = await _db.Set<Brand>()
                .AsNoTracking()
                .Select(b => new
                {
                    b.Id,
                    NameDe = b.Translations.Where(t => t.Culture == "de-DE").Select(t => t.Name).FirstOrDefault()
                })
                .ToListAsync(ct);

            foreach (var row in existing)
            {
                if (!string.IsNullOrWhiteSpace(row.NameDe))
                    map[row.NameDe] = row.Id;
            }

            return map;
        }

        #endregion

        #region Categories

        private async Task<Dictionary<string, Guid>> SeedCategoriesAsync(CancellationToken ct)
        {
            // Returns a map CategoryKey -> CategoryId, where keys are English names for convenience.
            var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            if (!await _db.Set<Category>().AnyAsync(ct))
            {
                // Pre-generate IDs so ParentId is valid before SaveChanges.
                var idRoot = Guid.NewGuid();
                var idLaptops = Guid.NewGuid();
                var idMonitors = Guid.NewGuid();
                var idKeyboards = Guid.NewGuid();
                var idMice = Guid.NewGuid();
                var idAccessories = Guid.NewGuid();
                var idSoftware = Guid.NewGuid();

                var root = new Category
                {
                    Id = idRoot,
                    IsActive = true,
                    SortOrder = 1,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Computer & Elektronik", Slug = "computer-elektronik",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Computers & Electronics", Slug = "computers-electronics",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var laptops = new Category
                {
                    Id = idLaptops,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 2,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Laptops", Slug = "laptops",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Laptops", Slug = "laptops",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var monitors = new Category
                {
                    Id = idMonitors,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 3,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Monitore", Slug = "monitore",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Monitors", Slug = "monitors",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var keyboards = new Category
                {
                    Id = idKeyboards,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 4,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Tastaturen", Slug = "tastaturen",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Keyboards", Slug = "keyboards",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var mice = new Category
                {
                    Id = idMice,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 5,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Mäuse", Slug = "maeuse",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Mice", Slug = "mice",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var accessories = new Category
                {
                    Id = idAccessories,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 6,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Zubehör", Slug = "zubehoer",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Accessories", Slug = "accessories",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                var software = new Category
                {
                    Id = idSoftware,
                    ParentId = idRoot,
                    IsActive = true,
                    SortOrder = 7,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<CategoryTranslation>
                    {
                        new CategoryTranslation
                        {
                            Culture = "de-DE", Name = "Software", Slug = "software",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new CategoryTranslation
                        {
                            Culture = "en-US", Name = "Software", Slug = "software",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                };

                _db.AddRange(root, laptops, monitors, keyboards, mice, accessories, software);
                await _db.SaveChangesAsync(ct);
            }

            var existing = await _db.Set<Category>()
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    Names = c.Translations.Select(t => t.Name).ToList()
                })
                .ToListAsync(ct);

            // Map by English names for convenience
            foreach (var c in existing)
            {
                foreach (var n in c.Names)
                {
                    map[n] = c.Id; // includes both de-DE and en-US names
                }
            }

            return map;
        }

        #endregion

        #region CMS Pages

        private async Task SeedPagesAsync(CancellationToken ct)
        {
            if (await _db.Set<Page>().AnyAsync(ct)) return;

            var pages = new List<Page>
            {
                new Page
                {
                    Status = PageStatus.Published,
                    PublishStartUtc = DateTime.UtcNow.AddDays(-1),
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<PageTranslation>
                    {
                        new PageTranslation
                        {
                            Culture = "de-DE", Title = "Startseite", Slug = "home",
                            MetaTitle = "Darwin – Startseite", MetaDescription = "Computer & Elektronik.",
                            ContentHtml = "<h2>Willkommen bei Darwin</h2><p>Ihr Shop für Computer & Elektronik.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new PageTranslation
                        {
                            Culture = "en-US", Title = "Home", Slug = "home",
                            MetaTitle = "Darwin – Home", MetaDescription = "Computers & Electronics.",
                            ContentHtml = "<h2>Welcome to Darwin</h2><p>Your shop for computers & electronics.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                },
                new Page
                {
                    Status = PageStatus.Published,
                    PublishStartUtc = DateTime.UtcNow.AddDays(-1),
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<PageTranslation>
                    {
                        new PageTranslation
                        {
                            Culture = "de-DE", Title = "Impressum", Slug = "impressum",
                            MetaTitle = "Impressum", MetaDescription = "Impressum Darwin.",
                            ContentHtml = "<h2>Impressum</h2><p>Rechtliche Angaben…</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new PageTranslation
                        {
                            Culture = "en-US", Title = "Imprint", Slug = "imprint",
                            MetaTitle = "Imprint", MetaDescription = "Darwin imprint.",
                            ContentHtml = "<h2>Imprint</h2><p>Legal details…</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                },
                new Page
                {
                    Status = PageStatus.Published,
                    PublishStartUtc = DateTime.UtcNow.AddDays(-1),
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<PageTranslation>
                    {
                        new PageTranslation
                        {
                            Culture = "de-DE", Title = "Datenschutz", Slug = "datenschutz",
                            MetaTitle = "Datenschutz", MetaDescription = "Schutz Ihrer Daten.",
                            ContentHtml = "<h2>Datenschutz</h2><p>Wie wir Ihre Daten schützen…</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new PageTranslation
                        {
                            Culture = "en-US", Title = "Privacy", Slug = "privacy",
                            MetaTitle = "Privacy", MetaDescription = "How we protect your data.",
                            ContentHtml = "<h2>Privacy</h2><p>How we protect your data…</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                }
            };

            _db.AddRange(pages);
            await _db.SaveChangesAsync(ct);
        }

        #endregion

        #region Products

        private async Task SeedProductsAsync(
            Dictionary<string, Guid> brandMap,
            Dictionary<string, Guid> categoryMap,
            CancellationToken ct)
        {
            if (await _db.Set<Product>().AnyAsync(ct)) return;

            // Resolve common relations
            var taxStandardId = await _db.Set<TaxCategory>()
                .Where(t => t.Name == "Standard")
                .Select(t => t.Id)
                .FirstAsync(ct);

            // Category helpers (fall back to any existing if map key missing)
            Guid Cat(string key) => categoryMap.TryGetValue(key, out var id)
                ? id
                : categoryMap.Values.First();

            // Brand helpers
            Guid Brand(string key) => brandMap.TryGetValue(key, out var id)
                ? id
                : brandMap.Values.First();

            var products = new List<Product>
            {
                // Microsoft Surface Laptop
                new Product
                {
                    Kind = ProductKind.Simple,
                    BrandId = Brand("Microsoft"),
                    PrimaryCategoryId = Cat("Laptops"),
                    IsActive = true,
                    IsVisible = true,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<ProductTranslation>
                    {
                        new ProductTranslation
                        {
                            Culture = "de-DE",
                            Name = "Microsoft Surface Laptop 5",
                            Slug = "microsoft-surface-laptop-5",
                            MetaTitle = "Surface Laptop 5 – Microsoft",
                            MetaDescription = "Stylischer Laptop mit Intel Core.",
                            FullDescriptionHtml = "<p>Leichter, schneller Laptop mit 13,5\" Display.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new ProductTranslation
                        {
                            Culture = "en-US",
                            Name = "Microsoft Surface Laptop 5",
                            Slug = "microsoft-surface-laptop-5",
                            MetaTitle = "Surface Laptop 5 – Microsoft",
                            MetaDescription = "Sleek laptop with Intel Core.",
                            FullDescriptionHtml = "<p>Lightweight, fast laptop with 13.5\" display.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            Sku = "MS-SUR-L5-13",
                            Gtin = "0889842970001",
                            Currency = "EUR",
                            TaxCategoryId = taxStandardId,
                            BasePriceNetMinor = 119999, // €1,199.99 NET
                            CompareAtPriceNetMinor = 129999,
                            StockOnHand = 15,
                            ReorderPoint = 3,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new ProductVariant
                        {
                            Sku = "MS-SUR-L5-15",
                            Gtin = "0889842970002",
                            Currency = "EUR",
                            TaxCategoryId = taxStandardId,
                            BasePriceNetMinor = 139999,
                            CompareAtPriceNetMinor = 149999,
                            StockOnHand = 8,
                            ReorderPoint = 2,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                },

                // Samsung 27" Monitor
                new Product
                {
                    Kind = ProductKind.Simple,
                    BrandId = Brand("Samsung"),
                    PrimaryCategoryId = Cat("Monitors"),
                    IsActive = true,
                    IsVisible = true,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<ProductTranslation>
                    {
                        new ProductTranslation
                        {
                            Culture = "de-DE",
                            Name = "Samsung 27\" Monitor 144Hz",
                            Slug = "samsung-27-monitor-144hz",
                            MetaTitle = "Samsung 27 Zoll 144Hz",
                            MetaDescription = "Flüssiges Gaming-Erlebnis.",
                            FullDescriptionHtml = "<p>27\" 144Hz, schnelles IPS-Panel.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new ProductTranslation
                        {
                            Culture = "en-US",
                            Name = "Samsung 27\" Monitor 144Hz",
                            Slug = "samsung-27-monitor-144hz",
                            MetaTitle = "Samsung 27 inch 144Hz",
                            MetaDescription = "Smooth gaming experience.",
                            FullDescriptionHtml = "<p>27\" 144Hz, fast IPS panel.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            Sku = "SAM-MON-27-144",
                            Gtin = "0880123456789",
                            Currency = "EUR",
                            TaxCategoryId = taxStandardId,
                            BasePriceNetMinor = 22999, // €229.99 NET
                            CompareAtPriceNetMinor = 24999,
                            StockOnHand = 25,
                            ReorderPoint = 5,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                },

                // Logitech MX Keys Keyboard
                new Product
                {
                    Kind = ProductKind.Simple,
                    BrandId = Brand("Logitech"),
                    PrimaryCategoryId = Cat("Keyboards"),
                    IsActive = true,
                    IsVisible = true,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<ProductTranslation>
                    {
                        new ProductTranslation
                        {
                            Culture = "de-DE",
                            Name = "Logitech MX Keys",
                            Slug = "logitech-mx-keys",
                            MetaTitle = "Logitech MX Keys",
                            MetaDescription = "Beliebte Tastatur für Produktivität.",
                            FullDescriptionHtml = "<p>Komfortables Tippen, beleuchtete Tasten.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new ProductTranslation
                        {
                            Culture = "en-US",
                            Name = "Logitech MX Keys",
                            Slug = "logitech-mx-keys",
                            MetaTitle = "Logitech MX Keys",
                            MetaDescription = "Popular productivity keyboard.",
                            FullDescriptionHtml = "<p>Comfortable typing, backlit keys.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            Sku = "LOG-MX-KEYS",
                            Gtin = "097855145612",
                            Currency = "EUR",
                            TaxCategoryId = taxStandardId,
                            BasePriceNetMinor = 7999, // €79.99 NET
                            CompareAtPriceNetMinor = 8999,
                            StockOnHand = 40,
                            ReorderPoint = 10,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                },

                // Logitech MX Master Mouse
                new Product
                {
                    Kind = ProductKind.Simple,
                    BrandId = Brand("Logitech"),
                    PrimaryCategoryId = Cat("Mice"),
                    IsActive = true,
                    IsVisible = true,
                    CreatedByUserId = WellKnownIds.SystemUserId,
                    ModifiedByUserId = WellKnownIds.SystemUserId,
                    Translations = new List<ProductTranslation>
                    {
                        new ProductTranslation
                        {
                            Culture = "de-DE",
                            Name = "Logitech MX Master 3S",
                            Slug = "logitech-mx-master-3s",
                            MetaTitle = "Logitech MX Master 3S",
                            MetaDescription = "Präzise Maus für Profis.",
                            FullDescriptionHtml = "<p>Ergonomisch, präzise, leise Klicks.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        },
                        new ProductTranslation
                        {
                            Culture = "en-US",
                            Name = "Logitech MX Master 3S",
                            Slug = "logitech-mx-master-3s",
                            MetaTitle = "Logitech MX Master 3S",
                            MetaDescription = "Precise mouse for professionals.",
                            FullDescriptionHtml = "<p>Ergonomic, precise, silent clicks.</p>",
                            CreatedByUserId = WellKnownIds.SystemUserId, ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            Sku = "LOG-MX-M3S",
                            Gtin = "097855166777",
                            Currency = "EUR",
                            TaxCategoryId = taxStandardId,
                            BasePriceNetMinor = 8999, // €89.99 NET
                            CompareAtPriceNetMinor = 9999,
                            StockOnHand = 50,
                            ReorderPoint = 12,
                            CreatedByUserId = WellKnownIds.SystemUserId,
                            ModifiedByUserId = WellKnownIds.SystemUserId
                        }
                    }
                }
            };

            _db.AddRange(products);
            await _db.SaveChangesAsync(ct);
        }

        #endregion
    }
}
