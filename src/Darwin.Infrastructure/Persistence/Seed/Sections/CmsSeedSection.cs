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
                new { Url = "/", Label = "Home", DeLabel = "Start", SortOrder = 0 },
                new { Url = "/catalog", Label = "Catalog", DeLabel = "Katalog", SortOrder = 1 },
                new { Url = "/cms", Label = "CMS", DeLabel = "Inhalte", SortOrder = 2 },
                new { Url = "/account", Label = "Account", DeLabel = "Konto", SortOrder = 3 },
                new { Url = "/cart", Label = "Cart", DeLabel = "Warenkorb", SortOrder = 4 },
                new { Url = "/checkout", Label = "Checkout", DeLabel = "Kasse", SortOrder = 5 },
                new { Url = "/orders", Label = "Orders", DeLabel = "Bestellungen", SortOrder = 6 },
                new { Url = "/invoices", Label = "Invoices", DeLabel = "Rechnungen", SortOrder = 7 },
                new { Url = "/loyalty", Label = "Loyalty", DeLabel = "Treue", SortOrder = 8 }
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
            menu.Items.Clear();
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
                            Label = item.Label
                        },
                        new MenuItemTranslation
                        {
                            Culture = DomainDefaults.DefaultCulture,
                            Label = item.DeLabel
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
                new { Url = "/cms/impressum", Label = "Legal notice", DeLabel = "Impressum", SortOrder = 0 },
                new { Url = "/cms/datenschutz", Label = "Privacy", DeLabel = "Datenschutz", SortOrder = 1 },
                new { Url = "/cms/agb", Label = "Terms", DeLabel = "AGB", SortOrder = 2 },
                new { Url = "/cms/widerruf", Label = "Cancellation", DeLabel = "Widerruf", SortOrder = 3 },
                new { Url = "/cms/kontakt", Label = "Contact", DeLabel = "Kontakt", SortOrder = 4 },
                new { Url = "/account/sign-in", Label = "Sign in", DeLabel = "Anmelden", SortOrder = 5 },
                new { Url = "/account/register", Label = "Register", DeLabel = "Registrieren", SortOrder = 6 },
                new { Url = "/account/profile", Label = "Profile", DeLabel = "Profil", SortOrder = 7 },
                new { Url = "/account/preferences", Label = "Preferences", DeLabel = "Praeferenzen", SortOrder = 8 },
                new { Url = "/account/addresses", Label = "Addresses", DeLabel = "Adressen", SortOrder = 9 },
                new { Url = "/account/security", Label = "Security", DeLabel = "Sicherheit", SortOrder = 10 },
                new { Url = "/orders", Label = "Orders", DeLabel = "Bestellungen", SortOrder = 11 },
                new { Url = "/invoices", Label = "Invoices", DeLabel = "Rechnungen", SortOrder = 12 },
                new { Url = "/mock-checkout", Label = "Mock checkout", DeLabel = "Mock-Kasse", SortOrder = 13 }
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
            menu.Items.Clear();
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
                            Label = item.Label
                        },
                        new MenuItemTranslation
                        {
                            Culture = DomainDefaults.DefaultCulture,
                            Label = item.DeLabel
                        }
                    }
                });
            }

            await db.SaveChangesAsync(ct);
        }

        #endregion

        #region Pages

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
<p>Diese Beispielseite zeigt, welche Angaben in einem deutschen Impressum typischerweise enthalten sein sollten. Ersetzen Sie alle Platzhalter durch die tatsächlichen Unternehmensdaten.</p>
<h2>Angaben gemäß § 5 TMG</h2>
<p><strong>Musterfirma GmbH</strong><br>Beispielstraße 12<br>10115 Berlin<br>Deutschland</p>
<h2>Vertreten durch</h2>
<p>Max Mustermann, Geschäftsführung</p>
<h2>Kontakt</h2>
<p>Telefon: +49 30 123456-0<br>E-Mail: info@example.de</p>
<h2>Registereintrag</h2>
<p>Eintragung im Handelsregister.<br>Registergericht: Amtsgericht Berlin-Charlottenburg<br>Registernummer: HRB 123456</p>
<h2>Umsatzsteuer-ID</h2>
<p>Umsatzsteuer-Identifikationsnummer gemäß § 27 a Umsatzsteuergesetz: DE123456789</p>
<h2>Verantwortlich für den Inhalt</h2>
<p>Max Mustermann<br>Beispielstraße 12<br>10115 Berlin</p>
<h2>Hinweis</h2>
<p>Bitte prüfen Sie vor dem Go-live gemeinsam mit Ihrer Rechtsberatung, ob weitere Pflichtangaben für Ihr Geschäftsmodell erforderlich sind.</p>";

            const string impressumEnHtml = @"<h1>Legal notice</h1>
