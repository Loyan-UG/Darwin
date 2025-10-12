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
            var main = await db.Menus.FirstOrDefaultAsync(m => m.Name == "Main" && !m.IsDeleted, ct);
            if (main == null)
            {
                main = new Menu { Name = "Main" };

                // Pre-allocate IDs so ParentId can reference before SaveChanges
                var homeId = Guid.NewGuid();
                var shopId = Guid.NewGuid();
                var aboutId = Guid.NewGuid();

                var items = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Id = homeId,
                        Url = "/de/home",
                        SortOrder = 0,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Startseite" } }
                    },
                    new MenuItem
                    {
                        Id = shopId,
                        Url = "/de/c",
                        SortOrder = 1,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Shop" } }
                    },
                    new MenuItem
                    {
                        Id = aboutId,
                        Url = "/de/ueber-uns",
                        SortOrder = 2,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Über uns" } }
                    },

                    // Children under "Shop"
                    new MenuItem
                    {
                        Url = "/de/c/iphones",
                        ParentId = shopId,
                        SortOrder = 0,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "iPhones" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/c/android-phones",
                        ParentId = shopId,
                        SortOrder = 1,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Android" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/c/ultrabooks",
                        ParentId = shopId,
                        SortOrder = 2,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Ultrabooks" } }
                    }
                };

                main.Items = items;
                db.Menus.Add(main);
                await db.SaveChangesAsync(ct);
            }

            // FOOTER
            var footer = await db.Menus.FirstOrDefaultAsync(m => m.Name == "Footer" && !m.IsDeleted, ct);
            if (footer == null)
            {
                footer = new Menu { Name = "Footer" };
                footer.Items = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Url = "/de/impressum",
                        SortOrder = 0,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Impressum" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/datenschutz",
                        SortOrder = 1,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "Datenschutz" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/agb",
                        SortOrder = 2,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = "de-DE", Label = "AGB" } }
                    }
                };

                db.Menus.Add(footer);
                await db.SaveChangesAsync(ct);
            }
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
