using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds CMS data:
    /// - Media assets (storefront gallery)
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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        /// Creates and upgrades the core storefront media assets used by seeded product galleries.
        /// </summary>
        private static async Task SeedMediaAsync(DarwinDbContext db, CancellationToken ct)
        {
            var mediaUrlMigrations = new Dictionary<string, string>
            {
                ["/media/sample/hero-electronics.jpg"] = "/media/storefront/hero-electronics.jpg",
                ["/media/sample/laptops-collection.jpg"] = "/media/storefront/laptops-collection.jpg",
                ["/media/sample/smartphones.jpg"] = "/media/storefront/smartphones.jpg",
                ["/media/sample/accessories.jpg"] = "/media/storefront/accessories.jpg"
            };

            var existingAssets = await db.MediaAssets
                .Where(x => mediaUrlMigrations.Keys.Contains(x.Url) || mediaUrlMigrations.Values.Contains(x.Url))
                .ToListAsync(ct);

            foreach (var asset in existingAssets.Where(x => mediaUrlMigrations.ContainsKey(x.Url)))
            {
                var targetUrl = mediaUrlMigrations[asset.Url];
                if (existingAssets.All(x => x.Url != targetUrl))
                {
                    asset.Url = targetUrl;
                }
            }

            foreach (var asset in existingAssets)
            {
                asset.Alt = NormalizeCmsSeedText(asset.Alt);
                asset.Title = string.IsNullOrWhiteSpace(asset.Title)
                    ? asset.Title
                    : NormalizeCmsSeedText(asset.Title);
            }

            if (existingAssets.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            var assets = new MediaAsset[]
            {
                new()
                {
                    Url = "/media/storefront/hero-electronics.jpg",
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
                    Url = "/media/storefront/laptops-collection.jpg",
                    Alt = "Laptops Auswahl",
                    Title = "Laptops",
                    OriginalFileName = "laptops-collection.jpg",
                    SizeBytes = 320_000,
                    Width = 1920,
                    Height = 1080,
                    Role = "Collection"
                },
                // Add more images to reach a richer storefront gallery
                new()
                {
                    Url = "/media/storefront/smartphones.jpg",
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
                    Url = "/media/storefront/accessories.jpg",
                    Alt = "Zubehör",
                    Title = "Zubehör",
                    OriginalFileName = "accessories.jpg",
                    SizeBytes = 220_000,
                    Width = 1920,
                    Height = 1080,
                    Role = "Collection"
                }
            };

            var assetUrls = assets.Select(x => x.Url).ToArray();
            var assetsByUrl = await db.MediaAssets
                .Where(x => assetUrls.Contains(x.Url))
                .ToDictionaryAsync(x => x.Url, x => x, ct);

            foreach (var desired in assets)
            {
                if (!assetsByUrl.TryGetValue(desired.Url, out var asset))
                {
                    asset = new MediaAsset { Url = desired.Url };
                    db.MediaAssets.Add(asset);
                }

                asset.Alt = NormalizeCmsSeedText(desired.Alt);
                asset.Title = string.IsNullOrWhiteSpace(desired.Title)
                    ? desired.Title
                    : NormalizeCmsSeedText(desired.Title);
                asset.OriginalFileName = desired.OriginalFileName;
                asset.SizeBytes = desired.SizeBytes;
                asset.Width = desired.Width;
                asset.Height = desired.Height;
                asset.Role = desired.Role;
            }

            await db.SaveChangesAsync(ct);
        }

        #endregion

        #region Menus

        /// <summary>
        /// Seeds the public storefront menus plus legacy CMS menus kept for compatibility.
        /// </summary>
        private static async Task SeedMenusAsync(DarwinDbContext db, CancellationToken ct)
        {
            await EnsurePublicMainNavigationAsync(db, ct);
            await EnsurePublicFooterNavigationAsync(db, ct);

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
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "Startseite" } }
                    },
                    new MenuItem
                    {
                        Id = shopId,
                        Url = "/de/c",
                        SortOrder = 1,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "Shop" } }
                    },
                    new MenuItem
                    {
                        Id = aboutId,
                        Url = "/de/ueber-uns",
                        SortOrder = 2,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "Über uns" } }
                    },

                    // Children under "Shop"
                    new MenuItem
                    {
                        Url = "/de/c/iphones",
                        ParentId = shopId,
                        SortOrder = 0,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "iPhones" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/c/android-phones",
                        ParentId = shopId,
                        SortOrder = 1,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "Android" } }
                    },
                    new MenuItem
                    {
                        Url = "/de/c/ultrabooks",
                        ParentId = shopId,
                        SortOrder = 2,
                        IsActive = true,
                        Translations = { new MenuItemTranslation { Culture = DomainDefaults.DefaultCulture, Label = "Ultrabooks" } }
                    }
                };

                main.Items = items;
                db.Menus.Add(main);
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task EnsurePublicMainNavigationAsync(DarwinDbContext db, CancellationToken ct)
        {
            var menu = await db.Menus
                .Include(x => x.Items)
                .ThenInclude(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Name == "main-navigation" && !x.IsDeleted, ct);

            var desiredItems = new[]
            {
                new { Url = "/", DeUrl = "/", Label = "Home", DeLabel = "Start", SortOrder = 0 },
                new { Url = "/catalog", DeUrl = "/catalog", Label = "Shop", DeLabel = "Shop", SortOrder = 1 },
                new { Url = "/loyalty", DeUrl = "/loyalty", Label = "Loyalty", DeLabel = "Treue", SortOrder = 2 },
                new { Url = "/help", DeUrl = "/help", Label = "Help", DeLabel = "Hilfe", SortOrder = 3 },
                new { Url = "/page/contact", DeUrl = "/page/kontakt", Label = "Contact", DeLabel = "Kontakt", SortOrder = 4 }
            };

            if (menu == null)
            {
                menu = new Menu
                {
                    Name = "main-navigation",
                    Culture = "en-US"
                };
                db.Menus.Add(menu);
            }

            menu.Culture = "en-US";
            await ResetMenuItemsAsync(db, menu, ct);
            foreach (var item in desiredItems)
            {
                menu.Items.Add(new MenuItem
                {
                    Url = item.Url,
                    Title = item.Label,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    Translations =
                    {
                        new MenuItemTranslation
                        {
                            Culture = "en-US",
                            Label = item.Label,
                            Url = item.Url
                        },
                        new MenuItemTranslation
                        {
                            Culture = DomainDefaults.DefaultCulture,
                            Label = item.DeLabel,
                            Url = item.DeUrl
                        }
                    }
                });
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsurePublicFooterNavigationAsync(DarwinDbContext db, CancellationToken ct)
        {
            var menu = await db.Menus
                .Include(x => x.Items)
                .ThenInclude(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Name == "Footer" && !x.IsDeleted, ct);

            var desiredItems = new[]
            {
                new { Url = "/catalog", DeUrl = "/catalog", Label = "Catalog", DeLabel = "Katalog", SortOrder = 0 },
                new { Url = "/loyalty", DeUrl = "/loyalty", Label = "Loyalty", DeLabel = "Treue", SortOrder = 1 },
                new { Url = "/cart", DeUrl = "/cart", Label = "Cart", DeLabel = "Warenkorb", SortOrder = 2 },
                new { Url = "/account/sign-in", DeUrl = "/account/sign-in", Label = "Sign in", DeLabel = "Anmelden", SortOrder = 3 },
                new { Url = "/account/register", DeUrl = "/account/register", Label = "Register", DeLabel = "Registrieren", SortOrder = 4 },
                new { Url = "/account", DeUrl = "/account", Label = "My account", DeLabel = "Mein Konto", SortOrder = 5 },
                new { Url = "/orders", DeUrl = "/orders", Label = "Orders", DeLabel = "Bestellungen", SortOrder = 6 },
                new { Url = "/invoices", DeUrl = "/invoices", Label = "Invoices", DeLabel = "Rechnungen", SortOrder = 7 },
                new { Url = "/help", DeUrl = "/help", Label = "Help", DeLabel = "Hilfe", SortOrder = 8 },
                new { Url = "/page/contact", DeUrl = "/page/kontakt", Label = "Contact", DeLabel = "Kontakt", SortOrder = 9 },
                new { Url = "/page/shipping", DeUrl = "/page/versand", Label = "Shipping", DeLabel = "Versand", SortOrder = 10 },
                new { Url = "/page/returns", DeUrl = "/page/rueckgabe", Label = "Returns / Cancellation", DeLabel = "Rueckgabe / Widerruf", SortOrder = 11 },
                new { Url = "/page/legal-notice", DeUrl = "/page/impressum", Label = "Legal notice", DeLabel = "Impressum", SortOrder = 12 },
                new { Url = "/page/privacy-policy", DeUrl = "/page/datenschutz", Label = "Privacy", DeLabel = "Datenschutz", SortOrder = 13 },
                new { Url = "/page/terms-and-conditions", DeUrl = "/page/agb", Label = "Terms", DeLabel = "AGB", SortOrder = 14 },
                new { Url = "/page/cancellation-policy", DeUrl = "/page/widerruf", Label = "Cancellation policy", DeLabel = "Widerrufsbelehrung", SortOrder = 15 }
            };

            if (menu == null)
            {
                menu = new Menu
                {
                    Name = "Footer",
                    Culture = "en-US"
                };
                db.Menus.Add(menu);
            }

            menu.Culture = "en-US";
            await ResetMenuItemsAsync(db, menu, ct);
            foreach (var item in desiredItems)
            {
                menu.Items.Add(new MenuItem
                {
                    Url = item.Url,
                    Title = item.Label,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    Translations =
                    {
                        new MenuItemTranslation
                        {
                            Culture = "en-US",
                            Label = item.Label,
                            Url = item.Url
                        },
                        new MenuItemTranslation
                        {
                            Culture = DomainDefaults.DefaultCulture,
                            Label = item.DeLabel,
                            Url = item.DeUrl
                        }
                    }
                });
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task ResetMenuItemsAsync(DarwinDbContext db, Menu menu, CancellationToken ct)
        {
            if (menu.Id == Guid.Empty)
            {
                menu.Items.Clear();
                return;
            }

            var existingItems = await db.MenuItems
                .Include(x => x.Translations)
                .Where(x => x.MenuId == menu.Id)
                .ToListAsync(ct);

            if (existingItems.Count == 0)
            {
                menu.Items.Clear();
                return;
            }

            db.Set<MenuItemTranslation>().RemoveRange(existingItems.SelectMany(x => x.Translations));
            db.MenuItems.RemoveRange(existingItems);
            menu.Items.Clear();
            await db.SaveChangesAsync(ct);
        }

        #endregion

        #region Pages

        private static bool ShouldRefreshStarterPageContent(string slug, string? contentHtml)
        {
            if (string.IsNullOrWhiteSpace(contentHtml))
            {
                return true;
            }

            if (slug is not ("impressum" or "datenschutz" or "agb" or "kontakt" or "widerruf"))
            {
                return false;
            }

            return contentHtml.Contains("Musterfirma GmbH", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Sample Company GmbH", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Max Mustermann", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("example.de", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("This sample page", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Replace every placeholder", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Sample cancellation form", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("starter text", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Add your actual", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Replace this", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Musterseite", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Beispielseite", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Platzhalter", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Ã", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("Â", StringComparison.OrdinalIgnoreCase)
                || contentHtml.Contains("â", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsThinStarterCmsContent(string? contentHtml)
        {
            if (string.IsNullOrWhiteSpace(contentHtml))
            {
                return true;
            }

            return contentHtml.Length < 360 && !contentHtml.Contains("<h2", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeCmsSeedText(string value)
        {
            return value
                .Replace("â€“", "-", StringComparison.Ordinal)
                .Replace("Â§", "Section ", StringComparison.Ordinal)
                .Replace("Ã¤", "ae", StringComparison.Ordinal)
                .Replace("Ã¶", "oe", StringComparison.Ordinal)
                .Replace("Ã¼", "ue", StringComparison.Ordinal)
                .Replace("Ã„", "Ae", StringComparison.Ordinal)
                .Replace("Ã–", "Oe", StringComparison.Ordinal)
                .Replace("Ãœ", "Ue", StringComparison.Ordinal)
                .Replace("ÃŸ", "ss", StringComparison.Ordinal)
                .Replace("Ã©", "e", StringComparison.Ordinal)
                .Replace("Â", string.Empty, StringComparison.Ordinal)
                .Replace("â", "-", StringComparison.Ordinal);
        }

        private static string BuildCmsMetaDescription(string title, bool english)
        {
            return english
                ? $"{title} for Darwin customers with practical guidance, service context, and storefront information."
                : $"{title} fuer Darwin Kunden mit praktischen Hinweisen, Servicekontext und Storefront-Informationen.";
        }

        private static string BuildExpandedCmsContent(string title, string html, bool english)
        {
            var normalizedHtml = NormalizeCmsSeedText(html);
            if (!IsThinStarterCmsContent(normalizedHtml))
            {
                return normalizedHtml;
            }

            if (english)
            {
                return $@"<h1>{title}</h1>
<p>This page gives Darwin customers a practical overview of the topic and connects the storefront, account area, order process and service channels.</p>
<h2>What customers can expect</h2>
<p>Customers find clear guidance, relevant next steps and the most important operational details in one place. The content is intended for browsing, search visibility and support conversations.</p>
<h2>Storefront context</h2>
<p>The information complements catalog browsing, checkout, account management and after-sales workflows so the public website can show useful content in English without falling back to another language.</p>
<h2>Next steps</h2>
<p>Use the related catalog, account or service links to continue the journey. The content team can later replace this seed copy with production-specific policies, contact paths and campaign messaging.</p>";
            }

            return $@"<h1>{title}</h1>
<p>Diese Seite gibt Darwin Kunden einen praktischen Ueberblick zum Thema und verbindet Storefront, Kundenkonto, Bestellprozess und Servicekanaele.</p>
<h2>Was Kunden erwarten koennen</h2>
<p>Kunden finden klare Hinweise, relevante naechste Schritte und die wichtigsten operativen Details an einem Ort. Der Inhalt ist fuer Browsing, Suchsichtbarkeit und Supportgespraeche geeignet.</p>
<h2>Storefront-Kontext</h2>
<p>Die Informationen ergaenzen Katalog, Checkout, Kontoverwaltung und After-Sales-Prozesse, damit die oeffentliche Website hilfreiche deutsche Inhalte ohne leere Platzhalter anzeigen kann.</p>
<h2>Naechste Schritte</h2>
<p>Nutzen Sie die passenden Katalog-, Konto- oder Service-Links, um die Customer Journey fortzusetzen. Das Content-Team kann diesen Seed-Text spaeter durch produktive Richtlinien und Kampagnen ersetzen.</p>";
        }
        /// <summary>
        /// Seeds at least 20 CMS pages with de-DE translations.
        /// </summary>
        private static async Task SeedPagesAsync(DarwinDbContext db, CancellationToken ct)
        {
            const string oldImpressumHtml = "<h1>Impressum</h1><p>Angaben gemäß §5 TMG.</p>";
            const string oldDatenschutzHtml = "<h1>Datenschutz</h1><p>Informationen gemäß DSGVO.</p>";
            const string oldAgbHtml = "<h1>Allgemeine Geschäftsbedingungen</h1><p>Bitte lesen Sie unsere Bedingungen.</p>";
            const string oldKontaktHtml = "<h1>Kontakt</h1><p>Wir sind für Sie da.</p>";
            const string oldWiderrufHtml = "<h1>Widerruf</h1><p>Formular & Informationen.</p>";

            const string impressumHtml = @"<h1>Impressum</h1>
<p>Diese Seite stellt die zentralen Unternehmens- und Kontaktinformationen des Darwin Storefronts bereit.</p>
<h2>Angaben gemäß § 5 TMG</h2>
<p><strong>Darwin GmbH</strong><br>Alexanderplatz 1<br>10115 Berlin<br>Deutschland</p>
<h2>Vertreten durch</h2>
<p>Darwin Management, Geschäftsführung</p>
<h2>Kontakt</h2>
<p>Telefon: +49 30 123456-0<br>E-Mail: info@darwin.de</p>
<h2>Registereintrag</h2>
<p>Eintragung im Handelsregister.<br>Registergericht: Amtsgericht Berlin-Charlottenburg<br>Registernummer: HRB 123456</p>
<h2>Umsatzsteuer-ID</h2>
<p>Umsatzsteuer-Identifikationsnummer gemäß § 27 a Umsatzsteuergesetz: DE123456789</p>
<h2>Verantwortlich für den Inhalt</h2>
<p>Darwin Management<br>Alexanderplatz 1<br>10115 Berlin</p>
<h2>Hinweis</h2>
<p>Bitte prüfen Sie vor dem Go-live gemeinsam mit Ihrer Rechtsberatung, ob weitere Pflichtangaben für Ihr Geschäftsmodell erforderlich sind.</p>";

            const string impressumEnHtml = @"<h1>Legal notice</h1>
<p>This legal notice page provides the core company and contact information for the Darwin storefront.</p>
<h2>Information according to section 5 TMG</h2>
<p><strong>Darwin GmbH</strong><br>Alexanderplatz 1<br>10115 Berlin<br>Germany</p>
<h2>Represented by</h2>
<p>Darwin Management, managing director</p>
<h2>Contact</h2>
<p>Phone: +49 30 123456-0<br>Email: info@darwin.de</p>
<h2>Commercial register</h2>
<p>Registered with the commercial register.<br>Register court: Berlin-Charlottenburg local court<br>Registration number: HRB 123456</p>
<h2>VAT ID</h2>
<p>VAT identification number according to section 27 a of the German VAT Act: DE123456789</p>
<h2>Responsible for content</h2>
<p>Darwin Management<br>Alexanderplatz 1<br>10115 Berlin</p>
<h2>Note</h2>
<p>Please review this page with legal counsel before going live to ensure that all mandatory disclosures for your business are covered.</p>";

            const string datenschutzHtml = @"<h1>Datenschutzerklärung</h1>
<p>Diese Datenschutzerklärung beschreibt die zentralen Datenverarbeitungen des Darwin Storefronts.</p>
<h2>1. Verantwortliche Stelle</h2>
<p>Darwin GmbH, Alexanderplatz 1, 10115 Berlin, Deutschland<br>E-Mail: privacy@darwin.de</p>
<h2>2. Verarbeitete Daten</h2>
<p>Beim Besuch dieser Website können insbesondere Bestandsdaten, Bestellinformationen, Kommunikationsdaten, Zahlungsinformationen sowie technische Zugriffsdaten verarbeitet werden.</p>
<h2>3. Zwecke der Verarbeitung</h2>
<p>Wir verarbeiten Daten zur Bereitstellung der Website, zur Vertragsabwicklung, für Kundenkommunikation, Betrugsprävention, Abrechnung und zur Verbesserung unseres Angebots.</p>
<h2>4. Rechtsgrundlagen</h2>
<p>Je nach Vorgang erfolgt die Verarbeitung auf Grundlage von Art. 6 Abs. 1 lit. b DSGVO, lit. c DSGVO, lit. f DSGVO oder einer erteilten Einwilligung nach lit. a DSGVO.</p>
<h2>5. Empfänger</h2>
<p>Daten können an Hosting-, Zahlungs-, Versand- und Kommunikationsdienstleister weitergegeben werden, soweit dies zur Erfüllung der jeweiligen Zwecke erforderlich ist.</p>
<h2>6. Speicherdauer</h2>
<p>Personenbezogene Daten werden nur so lange gespeichert, wie dies für den jeweiligen Zweck oder aufgrund gesetzlicher Aufbewahrungspflichten notwendig ist.</p>
<h2>7. Ihre Rechte</h2>
<p>Sie haben das Recht auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung, Datenübertragbarkeit sowie Widerspruch gegen bestimmte Verarbeitungen.</p>
<h2>8. Beschwerden</h2>
<p>Sie können sich bei einer Datenschutzaufsichtsbehörde beschweren, wenn Sie der Auffassung sind, dass die Verarbeitung Ihrer personenbezogenen Daten rechtswidrig erfolgt.</p>
<h2>9. Stand und Anpassungen</h2>
<p>Weitere Hinweise zu eingesetzten Tools, Cookie-Einstellungen, Newsletter-Prozessen und internationalen Datenübermittlungen werden je nach aktivierten Funktionen angezeigt.</p>";

            const string datenschutzEnHtml = @"<h1>Privacy policy</h1>
<p>This privacy policy page explains the core data processing areas used by the Darwin storefront. Review it with legal counsel before production use.</p>
<h2>1. Controller</h2>
<p>Darwin GmbH, Alexanderplatz 1, 10115 Berlin, Germany<br>Email: privacy@darwin.de</p>
<h2>2. Processed data</h2>
<p>When visiting this website, we may process customer master data, order information, communication data, payment information and technical access data.</p>
<h2>3. Purposes of processing</h2>
<p>We process data to provide the website, fulfil contracts, communicate with customers, prevent fraud, handle billing and improve our services.</p>
<h2>4. Legal bases</h2>
<p>Depending on the activity, processing may be based on Article 6(1)(b), (c), (f) GDPR or on consent under Article 6(1)(a) GDPR.</p>
<h2>5. Recipients</h2>
<p>Data may be shared with hosting, payment, shipping and communication service providers where required for the relevant purpose.</p>
<h2>6. Retention period</h2>
<p>Personal data is only stored for as long as necessary for the relevant purpose or to meet statutory retention requirements.</p>
<h2>7. Your rights</h2>
<p>You have the right to access, rectification, erasure, restriction of processing, data portability and objection to certain processing activities.</p>
<h2>8. Complaints</h2>
<p>You may lodge a complaint with a data protection supervisory authority if you believe that your personal data is being processed unlawfully.</p>
<h2>9. Review and adjustments</h2>
<p>Additional details about tools, cookie settings, newsletter processes and international data transfers are shown according to the enabled storefront features.</p>";

            const string agbHtml = @"<h1>Allgemeine Geschäftsbedingungen</h1>
<p>Diese AGB beschreiben die zentralen Regeln für Bestellung, Zahlung, Lieferung, Gewährleistung und Haftung im Darwin Storefront.</p>
<h2>1. Geltungsbereich</h2>
<p>Diese Bedingungen gelten für alle Bestellungen, die Verbraucher und Unternehmen über unseren Online-Shop mit der Darwin GmbH abschließen.</p>
<h2>2. Vertragspartner und Vertragsschluss</h2>
<p>Die Produktdarstellungen im Shop stellen noch kein bindendes Angebot dar. Ein Vertrag kommt erst zustande, wenn wir die Bestellung ausdrücklich bestätigen oder die Ware versenden.</p>
<h2>3. Preise und Zahlung</h2>
<p>Alle Preise verstehen sich inklusive gesetzlicher Mehrwertsteuer zuzüglich gegebenenfalls ausgewiesener Versandkosten. Die verfügbaren Zahlungsarten werden im Checkout angezeigt.</p>
<h2>4. Lieferung</h2>
<p>Lieferzeiten und Lieferbeschränkungen werden auf den jeweiligen Produktseiten und im Checkout ausgewiesen.</p>
<h2>5. Eigentumsvorbehalt</h2>
<p>Die Ware bleibt bis zur vollständigen Bezahlung unser Eigentum.</p>
<h2>6. Gewährleistung</h2>
<p>Es gelten die gesetzlichen Mängelhaftungsrechte, soweit nicht ausdrücklich etwas anderes vereinbart wurde.</p>
<h2>7. Haftung</h2>
<p>Wir haften unbeschränkt bei Vorsatz, grober Fahrlässigkeit, Verletzung von Leben, Körper oder Gesundheit sowie nach den zwingenden gesetzlichen Vorschriften.</p>
<h2>8. Streitbeilegung</h2>
<p>Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung bereit. Darwin informiert Kunden transparent über verfügbare Kontakt- und Klärungswege.</p>";

            const string agbEnHtml = @"<h1>Terms and conditions</h1>
<p>These terms describe the core ordering, payment, delivery and liability rules for the Darwin storefront. Review them with legal counsel before production use.</p>
<h2>1. Scope</h2>
<p>These terms apply to all orders placed through our online store by consumers and business customers with Darwin GmbH.</p>
<h2>2. Contract partner and conclusion of contract</h2>
<p>Product listings in the shop do not yet constitute a binding offer. A contract is only concluded once we expressly confirm the order or ship the goods.</p>
<h2>3. Prices and payment</h2>
<p>All prices include statutory VAT plus any shipping costs shown separately. The available payment methods are displayed during checkout.</p>
<h2>4. Delivery</h2>
<p>Delivery times and any delivery restrictions are shown on the relevant product pages and during checkout.</p>
<h2>5. Retention of title</h2>
<p>The goods remain our property until full payment has been received.</p>
<h2>6. Warranty</h2>
<p>Statutory warranty rights apply unless expressly agreed otherwise.</p>
<h2>7. Liability</h2>
<p>We are liable without limitation in cases of intent, gross negligence, injury to life, body or health, and wherever mandatory statutory law applies.</p>
<h2>8. Dispute resolution</h2>
<p>The European Commission provides an online dispute resolution platform. Darwin provides transparent contact and resolution channels for customers.</p>";

            const string widerrufHtml = @"<h1>Widerrufsbelehrung</h1>
<p>Diese Widerrufsbelehrung beschreibt den zentralen Ablauf für Verbraucherwiderrufe im Darwin Storefront.</p>
<h2>Widerrufsrecht</h2>
<p>Verbraucher haben das Recht, binnen vierzehn Tagen ohne Angabe von Gründen diesen Vertrag zu widerrufen.</p>
<h2>Widerrufsfrist</h2>
<p>Die Frist beträgt vierzehn Tage ab dem Tag, an dem der Verbraucher oder ein von ihm benannter Dritter die Waren in Besitz genommen hat.</p>
<h2>Ausübung des Widerrufs</h2>
<p>Um das Widerrufsrecht auszuüben, muss eine eindeutige Erklärung an die Darwin GmbH, Alexanderplatz 1, 10115 Berlin, E-Mail: widerruf@darwin.de gesendet werden.</p>
<h2>Folgen des Widerrufs</h2>
<p>Im Falle eines wirksamen Widerrufs sind alle Zahlungen einschließlich der Lieferkosten unverzüglich und spätestens binnen vierzehn Tagen zurückzuzahlen, vorbehaltlich gesetzlich zulässiger Abzüge.</p>
<h2>Widerrufsformular</h2>
<p>An die Darwin GmbH, Alexanderplatz 1, 10115 Berlin, widerruf@darwin.de:<br>Hiermit widerrufe ich den von mir abgeschlossenen Vertrag über den Kauf der folgenden Waren / die Erbringung der folgenden Dienstleistung ...</p>";

            const string widerrufEnHtml = @"<h1>Cancellation policy</h1>
<p>This cancellation policy describes the core withdrawal process for consumer orders in the Darwin storefront. Review it with legal counsel before production use.</p>
<h2>Right of cancellation</h2>
<p>Consumers have the right to cancel this contract within fourteen days without giving any reason.</p>
<h2>Cancellation period</h2>
<p>The cancellation period is fourteen days from the day on which the consumer or a third party named by the consumer took possession of the goods.</p>
<h2>How to exercise cancellation</h2>
<p>To exercise the right of cancellation, an unambiguous statement must be sent to Darwin GmbH, Alexanderplatz 1, 10115 Berlin, email: widerruf@darwin.de.</p>
<h2>Effects of cancellation</h2>
<p>If the cancellation is valid, all payments including delivery costs must be reimbursed without undue delay and no later than fourteen days, subject to any deductions permitted by law.</p>
<h2>Cancellation form</h2>
<p>To Darwin GmbH, Alexanderplatz 1, 10115 Berlin, widerruf@darwin.de:<br>I hereby cancel the contract concluded by me for the purchase of the following goods / the provision of the following service ...</p>";

            const string kontaktHtml = @"<h1>Kontakt</h1>
<p>Diese Kontaktseite stellt die zentralen Servicekanaele und Kontaktinformationen des Darwin Storefronts bereit.</p>
<h2>Kundenservice</h2>
<p>Telefon: +49 30 123456-0<br>E-Mail: service@darwin.de<br>Servicezeiten: Montag bis Freitag, 09:00 bis 18:00 Uhr</p>
<h2>Rueckfragen zu Bestellungen</h2>
<p>Bitte halten Sie Ihre Bestellnummer bereit, wenn Sie Fragen zu Versand, Rechnung oder Rueckgabe haben.</p>
<h2>Geschaeftsanschrift</h2>
<p>Darwin GmbH<br>Alexanderplatz 1<br>10115 Berlin<br>Deutschland</p>
<h2>Hinweis</h2>
<p>Die Servicekanaele, Reaktionszeiten und Eskalationswege werden hier fuer Kunden transparent gebuendelt.</p>";

            const string kontaktEnHtml = @"<h1>Contact</h1>
<p>This contact page provides the core service channels and contact details for the Darwin storefront.</p>
<h2>Customer service</h2>
<p>Phone: +49 30 123456-0<br>Email: service@darwin.de<br>Service hours: Monday to Friday, 09:00 to 18:00</p>
<h2>Order-related questions</h2>
<p>Please have your order number ready when contacting us about shipping, invoices or returns.</p>
<h2>Business address</h2>
<p>Darwin GmbH<br>Alexanderplatz 1<br>10115 Berlin<br>Germany</p>
<h2>Note</h2>
<p>Support channels, response times and escalation paths are collected here for customer transparency.</p>";

            // Common e-commerce pages for electronics
            var pages = new (string Slug, string Title, string Html, string? EnTitle, string? EnHtml, string? EnSlug)[]
            {
                ("startseite","Startseite","<h1>Willkommen</h1><p>Elektronik & Computer - Top-Angebote.</p>", "Home", "<h1>Welcome</h1><p>Electronics and computers - top deals for your home, office and mobile setup.</p>", "home"),
                ("ueber-uns","Ueber uns","<h1>Ueber uns</h1><p>Kompetenz fuer Technik seit 2010.</p>", "About us", "<h1>About us</h1><p>Technology expertise since 2010, with practical advice for electronics, computers and connected services.</p>", "about-us"),
                ("kontakt","Kontakt", kontaktHtml, "Contact", kontaktEnHtml, "contact"),
                ("impressum","Impressum", impressumHtml, "Legal notice", impressumEnHtml, "legal-notice"),
                ("datenschutz","Datenschutz", datenschutzHtml, "Privacy policy", datenschutzEnHtml, "privacy-policy"),
                ("agb","AGB", agbHtml, "Terms and conditions", agbEnHtml, "terms-and-conditions"),
                ("versand","Versand","<h1>Versand</h1><p>Lieferzeiten & -kosten.</p>", "Shipping", "<h1>Shipping</h1><p>Delivery times, shipping costs and available delivery options for your order.</p>", "shipping"),
                ("rueckgabe","Rueckgabe","<h1>Rueckgabe</h1><p>30 Tage Rueckgaberecht.</p>", "Returns", "<h1>Returns</h1><p>You can return eligible products within 30 days according to the published return conditions.</p>", "returns"),
                ("faq","FAQ","<h1>Haeufige Fragen</h1><p>Antworten auf Ihre Fragen.</p>", "FAQ", "<h1>Frequently asked questions</h1><p>Answers to common questions about products, orders, shipping, returns and customer accounts.</p>", "faq"),
                ("zahlung","Zahlung","<h1>Zahlung</h1><p>Alle gaengigen Zahlungsmethoden.</p>", "Payment", "<h1>Payment</h1><p>All common payment methods are shown during checkout, including any available invoice or card options.</p>", "payment"),
                ("reparatur-service","Reparatur-Service","<h1>Reparatur</h1><p>Fachwerkstatt fuer Geraete.</p>", "Repair service", "<h1>Repair service</h1><p>Specialist workshop support for selected devices, diagnostics and service requests.</p>", "repair-service"),
                ("garantie","Garantie","<h1>Garantie</h1><p>2 Jahre Herstellergarantie.</p>", "Warranty", "<h1>Warranty</h1><p>Information about manufacturer warranty, statutory rights and how to start a support request.</p>", "warranty"),
                ("filialen","Filialen","<h1>Filialen</h1><p>Standorte & Oeffnungszeiten.</p>", "Stores", "<h1>Stores</h1><p>Locations, opening hours and service points for in-person advice and pickup.</p>", "stores"),
                ("jobs","Jobs","<h1>Jobs</h1><p>Werde Teil unseres Teams.</p>", "Jobs", "<h1>Jobs</h1><p>Join our team and help build practical commerce experiences for technology customers.</p>", "jobs"),
                ("news","News","<h1>News</h1><p>Neuigkeiten & Angebote.</p>", "News", "<h1>News</h1><p>Latest updates, product launches, service announcements and current offers.</p>", "news"),
                ("marken","Marken","<h1>Marken</h1><p>Beliebte Hersteller im Ueberblick.</p>", "Brands", "<h1>Brands</h1><p>An overview of popular manufacturers and selected technology brands in the catalog.</p>", "brands"),
                ("kundenkonto","Kundenkonto","<h1>Mein Konto</h1><p>Einstellungen & Bestellungen.</p>", "Customer account", "<h1>My account</h1><p>Manage your profile, preferences, addresses, orders and account security settings.</p>", "customer-account"),
                ("widerruf","Widerruf", widerrufHtml, "Cancellation policy", widerrufEnHtml, "cancellation-policy"),
                ("datensicherheit","Datensicherheit","<h1>Datensicherheit</h1><p>Schutz Ihrer Daten.</p>", "Data security", "<h1>Data security</h1><p>How we protect account, order and communication data across the storefront.</p>", "data-security"),
                ("lieferstatus","Lieferstatus","<h1>Lieferstatus</h1><p>Sendungsverfolgung.</p>", "Delivery status", "<h1>Delivery status</h1><p>Track shipments and review delivery progress for your recent orders.</p>", "delivery-status"),
                ("geschenkkarten","Geschenkkarten","<h1>Geschenkkarten</h1><p>Das ideale Geschenk.</p>", "Gift cards", "<h1>Gift cards</h1><p>A flexible gift option for electronics, accessories and future purchases.</p>", "gift-cards")
            };

            var publishStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var p in pages)
            {
                var defaultTitle = NormalizeCmsSeedText(p.Title);
                var defaultHtml = BuildExpandedCmsContent(defaultTitle, p.Html, english: false);
                var defaultMetaDescription = BuildCmsMetaDescription(defaultTitle, english: false);
                var englishTitle = !string.IsNullOrWhiteSpace(p.EnTitle)
                    ? NormalizeCmsSeedText(p.EnTitle)
                    : null;
                var englishHtml = !string.IsNullOrWhiteSpace(p.EnHtml) && !string.IsNullOrWhiteSpace(englishTitle)
                    ? BuildExpandedCmsContent(englishTitle, p.EnHtml, english: true)
                    : null;
                var englishMetaDescription = englishTitle is null
                    ? null
                    : BuildCmsMetaDescription(englishTitle, english: true);

                var page = await db.Pages
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync(
                        x => x.Slug == p.Slug ||
                             x.Translations.Any(t => t.Culture == DomainDefaults.DefaultCulture && t.Slug == p.Slug),
                        ct);

                if (page == null)
                {
                    page = new Page();
                    db.Pages.Add(page);
                }

                page.Title = string.IsNullOrWhiteSpace(page.Title) ? defaultTitle : NormalizeCmsSeedText(page.Title);
                page.Slug = string.IsNullOrWhiteSpace(page.Slug) ? p.Slug : page.Slug;
                if (ShouldRefreshStarterPageContent(p.Slug, page.ContentHtml)
                    || IsThinStarterCmsContent(page.ContentHtml)
                    || (p.Slug == "impressum" && page.ContentHtml == oldImpressumHtml)
                    || (p.Slug == "datenschutz" && page.ContentHtml == oldDatenschutzHtml)
                    || (p.Slug == "agb" && page.ContentHtml == oldAgbHtml)
                    || (p.Slug == "kontakt" && page.ContentHtml == oldKontaktHtml)
                    || (p.Slug == "widerruf" && page.ContentHtml == oldWiderrufHtml))
                {
                    page.ContentHtml = defaultHtml;
                }
                else
                {
                    page.ContentHtml = NormalizeCmsSeedText(page.ContentHtml);
                }

                page.MetaTitle = string.IsNullOrWhiteSpace(page.MetaTitle) ? defaultTitle : NormalizeCmsSeedText(page.MetaTitle);
                page.MetaDescription = string.IsNullOrWhiteSpace(page.MetaDescription)
                    ? defaultMetaDescription
                    : NormalizeCmsSeedText(page.MetaDescription);
                page.MetaDescription = ShouldRefreshStarterPageContent(p.Slug, page.MetaDescription)
                    ? defaultMetaDescription
                    : NormalizeCmsSeedText(page.MetaDescription);
                page.IsPublished = true;
                page.Status = PageStatus.Published;
                page.PublishStartUtc ??= publishStartUtc;

                var translation = page.Translations.FirstOrDefault(x => x.Culture == DomainDefaults.DefaultCulture);
                if (translation == null)
                {
                    translation = new PageTranslation { Culture = DomainDefaults.DefaultCulture };
                    page.Translations.Add(translation);
                }

                translation.Title = string.IsNullOrWhiteSpace(translation.Title) ? defaultTitle : NormalizeCmsSeedText(translation.Title);
                translation.Slug = string.IsNullOrWhiteSpace(translation.Slug) ? p.Slug : translation.Slug;
                if (ShouldRefreshStarterPageContent(p.Slug, translation.ContentHtml)
                    || IsThinStarterCmsContent(translation.ContentHtml)
                    || (p.Slug == "impressum" && translation.ContentHtml == oldImpressumHtml)
                    || (p.Slug == "datenschutz" && translation.ContentHtml == oldDatenschutzHtml)
                    || (p.Slug == "agb" && translation.ContentHtml == oldAgbHtml)
                    || (p.Slug == "kontakt" && translation.ContentHtml == oldKontaktHtml)
                    || (p.Slug == "widerruf" && translation.ContentHtml == oldWiderrufHtml))
                {
                    translation.ContentHtml = defaultHtml;
                }
                else
                {
                    translation.ContentHtml = NormalizeCmsSeedText(translation.ContentHtml);
                }

                translation.MetaTitle = string.IsNullOrWhiteSpace(translation.MetaTitle) ? defaultTitle : NormalizeCmsSeedText(translation.MetaTitle);
                translation.MetaDescription = string.IsNullOrWhiteSpace(translation.MetaDescription)
                    ? defaultMetaDescription
                    : NormalizeCmsSeedText(translation.MetaDescription);

                translation.MetaDescription = ShouldRefreshStarterPageContent(p.Slug, translation.MetaDescription)
                    ? defaultMetaDescription
                    : NormalizeCmsSeedText(translation.MetaDescription);

                if (!string.IsNullOrWhiteSpace(englishTitle) && !string.IsNullOrWhiteSpace(englishHtml))
                {
                    var englishTranslation = page.Translations.FirstOrDefault(x => x.Culture == "en-US");
                    if (englishTranslation == null)
                    {
                        englishTranslation = new PageTranslation { Culture = "en-US" };
                        page.Translations.Add(englishTranslation);
                    }

                    englishTranslation.Title = string.IsNullOrWhiteSpace(englishTranslation.Title)
                        ? englishTitle
                        : NormalizeCmsSeedText(englishTranslation.Title);
                    englishTranslation.Slug = string.IsNullOrWhiteSpace(englishTranslation.Slug) || englishTranslation.Slug == p.Slug
                        ? (p.EnSlug ?? p.Slug)
                        : englishTranslation.Slug;
                    if (ShouldRefreshStarterPageContent(p.Slug, englishTranslation.ContentHtml)
                        || IsThinStarterCmsContent(englishTranslation.ContentHtml))
                    {
                        englishTranslation.ContentHtml = englishHtml;
                    }
                    else
                    {
                        englishTranslation.ContentHtml = NormalizeCmsSeedText(englishTranslation.ContentHtml);
                    }

                    englishTranslation.MetaTitle = string.IsNullOrWhiteSpace(englishTranslation.MetaTitle)
                        ? englishTitle
                        : NormalizeCmsSeedText(englishTranslation.MetaTitle);
                    englishTranslation.MetaDescription = string.IsNullOrWhiteSpace(englishTranslation.MetaDescription)
                        ? englishMetaDescription!
                        : NormalizeCmsSeedText(englishTranslation.MetaDescription);

                    englishTranslation.MetaDescription = ShouldRefreshStarterPageContent(p.Slug, englishTranslation.MetaDescription)
                        ? englishMetaDescription!
                        : NormalizeCmsSeedText(englishTranslation.MetaDescription!);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        #endregion
    }
}






