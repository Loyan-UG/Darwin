using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds a single SiteSetting row with German defaults (culture, formats, and integrations).
    /// </summary>
    public sealed class SiteSettingsSeedSection
    {
        private readonly ILogger<SiteSettingsSeedSection> _logger;

        /// <summary>Parameterless ctor for compatibility; uses NullLogger if DI not present.</summary>
        public SiteSettingsSeedSection() : this(NullLogger<SiteSettingsSeedSection>.Instance) { }

        public SiteSettingsSeedSection(ILogger<SiteSettingsSeedSection> logger)
        {
            _logger = logger ?? NullLogger<SiteSettingsSeedSection>.Instance;
        }

        /// <summary>
        /// Creates a default settings record when none exists. Idempotent.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding SiteSettings ...");

            var exists = await db.Set<SiteSetting>().AnyAsync(ct);
            if (exists)
            {
                _logger.LogInformation("SiteSettings already present. Skipping.");
                return;
            }

            var s = new SiteSetting
            {
                // Basic
                Title = "Darwin",
                LogoUrl = "/img/logo.svg",
                ContactEmail = "admin@darwin.de",

                // Culture & Currency
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                DefaultCountry = "DE",
                DefaultCurrency = "EUR",
                TimeZone = "Europe/Berlin",
                DateFormat = "dd.MM.yyyy",
                TimeFormat = "HH:mm",

                // JWT / Access-Refresh tokens
                JwtEnabled = true,
                JwtIssuer = "Darwin",
                JwtAudience = "Darwin.PublicApi",
                JwtAccessTokenMinutes = 15,
                JwtRefreshTokenDays = 30,
                // NOTE: For dev, a random key is fine; in prod must be long random.
                JwtSigningKey = "uKq9v8x7Gk1v++e2tqV0E7r3a9kYh3m1v2x5b8c5e7h9j2kL1m3o5q7s9u0w2y4z6A8B+DxFhJkMnPq",

                JwtPreviousSigningKey = null,
                JwtEmitScopes = false,


                // Measurement & Units
                MeasurementSystem = "Metric",
                DisplayWeightUnit = "kg",
                DisplayLengthUnit = "cm",

                // URL/SEO
                EnableCanonical = true,
                HreflangEnabled = true,
                SeoTitleTemplate = "{title} | Darwin",
                SeoMetaDescriptionTemplate = "{title} – Online-Shop",
                OpenGraphDefaultsJson = "{\"site_name\":\"Darwin\",\"locale\":\"de_DE\"}",

                // Analytics (empty by default)
                GoogleAnalyticsId = null,
                GoogleTagManagerId = null,
                GoogleSearchConsoleVerification = null,

                // Feature flags
                FeatureFlagsJson = "{}",

                // WebAuthn (Passkeys)
                WebAuthnRelyingPartyId = "localhost",
                WebAuthnRelyingPartyName = "Darwin Dev (DE)",
                WebAuthnAllowedOriginsCsv = "https://localhost:5001,https://localhost:7170",
                WebAuthnRequireUserVerification = false,

                // SMTP (disabled by default in seed; configured later via Admin)
                SmtpEnabled = false,
                SmtpHost = "smtp.darwin.de",
                SmtpPort = 587,
                SmtpEnableSsl = true,
                SmtpUsername = "no-reply@darwin.de",
                SmtpPassword = "",

                // Admin alert channels (email/SMS to ops)
                AdminAlertEmailsCsv = "ops@darwin.de",
                AdminAlertSmsRecipientsCsv = "+4915112345678",

                // SMS (disabled by default)
                SmsEnabled = false,
                SmsProvider = null,
                SmsFromPhoneE164 = "+4915112345678",
                SmsApiKey = null,
                SmsApiSecret = null,
                SmsExtraSettingsJson = null,

                // WhatsApp (disabled by default)
                WhatsAppEnabled = false,
                WhatsAppBusinessPhoneId = null,
                WhatsAppAccessToken = null,
                WhatsAppFromPhoneE164 = null,
                WhatsAppAdminRecipientsCsv = null,

                // Number formatting overrides for German locale (comma decimal, dot thousands)
                NumberFormattingOverridesJson = "{\"decimalSeparator\":\",\",\"thousandsSeparator\":\".\",\"groupSize\":3}",

                // Advanced measurement (leave empty = derive from system)
                MeasurementSettingsJson = null,

                // Home URL slug (German)
                HomeSlug = "startseite"
            };

            db.Set<SiteSetting>().Add(s);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("SiteSettings seeding done.");
        }
    }
}