<p>This sample page shows the information that is typically required for a German legal notice. Replace every placeholder with your actual company details.</p>
<h2>Information according to section 5 TMG</h2>
<p><strong>Sample Company GmbH</strong><br>Beispielstrasse 12<br>10115 Berlin<br>Germany</p>
<h2>Represented by</h2>
<p>Max Mustermann, managing director</p>
<h2>Contact</h2>
<p>Phone: +49 30 123456-0<br>Email: info@example.de</p>
<h2>Commercial register</h2>
<p>Registered with the commercial register.<br>Register court: Berlin-Charlottenburg local court<br>Registration number: HRB 123456</p>
<h2>VAT ID</h2>
<p>VAT identification number according to section 27 a of the German VAT Act: DE123456789</p>
<h2>Responsible for content</h2>
<p>Max Mustermann<br>Beispielstrasse 12<br>10115 Berlin</p>
<h2>Note</h2>
<p>Please review this page with legal counsel before going live to ensure that all mandatory disclosures for your business are covered.</p>";

            const string datenschutzHtml = @"<h1>Datenschutzerklärung</h1>
<p>Diese Musterseite zeigt eine sinnvolle Startstruktur für Datenschutzhinweise nach DSGVO. Sie ersetzt keine rechtliche Prüfung.</p>
<h2>1. Verantwortliche Stelle</h2>
<p>Musterfirma GmbH, Beispielstraße 12, 10115 Berlin, Deutschland<br>E-Mail: privacy@example.de</p>
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
<p>Ergänzen Sie diese Vorlage um die konkret eingesetzten Tools, Cookie-Hinweise, Newsletter-Prozesse und Drittlandtransfers Ihres tatsächlichen Betriebs.</p>";

            const string datenschutzEnHtml = @"<h1>Privacy policy</h1>
<p>This sample page shows a sensible starting structure for GDPR-related privacy information. It does not replace legal review.</p>
<h2>1. Controller</h2>
<p>Sample Company GmbH, Beispielstrasse 12, 10115 Berlin, Germany<br>Email: privacy@example.de</p>
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
<p>Complete this starter text with the concrete tools, cookies, newsletter processes and international data transfers used in your real operation.</p>";

            const string agbHtml = @"<h1>Allgemeine Geschäftsbedingungen</h1>
<p>Diese Muster-AGB dienen als Startpunkt für die inhaltliche Ausarbeitung. Sie müssen vor Veröffentlichung an das tatsächliche Geschäftsmodell angepasst und rechtlich geprüft werden.</p>
<h2>1. Geltungsbereich</h2>
<p>Diese Bedingungen gelten für alle Bestellungen, die Verbraucher und Unternehmen über unseren Online-Shop mit der Musterfirma GmbH abschließen.</p>
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
<p>Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung bereit. Bitte ergänzen Sie hier Ihre tatsächliche Erklärung zur Teilnahme an Verbraucherschlichtungsverfahren.</p>";

            const string agbEnHtml = @"<h1>Terms and conditions</h1>
