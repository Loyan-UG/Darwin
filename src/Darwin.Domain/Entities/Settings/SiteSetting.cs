using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Settings
{
    /// <summary>
    /// Site-wide settings editable from Admin. Cached with invalidation.
    /// </summary>
    public sealed class SiteSetting : BaseEntity
    {
        // General
        /// <summary>Public site title displayed in headers and page titles.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Absolute/relative URL to site logo image file.</summary>
        public string? LogoUrl { get; set; }
        /// <summary>Primary contact email for the site (support/inquiries).</summary>
        public string ContactEmail { get; set; } = string.Empty;
        /// <summary>Slug of the home page used for routing.</summary>
        public string HomeSlug { get; set; } = "home";
        /// <summary>Default culture code, e.g., "de-DE".</summary>
        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US"; // comma-separated
        /// <summary>Default ISO 3166-1 alpha-2 country code, e.g., "DE".</summary>
        public string DefaultCountry { get; set; } = "DE";
        /// <summary>Default ISO 4217 currency code (e.g., "EUR").</summary>
        public string DefaultCurrency { get; set; } = "EUR";
        /// <summary>Application time zone identifier used for display (storage is UTC).</summary>
        public string TimeZone { get; set; } = "Europe/Berlin";
        /// <summary>Date and time formatting patterns for display.</summary>
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        public string TimeFormat { get; set; } = "HH:mm";



        // JWT / Access-Refresh tokens
        public bool JwtEnabled { get; set; } = true;
        public string? JwtIssuer { get; set; } = "Darwin";
        public string? JwtAudience { get; set; } = "Darwin.PublicApi";
        public int JwtAccessTokenMinutes { get; set; } = 15;
        public int JwtRefreshTokenDays { get; set; } = 30;

        /// <summary>
        /// Current HMAC symmetric signing key material (Base64 or raw text).
        /// Rotation strategy: move current key here; previous key to JwtPreviousSigningKey.
        /// </summary>
        public string? JwtSigningKey { get; set; }

        /// <summary>
        /// Previous signing key to allow rolling rotation during a grace period.
        /// </summary>
        public string? JwtPreviousSigningKey { get; set; }

        /// <summary>
        /// If true, embeds "scope" claim from a short CSV (e.g., "api.read").
        /// Prefer not to embed roles/permissions fully in the token.
        /// </summary>
        public bool JwtEmitScopes { get; set; } = false;




        // Display & Units
        /// <summary>Measurement system for display (e.g., "Metric" or "Imperial").</summary>
        public string MeasurementSystem { get; set; } = "Metric";
        /// <summary>Preferred display unit for mass (e.g., "kg", "lb").</summary>
        public string DisplayWeightUnit { get; set; } = "kg";
        /// <summary>Preferred display unit for length/dimensions (e.g., "cm", "in").</summary>
        public string DisplayLengthUnit { get; set; } = "cm";
        /// <summary>Optional JSON to extend measurement and formatting preferences (precision, rounding, symbol placement).</summary>
        public string? MeasurementSettingsJson { get; set; }
        /// <summary>Optional overrides for number/currency separators and formats; by default derived from culture.</summary>
        public string? NumberFormattingOverridesJson { get; set; }


        // SEO
        /// <summary>Pattern for composing HTML titles, e.g., "{PageTitle} | {SiteTitle}".</summary>
        public string? SeoTitleTemplate { get; set; }
        /// <summary>Default meta description template for pages lacking custom metadata.</summary>
        public string? SeoMetaDescriptionTemplate { get; set; }
        /// <summary>Default OpenGraph values serialized as JSON (e.g., site_name, image).</summary>
        public string? OpenGraphDefaultsJson { get; set; }
        /// <summary>When true, output canonical link tags on pages.</summary>
        public bool EnableCanonical { get; set; } = true;
        /// <summary>When true, output hreflang link tags for alternate languages.</summary>
        public bool HreflangEnabled { get; set; } = true;


        // Analytics
        public string? GoogleAnalyticsId { get; set; }
        public string? GoogleTagManagerId { get; set; }
        public string? GoogleSearchConsoleVerification { get; set; }


        // Feature Flags
        /// <summary>Feature flags serialized as JSON key-value pairs (toggled at runtime).</summary>
        public string? FeatureFlagsJson { get; set; }


        // WhatsApp
        /// <summary>Enable WhatsApp notifications integration.</summary>
        public bool WhatsAppEnabled { get; set; }
        /// <summary>Meta Business Phone ID for Cloud API.</summary>
        public string? WhatsAppBusinessPhoneId { get; set; }
        /// <summary>Access token used for Cloud API calls. Rotate regularly.</summary>
        public string? WhatsAppAccessToken { get; set; }
        /// <summary>Sender phone number in E.164 format.</summary>
        public string? WhatsAppFromPhoneE164 { get; set; }
        /// <summary>Comma-separated admin recipient phone numbers in E.164 format.</summary>
        public string? WhatsAppAdminRecipientsCsv { get; set; }


        /// <summary>
        /// WebAuthn relying party identifier (RP ID). Usually the registrable domain ("example.com") or "localhost" for dev.
        /// Must match the effective host where WebAuthn ceremonies happen.
        /// </summary>
        public string WebAuthnRelyingPartyId { get; set; } = "localhost";

        /// <summary>
        /// Human-friendly RP name shown by authenticators (e.g., "Darwin").
        /// </summary>
        public string WebAuthnRelyingPartyName { get; set; } = "Darwin";

        /// <summary>
        /// Comma-separated list of allowed origins (scheme + host [+ port]).
        /// Examples: "https://shop.example.com,https://admin.example.com,https://localhost:5001".
        /// </summary>
        public string WebAuthnAllowedOriginsCsv { get; set; } = "https://localhost:5001";

        /// <summary>
        /// If true, user verification is required during authentication (biometric/PIN).
        /// If false, the server prefers UV but does not require it (broader compatibility).
        /// </summary>
        public bool WebAuthnRequireUserVerification { get; set; } = false;


        // ---- Add to Darwin.Domain.Entities.Settings.SiteSetting (new fields) ----

        // Email (SMTP)
        /// <summary>Enable SMTP email delivery globally (disable to route emails to a sink/dev-null).</summary>
        public bool SmtpEnabled { get; set; } = false;
        /// <summary>SMTP server host name, e.g., "smtp.contoso.com".</summary>
        public string? SmtpHost { get; set; }
        /// <summary>SMTP server port (e.g., 587 for STARTTLS, 465 for implicit TLS).</summary>
        public int? SmtpPort { get; set; }
        /// <summary>Whether to use SSL/TLS on the SMTP connection.</summary>
        public bool SmtpEnableSsl { get; set; } = true;
        /// <summary>SMTP username; leave empty for anonymous relay.</summary>
        public string? SmtpUsername { get; set; }
        /// <summary>SMTP password/secret (consider at-rest protection).</summary>
        public string? SmtpPassword { get; set; }
        /// <summary>Default From address for outgoing emails.</summary>
        public string? SmtpFromAddress { get; set; }
        /// <summary>Default From display name for outgoing emails.</summary>
        public string? SmtpFromDisplayName { get; set; }

        // SMS (provider-agnostic)
        /// <summary>Enable SMS notifications globally.</summary>
        public bool SmsEnabled { get; set; } = false;
        /// <summary>Provider key (e.g., "Twilio", "Vonage", "CustomHttp").</summary>
        public string? SmsProvider { get; set; }
        /// <summary>Sender phone number in E.164 format (if provider requires).</summary>
        public string? SmsFromPhoneE164 { get; set; }
        /// <summary>API key/client id for the SMS provider.</summary>
        public string? SmsApiKey { get; set; }
        /// <summary>API secret/token for the SMS provider.</summary>
        public string? SmsApiSecret { get; set; }
        /// <summary>Optional JSON for provider-specific extra settings (endpoints, templates).</summary>
        public string? SmsExtraSettingsJson { get; set; }

        // Notifications routing defaults (future-friendly)
        /// <summary>Comma-separated default admin recipients for email alerts.</summary>
        public string? AdminAlertEmailsCsv { get; set; }
        /// <summary>Comma-separated default admin recipients for SMS alerts (E.164).</summary>
        public string? AdminAlertSmsRecipientsCsv { get; set; }

    }
}