using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Common;
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
                DefaultCulture = DomainDefaults.DefaultCulture,
                SupportedCulturesCsv = DomainDefaults.SupportedCulturesCsv,
                DefaultCountry = DomainDefaults.DefaultCountryCode,
                DefaultCurrency = DomainDefaults.DefaultCurrency,
                TimeZone = DomainDefaults.DefaultTimezone,
                DateFormat = "dd.MM.yyyy",
                TimeFormat = "HH:mm",

                // JWT / Access-Refresh tokens
                JwtEnabled = true,
                JwtIssuer = "Darwin",
                JwtAudience = "Darwin.PublicApi",
                JwtAccessTokenMinutes = 15,
                JwtRefreshTokenDays = 30,
                // NOTE: For dev, a random key is fine; in prod must be long random.
                JwtSigningKey = "3WvY3E2y3oX7k7Vg2y7r9kJr1Z0F1+f2tP6o3Q2i4G9z6L5k8N1q3R6u8W2x5Z7C9m1T3V6Y8c2D4F6H8J0L2N4Q6S8U0W2Y4",

                JwtPreviousSigningKey = "uKq9v8x7Gk1v++e2tqV0E7r3a9kYh3m1v2x5b8c5e7h9j2kL1m3o5q7s9u0w2y4z6A8B+DxFhJkMnPq",
                JwtEmitScopes = false,

                JwtSingleDeviceOnly = false,
                JwtRequireDeviceBinding = true,
                JwtClockSkewSeconds = 120,

                // Mobile app bootstrap defaults (keep in sync with Contracts defaults).
                // These values are not secrets; they define client polling behavior and
                // offline/outbox safety limits.
                MobileQrTokenRefreshSeconds = 60,
                MobileMaxOutboxItems = 100,
                BusinessManagementWebsiteUrl = "https://www.loyan.de",
                ImpressumUrl = "https://www.loyan.de/impressum",
                PrivacyPolicyUrl = "https://www.loyan.de/datenschutz",
                BusinessTermsUrl = "https://www.loyan.de/agb-business",
                AccountDeletionUrl = "https://www.loyan.de/account-deletion",
                StripeEnabled = false,
                StripePublishableKey = null,
                StripeSecretKey = null,
                StripeWebhookSecret = null,
                StripeMerchantDisplayName = "Darwin",
                VatEnabled = true,
                DefaultVatRatePercent = 19m,
                PricesIncludeVat = true,
                AllowReverseCharge = true,
                InvoiceIssuerLegalName = "Darwin GmbH",
                InvoiceIssuerTaxId = "DE123456789",
                InvoiceIssuerAddressLine1 = "Musterstrasse 1",
                InvoiceIssuerPostalCode = "10115",
                InvoiceIssuerCity = "Berlin",
                InvoiceIssuerCountry = DomainDefaults.DefaultCountryCode,
                DhlEnabled = false,
                DhlEnvironment = "Sandbox",
                DhlApiBaseUrl = "https://api-sandbox.dhl.example",
                DhlApiKey = null,
                DhlApiSecret = null,
                DhlAccountNumber = null,
                DhlShipperName = "Darwin",
                DhlShipperEmail = "ops@darwin.de",
                DhlShipperPhoneE164 = "+4915112345678",
                DhlShipperStreet = "Musterstrasse 1",
                DhlShipperPostalCode = "10115",
                DhlShipperCity = "Berlin",
                DhlShipperCountry = DomainDefaults.DefaultCountryCode,
                ShipmentAttentionDelayHours = 24,
                ShipmentTrackingGraceHours = 12,

                // Soft delete / data retention
                SoftDeleteCleanupEnabled = true,
                SoftDeleteRetentionDays = 90,
                SoftDeleteCleanupBatchSize = 500,



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
                TransactionalEmailSubjectPrefix = "[Darwin]",
                CommunicationTestInboxEmail = null,
                CommunicationTestSmsRecipientE164 = null,
                CommunicationTestWhatsAppRecipientE164 = null,
                CommunicationTestEmailSubjectTemplate = "Darwin communication test for {channel}",
                CommunicationTestEmailBodyTemplate = "<p>This is a Darwin {channel} communication test.</p><ul><li><strong>Requested by:</strong> {requested_by}</li><li><strong>Attempted at (UTC):</strong> {attempted_at_utc}</li><li><strong>Target:</strong> {test_target}</li><li><strong>Transport state:</strong> {transport_state}</li></ul><p>This diagnostic is intended only for the configured communication test target.</p>",
                CommunicationTestSmsTemplate = "Darwin SMS transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.",
                CommunicationTestWhatsAppTemplate = "Darwin WhatsApp transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.",
                BusinessInvitationEmailSubjectTemplate = "Invitation to join {business_name} on Darwin",
                BusinessInvitationEmailBodyTemplate = "<p>Hello,</p><p>{invitation_intro_html}</p>{acceptance_link_html}<p>Your invitation token is:</p><p><code>{token}</code></p><p>This invitation expires at <strong>{expires_at_utc}</strong>.</p><p>Use this token in the Darwin business onboarding flow or contact your administrator if you need assistance.</p>",
                AccountActivationEmailSubjectTemplate = "Confirm your Darwin account email",
                AccountActivationEmailBodyTemplate = "<p>Hello,</p><p>Use the following token to confirm the Darwin account email for <strong>{email}</strong>:</p><p><code>{token}</code></p><p>This token expires at <strong>{expires_at_utc}</strong>.</p>",
                PasswordResetEmailSubjectTemplate = "Reset your Darwin account password",
                PasswordResetEmailBodyTemplate = "<p>Hello,</p><p>Use the following token to reset the Darwin account password for <strong>{email}</strong>:</p><p><code>{token}</code></p><p>This token expires at <strong>{expires_at_utc}</strong>.</p>",
                PhoneVerificationSmsTemplate = "Your Darwin verification code is {token}. It expires at {expires_at_utc} UTC.",
                PhoneVerificationWhatsAppTemplate = "Confirm your Darwin mobile number with code {token}. It expires at {expires_at_utc} UTC.",
                PhoneVerificationPreferredChannel = "Sms",
                PhoneVerificationAllowFallback = true,

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