<p>These sample terms are a starting point only. They must be adapted to the real business model and legally reviewed before publication.</p>
<h2>1. Scope</h2>
<p>These terms apply to all orders placed through our online store by consumers and business customers with Sample Company GmbH.</p>
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
<p>The European Commission provides an online dispute resolution platform. Add your actual statement here regarding participation in consumer dispute resolution proceedings.</p>";

            const string widerrufHtml = @"<h1>Widerrufsbelehrung</h1>
<p>Diese Musterseite enthält die wichtigsten Bausteine für Verbraucherinformationen zum Widerrufsrecht im Fernabsatz. Prüfen Sie die finale Fassung vor Veröffentlichung rechtlich.</p>
<h2>Widerrufsrecht</h2>
<p>Verbraucher haben das Recht, binnen vierzehn Tagen ohne Angabe von Gründen diesen Vertrag zu widerrufen.</p>
<h2>Widerrufsfrist</h2>
<p>Die Frist beträgt vierzehn Tage ab dem Tag, an dem der Verbraucher oder ein von ihm benannter Dritter die Waren in Besitz genommen hat.</p>
<h2>Ausübung des Widerrufs</h2>
<p>Um das Widerrufsrecht auszuüben, muss eine eindeutige Erklärung an die Musterfirma GmbH, Beispielstraße 12, 10115 Berlin, E-Mail: widerruf@example.de gesendet werden.</p>
<h2>Folgen des Widerrufs</h2>
<p>Im Falle eines wirksamen Widerrufs sind alle Zahlungen einschließlich der Lieferkosten unverzüglich und spätestens binnen vierzehn Tagen zurückzuzahlen, vorbehaltlich gesetzlich zulässiger Abzüge.</p>
<h2>Muster-Widerrufsformular</h2>
<p>An die Musterfirma GmbH, Beispielstraße 12, 10115 Berlin, widerruf@example.de:<br>Hiermit widerrufe ich den von mir abgeschlossenen Vertrag über den Kauf der folgenden Waren / die Erbringung der folgenden Dienstleistung ...</p>";

            const string widerrufEnHtml = @"<h1>Cancellation policy</h1>
<p>This sample page contains the main building blocks for consumer cancellation information in German distance selling. Please obtain legal review before publication.</p>
<h2>Right of cancellation</h2>
<p>Consumers have the right to cancel this contract within fourteen days without giving any reason.</p>
<h2>Cancellation period</h2>
<p>The cancellation period is fourteen days from the day on which the consumer or a third party named by the consumer took possession of the goods.</p>
<h2>How to exercise cancellation</h2>
<p>To exercise the right of cancellation, an unambiguous statement must be sent to Sample Company GmbH, Beispielstrasse 12, 10115 Berlin, email: widerruf@example.de.</p>
<h2>Effects of cancellation</h2>
<p>If the cancellation is valid, all payments including delivery costs must be reimbursed without undue delay and no later than fourteen days, subject to any deductions permitted by law.</p>
<h2>Sample cancellation form</h2>
<p>To Sample Company GmbH, Beispielstrasse 12, 10115 Berlin, widerruf@example.de:<br>I hereby cancel the contract concluded by me for the purchase of the following goods / the provision of the following service ...</p>";

            const string kontaktHtml = @"<h1>Kontakt</h1>
<p>Diese Musterseite zeigt eine sinnvolle Startstruktur fuer die Kontakt- und Serviceinformationen eines in Deutschland betriebenen Storefronts.</p>
<h2>Kundenservice</h2>
<p>Telefon: +49 30 123456-0<br>E-Mail: service@example.de<br>Servicezeiten: Montag bis Freitag, 09:00 bis 18:00 Uhr</p>
<h2>Rueckfragen zu Bestellungen</h2>
<p>Bitte halten Sie Ihre Bestellnummer bereit, wenn Sie Fragen zu Versand, Rechnung oder Rueckgabe haben.</p>
<h2>Geschaeftsanschrift</h2>
<p>Musterfirma GmbH<br>Beispielstraße 12<br>10115 Berlin<br>Deutschland</p>
<h2>Hinweis</h2>
<p>Ersetzen Sie diese Angaben vor dem Go-live durch die echten Servicekanaele, Reaktionszeiten und Eskalationswege Ihres Unternehmens.</p>";

            const string kontaktEnHtml = @"<h1>Contact</h1>
