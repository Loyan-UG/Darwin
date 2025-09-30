using System;

namespace Darwin.Application.Settings.DTOs
{
    /// <summary>
    /// Data transfer object representing the single SiteSetting entity as read from
    /// the database. This model is used to transfer the current settings to the
    /// Admin UI for display and editing. It includes the RowVersion token for
    /// optimistic concurrency control.
    /// </summary>
    public sealed class SiteSettingDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Basic site information
        public string Title { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ContactEmail { get; set; }

        // Routing
        public string? HomeSlug { get; set; } = "home";

        // Localization settings
        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";
        public string? DefaultCountry { get; set; } = "DE";
        public string DefaultCurrency { get; set; } = "EUR";
        public string? TimeZone { get; set; } = "Europe/Berlin";
        public string? DateFormat { get; set; } = "yyyy-MM-dd";
        public string? TimeFormat { get; set; } = "HH:mm";

        // Measurement and unit settings
        public string MeasurementSystem { get; set; } = "Metric";
        public string? DisplayWeightUnit { get; set; } = "kg";
        public string? DisplayLengthUnit { get; set; } = "cm";
        // Additional measurement & formatting overrides
        public string? MeasurementSettingsJson { get; set; }
        public string? NumberFormattingOverridesJson { get; set; }

        // SEO settings
        public bool EnableCanonical { get; set; } = true;
        public bool HreflangEnabled { get; set; } = true;
        public string? SeoTitleTemplate { get; set; } = "{title} | {site}";
        /// <summary>
        /// Default meta description template used when pages lack custom metadata.
        /// Corresponds to <see cref="Darwin.Domain.Entities.Settings.SiteSetting.SeoMetaDescriptionTemplate"/>.
        /// </summary>
        public string? SeoMetaDescriptionTemplate { get; set; }
        /// <summary>
        /// Default OpenGraph values serialized as JSON (e.g., site_name, image). See
        /// <see cref="Darwin.Domain.Entities.Settings.SiteSetting.OpenGraphDefaultsJson"/>.
        /// </summary>
        public string? OpenGraphDefaultsJson { get; set; }

        // Analytics settings
        public string? GoogleAnalyticsId { get; set; }
        public string? GoogleTagManagerId { get; set; }
        public string? GoogleSearchConsoleVerification { get; set; }

        // Feature flags (serialized as JSON)
        public string? FeatureFlagsJson { get; set; }

        // WhatsApp integration settings
        public bool WhatsAppEnabled { get; set; }
        public string? WhatsAppBusinessPhoneId { get; set; }
        public string? WhatsAppAccessToken { get; set; }
        public string? WhatsAppFromPhoneE164 { get; set; }
        public string? WhatsAppAdminRecipientsCsv { get; set; }

        // WebAuthn

        /// <summary>
        /// WebAuthn relying party identifier (RP ID). Usually your registrable domain (e.g., "example.com").
        /// Must match the effective domain of your site; on dev you may use "localhost".
        /// </summary>
        public string WebAuthnRelyingPartyId { get; set; } = "localhost";

        /// <summary>
        /// Human-friendly relying party name shown by authenticators (e.g., "Darwin Store").
        /// </summary>
        public string WebAuthnRelyingPartyName { get; set; } = "Darwin";

        /// <summary>
        /// Comma-separated list of allowed origins for WebAuthn ceremonies (e.g., "https://shop.example.com,https://admin.example.com").
        /// On development you may include "https://localhost:5001".
        /// </summary>
        public string WebAuthnAllowedOriginsCsv { get; set; } = "https://localhost:5001";

        /// <summary>
        /// When true, requires user verification (e.g., biometric/PIN) during WebAuthn authentication.
        /// If false, "preferred" verification is used to maximize compatibility.
        /// </summary>
        public bool WebAuthnRequireUserVerification { get; set; } = false;


        // SMTP
        public bool SmtpEnabled { get; set; } = false;
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public bool SmtpEnableSsl { get; set; } = true;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public string? SmtpFromAddress { get; set; }
        public string? SmtpFromDisplayName { get; set; }

        // SMS
        public bool SmsEnabled { get; set; } = false;
        public string? SmsProvider { get; set; }
        public string? SmsFromPhoneE164 { get; set; }
        public string? SmsApiKey { get; set; }
        public string? SmsApiSecret { get; set; }
        public string? SmsExtraSettingsJson { get; set; }

        // Admin alert routing
        public string? AdminAlertEmailsCsv { get; set; }
        public string? AdminAlertSmsRecipientsCsv { get; set; }
    }
}