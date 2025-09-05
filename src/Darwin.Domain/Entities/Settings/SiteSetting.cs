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
        /// <summary>Default ISO 3166-1 alpha-2 country code, e.g., "DE".</summary>
        public string DefaultCountry { get; set; } = "DE";
        /// <summary>Default ISO 4217 currency code (e.g., "EUR").</summary>
        public string DefaultCurrency { get; set; } = "EUR";
        /// <summary>Application time zone identifier used for display (storage is UTC).</summary>
        public string TimeZone { get; set; } = "Europe/Berlin";
        /// <summary>Date and time formatting patterns for display.</summary>
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        public string TimeFormat { get; set; } = "HH:mm";


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
    }
}