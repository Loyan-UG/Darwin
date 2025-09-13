using System;
using System.ComponentModel.DataAnnotations;

namespace Darwin.Web.Areas.Admin.ViewModels.Settings
{
    /// <summary>
    /// View model backing the SiteSettings edit form. Keeps separation between the
    /// persisted DTOs and the Razor view. Data annotations define human-readable
    /// labels and provide client-side validation hints (server-side validation is
    /// handled by FluentValidation in the Application layer).
    /// </summary>
    public sealed class SiteSettingVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Basic
        [Display(Name = "Title")]
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Logo URL")]
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [Display(Name = "Contact Email")]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        // Localization
        [Display(Name = "Default Culture")]
        [Required, MaxLength(10)]
        public string DefaultCulture { get; set; } = "de-DE";

        [Display(Name = "Supported Cultures")]
        [Required]
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";

        [Display(Name = "Default Country")]
        [MaxLength(2)]
        public string? DefaultCountry { get; set; } = "DE";

        [Display(Name = "Default Currency")]
        [Required, MaxLength(3)]
        public string DefaultCurrency { get; set; } = "EUR";

        [Display(Name = "Time Zone")]
        public string? TimeZone { get; set; } = "Europe/Berlin";

        [Display(Name = "Date Format")]
        public string? DateFormat { get; set; } = "yyyy-MM-dd";

        [Display(Name = "Time Format")]
        public string? TimeFormat { get; set; } = "HH:mm";

        // Measurement & units
        [Display(Name = "Measurement System")]
        public string MeasurementSystem { get; set; } = "Metric";

        [Display(Name = "Display Weight Unit")]
        public string? DisplayWeightUnit { get; set; } = "kg";

        [Display(Name = "Display Length Unit")]
        public string? DisplayLengthUnit { get; set; } = "cm";

        // SEO
        [Display(Name = "Enable Canonical")]
        public bool EnableCanonical { get; set; } = true;

        [Display(Name = "Hreflang Enabled")]
        public bool HreflangEnabled { get; set; } = true;

        [Display(Name = "SEO Title Template")]
        [MaxLength(150)]
        public string? SeoTitleTemplate { get; set; } = "{title} | {site}";

        [Display(Name = "SEO Meta Description Template")]
        [MaxLength(200)]
        public string? SeoMetaDescriptionTemplate { get; set; }

        [Display(Name = "Open Graph Defaults (JSON)")]
        [MaxLength(2000)]
        public string? OpenGraphDefaultsJson { get; set; }

        // Analytics
        [Display(Name = "Google Analytics ID")]
        public string? GoogleAnalyticsId { get; set; }

        [Display(Name = "Google Tag Manager ID")]
        public string? GoogleTagManagerId { get; set; }

        [Display(Name = "Google Search Console Verification")]
        public string? GoogleSearchConsoleVerification { get; set; }

        // Feature flags (JSON)
        [Display(Name = "Feature Flags (JSON)")]
        public string? FeatureFlagsJson { get; set; }

        // WhatsApp Integration
        [Display(Name = "WhatsApp Enabled")]
        public bool WhatsAppEnabled { get; set; }

        [Display(Name = "WhatsApp Business Phone ID")]
        public string? WhatsAppBusinessPhoneId { get; set; }

        [Display(Name = "WhatsApp Access Token")]
        public string? WhatsAppAccessToken { get; set; }

        [Display(Name = "WhatsApp From Phone (E.164)")]
        public string? WhatsAppFromPhoneE164 { get; set; }

        [Display(Name = "WhatsApp Admin Recipients (CSV)")]
        public string? WhatsAppAdminRecipientsCsv { get; set; }

        // Additional measurement & formatting overrides
        [Display(Name = "Measurement Settings (JSON)")]
        public string? MeasurementSettingsJson { get; set; }

        [Display(Name = "Number Formatting Overrides (JSON)")]
        public string? NumberFormattingOverridesJson { get; set; }

        // Routing
        [Display(Name = "Home Slug")]
        public string? HomeSlug { get; set; } = "home";
    }
}