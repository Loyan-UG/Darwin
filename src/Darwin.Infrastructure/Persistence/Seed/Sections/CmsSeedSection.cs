using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.CMS;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds CMS data:
    /// - Media assets (sample gallery)
    /// - Menus: Main, Footer, Account (with hierarchical items and de-DE translations)
    /// - Pages: at least 20 common pages for an electronics e-commerce site (with translations)
    /// 
    /// Idempotent: checks existence by simple conditions to avoid duplicates on re-run.
    /// </summary>
    public sealed class CmsSeedSection
    {
        private readonly ILogger<CmsSeedSection> _logger;

        public CmsSeedSection(ILogger<CmsSeedSection> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entry point invoked by <see cref="DataSeeder"/>.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding CMS (media/menus/pages) ...");

            await SeedMediaAsync(db, ct);
            await SeedMenusAsync(db, ct);
            await SeedPagesAsync(db, ct);

            _logger.LogInformation("CMS seeding done.");
        }

        #region Media

        /// <summary>
        /// Creates a small set of media assets as placeholders if none exist.
        /// </summary>
        private static async Task SeedMediaAsync(DarwinDbContext db, CancellationToken ct)
        {
            if (await db.MediaAssets.AnyAsync(ct)) return;

            var assets = new List<MediaAsset>
            {
                new()
                {
                    Url = "/media/sample/hero-electronics.jpg",
                    Alt = "Elektronik – Angebote",
                    Title = "Elektronik",
                    OriginalFileName = "hero-electronics.jpg",
                    SizeBytes = 256_000,
                    Width = 1920,
                    Height = 600,
                    Role = "Hero"
                },
                new()
                {
                    Url = "/media/sample/laptops-collection.jpg",
                    Alt = "Laptops Auswahl",
                    Title = "Laptops",
                    OriginalFileName = "laptops-collection.jpg",
                    SizeBytes = 320_000,
                    Width = 1920,
                    Height = 1080,
                    Role = "Collection"
                },
                // Add more images to reach a richer gallery
                new()
                {
                    Url = "/media/sample/smartphones.jpg",
                    Alt = "Smartphones Auswahl",
                    Title = "Smartphones",
                    OriginalFileName = "smartphones.jpg",
                    SizeBytes = 300_000,
                    Width = 1920,
                    Height = 1080,
                    Role = "Collection"
                },
                new()
                {
                    Url = "/media/sample/accessories.jpg",
                    Alt = "Zubehör",
                    Title = "Zubehör",
                    OriginalFileName = "accessories.jpg",
                    SizeBytes = 220_000,
                    Width = 1920,
                    Height = 1080,
                    Role = "Collection"
                }
            };

            db.MediaAssets.AddRange(assets);
            await db.SaveChangesAsync(ct);
        }

        #endregion

        #region Menus

        /// <summary>
        /// Seeds three menus (Main, Footer, Account) with common items and de-DE translations.
        /// </summary>
        private static async Task SeedMenusAsync(DarwinDbContext db, CancellationToken ct)
        {
            // MAIN
            if (!await db.Menus.AnyAsync(m => m.Name == "Main" && !m.IsDeleted, ct))
            {
                var main = new Menu
                {
                    Name = "Main",
                    Items = new()
                    {
                        // Top-level
                        CreateMenuItem("Startseite", "Home", "/", 0, null),
                        CreateMenuItem("Shop", "Shop", "/shop", 1, null, children: new[]{
                            CreateMenuItem("Smartphones", "Smartphones", "/c/smartphones", 0),
                            CreateMenuItem("Laptops", "Laptops", "/c/laptops", 1),
                            CreateMenuItem("Tablets", "Tablets", "/c/tablets", 2),
                            CreateMenuItem("PC & Komponenten", "PC & Komponenten", "/c/pc-komponenten", 3)
                        }),
                        CreateMenuItem("Angebote", "Deals", "/deals", 2, null),
                        CreateMenuItem("Kontakt", "Contact", "/kontakt", 3, null)
                    }
                };
                db.Menus.Add(main);
            }

            // FOOTER
            if (!await db.Menus.AnyAsync(m => m.Name == "Footer" && !m.IsDeleted, ct))
            {
                var footer = new Menu
                {
                    Name = "Footer",
                    Items = new()
                    {
                        CreateMenuItem("Über uns", "About", "/ueber-uns", 0, null),
                        CreateMenuItem("Impressum", "Imprint", "/impressum", 1, null),
                        CreateMenuItem("Datenschutz", "Privacy", "/datenschutz", 2, null),
                        CreateMenuItem("AGB", "Terms", "/agb", 3, null),
                        CreateMenuItem("Versand", "Shipping", "/versand", 4, null),
                        CreateMenuItem("Rückgabe", "Returns", "/rueckgabe", 5, null),
                        CreateMenuItem("FAQ", "FAQ", "/faq", 6, null)
                    }
                };
                db.Menus.Add(footer);
            }

            // ACCOUNT
            if (!await db.Menus.AnyAsync(m => m.Name == "Account" && !m.IsDeleted, ct))
            {
                var account = new Menu
                {
                    Name = "Account",
                    Items = new()
                    {
                        CreateMenuItem("Mein Konto", "Account", "/konto", 0, null),
                        CreateMenuItem("Bestellungen", "Orders", "/konto/bestellungen", 1, null),
                        CreateMenuItem("Adressen", "Addresses", "/konto/adressen", 2, null),
                        CreateMenuItem("Abmelden", "Sign out", "/logout", 3, null)
                    }
                };
                db.Menus.Add(account);
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Helper to create a MenuItem with a de-DE translation and optional children.
        /// </summary>
        private static MenuItem CreateMenuItem(
            string titleDe,
            string titleEnFallback,
            string url,
            int sortOrder,
            Guid? parentId = null,
            IEnumerable<MenuItem>? children = null)
        {
            var mi = new MenuItem
            {
                ParentId = parentId,
                Url = url,
                SortOrder = sortOrder,
                Translations = new()
                {
                    new MenuItemTranslation
                    {
                        Culture = "de-DE",
                        Title = titleDe
                    }
                }
            };

            if (children != null)
                mi.Translations[0].Title = titleDe; // keep de-DE; children are attached below

            if (children != null)
            {
                var order = 0;
                mi = mi with { }; // ensure object init complete before adding children
                foreach (var child in children)
                {
                    child.ParentId = mi.Id; // relationship by Guid; EF will assign Id at SaveChanges, but FK holds the value
                    child.SortOrder = order++;
                }
            }

            return mi;
        }

        #endregion

        #region Pages

        /// <summary>
        /// Seeds at least 20 CMS pages with de-DE translations.
        /// </summary>
        private static async Task SeedPagesAsync(DarwinDbContext db, CancellationToken ct)
        {
            if (await db.Pages.AnyAsync(ct)) return;

            // Common e-commerce pages for electronics
            var pages = new (string Slug, string Title, string Html)[]
            {
                ("startseite","Startseite","<h1>Willkommen</h1><p>Elektronik & Computer – Top-Angebote.</p>"),
                ("ueber-uns","Über uns","<h1>Über uns</h1><p>Kompetenz für Technik seit 2010.</p>"),
                ("kontakt","Kontakt","<h1>Kontakt</h1><p>Wir sind für Sie da.</p>"),
                ("impressum","Impressum","<h1>Impressum</h1><p>Angaben gemäß §5 TMG.</p>"),
                ("datenschutz","Datenschutz","<h1>Datenschutz</h1><p>Informationen gemäß DSGVO.</p>"),
                ("agb","AGB","<h1>Allgemeine Geschäftsbedingungen</h1><p>Bitte lesen Sie unsere Bedingungen.</p>"),
                ("versand","Versand","<h1>Versand</h1><p>Lieferzeiten & -kosten.</p>"),
                ("rueckgabe","Rückgabe","<h1>Rückgabe</h1><p>30 Tage Rückgaberecht.</p>"),
                ("faq","FAQ","<h1>Häufige Fragen</h1><p>Antworten auf Ihre Fragen.</p>"),
                ("zahlung","Zahlung","<h1>Zahlung</h1><p>Alle gängigen Zahlungsmethoden.</p>"),
                ("reparatur-service","Reparatur-Service","<h1>Reparatur</h1><p>Fachwerkstatt für Geräte.</p>"),
                ("garantie","Garantie","<h1>Garantie</h1><p>2 Jahre Herstellergarantie.</p>"),
                ("filialen","Filialen","<h1>Filialen</h1><p>Standorte & Öffnungszeiten.</p>"),
                ("jobs","Jobs","<h1>Jobs</h1><p>Werde Teil unseres Teams.</p>"),
                ("news","News","<h1>News</h1><p>Neuigkeiten & Angebote.</p>"),
                ("marken","Marken","<h1>Marken</h1><p>Beliebte Hersteller im Überblick.</p>"),
                ("kundenkonto","Kundenkonto","<h1>Mein Konto</h1><p>Einstellungen & Bestellungen.</p>"),
                ("widerruf","Widerruf","<h1>Widerruf</h1><p>Formular & Informationen.</p>"),
                ("datensicherheit","Datensicherheit","<h1>Datensicherheit</h1><p>Schutz Ihrer Daten.</p>"),
                ("lieferstatus","Lieferstatus","<h1>Lieferstatus</h1><p>Sendungsverfolgung.</p>"),
                ("geschenkkarten","Geschenkkarten","<h1>Geschenkkarten</h1><p>Das ideale Geschenk.</p>")
            };

            var list = new List<Page>(pages.Length);
            foreach (var p in pages)
            {
                list.Add(new Page
                {
                    Translations = new()
                    {
                        new PageTranslation
                        {
                            Culture = "de-DE",
                            Title = p.Title,
                            Slug = p.Slug,
                            ContentHtml = p.Html,
                            MetaTitle = p.Title,
                            MetaDescription = $"{p.Title} – Informationen & Details."
                        }
                    }
                });
            }

            db.Pages.AddRange(list);
            await db.SaveChangesAsync(ct);
        }

        #endregion
    }
}