<p>This sample page shows a sensible starting structure for contact and service information on a Germany-oriented storefront.</p>
<h2>Customer service</h2>
<p>Phone: +49 30 123456-0<br>Email: service@example.de<br>Service hours: Monday to Friday, 09:00 to 18:00</p>
<h2>Order-related questions</h2>
<p>Please have your order number ready when contacting us about shipping, invoices or returns.</p>
<h2>Business address</h2>
<p>Sample Company GmbH<br>Beispielstrasse 12<br>10115 Berlin<br>Germany</p>
<h2>Note</h2>
<p>Replace this starter text before go-live with your real support channels, response times and escalation paths.</p>";

            // Common e-commerce pages for electronics
            var pages = new (string Slug, string Title, string Html, string? EnTitle, string? EnHtml)[]
            {
                ("startseite","Startseite","<h1>Willkommen</h1><p>Elektronik & Computer – Top-Angebote.</p>", null, null),
                ("ueber-uns","Über uns","<h1>Über uns</h1><p>Kompetenz für Technik seit 2010.</p>", null, null),
                ("kontakt","Kontakt", kontaktHtml, "Contact", kontaktEnHtml),
                ("impressum","Impressum", impressumHtml, "Legal notice", impressumEnHtml),
                ("datenschutz","Datenschutz", datenschutzHtml, "Privacy policy", datenschutzEnHtml),
                ("agb","AGB", agbHtml, "Terms and conditions", agbEnHtml),
                ("versand","Versand","<h1>Versand</h1><p>Lieferzeiten & -kosten.</p>", null, null),
                ("rueckgabe","Rückgabe","<h1>Rückgabe</h1><p>30 Tage Rückgaberecht.</p>", null, null),
                ("faq","FAQ","<h1>Häufige Fragen</h1><p>Antworten auf Ihre Fragen.</p>", null, null),
                ("zahlung","Zahlung","<h1>Zahlung</h1><p>Alle gängigen Zahlungsmethoden.</p>", null, null),
                ("reparatur-service","Reparatur-Service","<h1>Reparatur</h1><p>Fachwerkstatt für Geräte.</p>", null, null),
                ("garantie","Garantie","<h1>Garantie</h1><p>2 Jahre Herstellergarantie.</p>", null, null),
                ("filialen","Filialen","<h1>Filialen</h1><p>Standorte & Öffnungszeiten.</p>", null, null),
                ("jobs","Jobs","<h1>Jobs</h1><p>Werde Teil unseres Teams.</p>", null, null),
                ("news","News","<h1>News</h1><p>Neuigkeiten & Angebote.</p>", null, null),
                ("marken","Marken","<h1>Marken</h1><p>Beliebte Hersteller im Überblick.</p>", null, null),
                ("kundenkonto","Kundenkonto","<h1>Mein Konto</h1><p>Einstellungen & Bestellungen.</p>", null, null),
                ("widerruf","Widerruf", widerrufHtml, "Cancellation policy", widerrufEnHtml),
                ("datensicherheit","Datensicherheit","<h1>Datensicherheit</h1><p>Schutz Ihrer Daten.</p>", null, null),
                ("lieferstatus","Lieferstatus","<h1>Lieferstatus</h1><p>Sendungsverfolgung.</p>", null, null),
                ("geschenkkarten","Geschenkkarten","<h1>Geschenkkarten</h1><p>Das ideale Geschenk.</p>", null, null)
            };

            var publishStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var p in pages)
            {
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

                page.Title = string.IsNullOrWhiteSpace(page.Title) ? p.Title : page.Title;
                page.Slug = string.IsNullOrWhiteSpace(page.Slug) ? p.Slug : page.Slug;
                if (string.IsNullOrWhiteSpace(page.ContentHtml)
                    || (p.Slug == "impressum" && page.ContentHtml == oldImpressumHtml)
                    || (p.Slug == "datenschutz" && page.ContentHtml == oldDatenschutzHtml)
                    || (p.Slug == "agb" && page.ContentHtml == oldAgbHtml)
                    || (p.Slug == "kontakt" && page.ContentHtml == oldKontaktHtml)
                    || (p.Slug == "widerruf" && page.ContentHtml == oldWiderrufHtml))
                {
                    page.ContentHtml = p.Html;
                }

                page.MetaTitle = string.IsNullOrWhiteSpace(page.MetaTitle) ? p.Title : page.MetaTitle;
                page.MetaDescription = string.IsNullOrWhiteSpace(page.MetaDescription)
                    ? $"{p.Title} – Informationen & Details."
                    : page.MetaDescription;
                page.IsPublished = true;
                page.Status = PageStatus.Published;
                page.PublishStartUtc ??= publishStartUtc;

                var translation = page.Translations.FirstOrDefault(x => x.Culture == DomainDefaults.DefaultCulture);
                if (translation == null)
                {
                    translation = new PageTranslation { Culture = DomainDefaults.DefaultCulture };
                    page.Translations.Add(translation);
                }

                translation.Title = string.IsNullOrWhiteSpace(translation.Title) ? p.Title : translation.Title;
                translation.Slug = string.IsNullOrWhiteSpace(translation.Slug) ? p.Slug : translation.Slug;
                if (string.IsNullOrWhiteSpace(translation.ContentHtml)
                    || (p.Slug == "impressum" && translation.ContentHtml == oldImpressumHtml)
                    || (p.Slug == "datenschutz" && translation.ContentHtml == oldDatenschutzHtml)
                    || (p.Slug == "agb" && translation.ContentHtml == oldAgbHtml)
                    || (p.Slug == "kontakt" && translation.ContentHtml == oldKontaktHtml)
                    || (p.Slug == "widerruf" && translation.ContentHtml == oldWiderrufHtml))
                {
                    translation.ContentHtml = p.Html;
                }

                translation.MetaTitle = string.IsNullOrWhiteSpace(translation.MetaTitle) ? p.Title : translation.MetaTitle;
                translation.MetaDescription = string.IsNullOrWhiteSpace(translation.MetaDescription)
                    ? $"{p.Title} – Informationen & Details."
                    : translation.MetaDescription;

                if (!string.IsNullOrWhiteSpace(p.EnTitle) && !string.IsNullOrWhiteSpace(p.EnHtml))
                {
                    var englishTranslation = page.Translations.FirstOrDefault(x => x.Culture == "en-US");
                    if (englishTranslation == null)
                    {
                        englishTranslation = new PageTranslation { Culture = "en-US" };
                        page.Translations.Add(englishTranslation);
                    }

                    englishTranslation.Title = string.IsNullOrWhiteSpace(englishTranslation.Title)
                        ? p.EnTitle
                        : englishTranslation.Title;
                    englishTranslation.Slug = string.IsNullOrWhiteSpace(englishTranslation.Slug)
                        ? p.Slug
                        : englishTranslation.Slug;
                    if (string.IsNullOrWhiteSpace(englishTranslation.ContentHtml))
                    {
                        englishTranslation.ContentHtml = p.EnHtml;
                    }

                    englishTranslation.MetaTitle = string.IsNullOrWhiteSpace(englishTranslation.MetaTitle)
                        ? p.EnTitle
                        : englishTranslation.MetaTitle;
                    englishTranslation.MetaDescription = string.IsNullOrWhiteSpace(englishTranslation.MetaDescription)
                        ? $"{p.EnTitle} – information and guidance."
                        : englishTranslation.MetaDescription;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        #endregion
    }
}






