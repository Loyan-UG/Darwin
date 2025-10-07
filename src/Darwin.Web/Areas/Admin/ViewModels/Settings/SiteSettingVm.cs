using System;
using System.ComponentModel.DataAnnotations;

namespace Darwin.Web.Areas.Admin.ViewModels.Settings
{
    /// <summary>
    /// View model backing the Site Settings edit form. Mirrors <c>SiteSettingDto</c> fields
    /// for a 1:1 mapping, but uses DataAnnotations for display names and client hints.
    /// Server-side validation is enforced by FluentValidation in the Application layer.
    /// </summary>
    public sealed class SiteSettingVm
    {
        public Guid Id { get; set; }

        /// <summary>
        /// RowVersion for optimistic concurrency. Required by the Application handler.
        /// </summary>
        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // ---------- Basics ----------
        [Display(Name = "Title"), Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Logo URL"), MaxLength(500)]
        public string? LogoUrl { get; set; }

        [Display(Name = "Contact Email"), EmailAddress]
        public string? ContactEmail { get; set; }

        // ---------- Routing ----------
        [Display(Name = "Home Slug")]
        public string? HomeSlug { get; set; } = "home";

        // ---------- Localization ----------
        [Display(Name = "Default Culture"), Required, MaxLength(10)]
        public string DefaultCulture { get; set; } = "de-DE";

        [Display(Name = "Supported Cultures"), Required]
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";

        [Display(Name = "Default Country"), MaxLength(2)]
        public string? DefaultCountry { get; set; } = "DE";

        [Display(Name = "Default Currency"), Required, MaxLength(3)]
        public string DefaultCurrency { get; set; } = "EUR";

        [Display(Name = "Time Zone")]
        public string? TimeZone { get; set; } = "Europe/Berlin";

        [Display(Name = "Date Format")]
        public string? DateFormat { get; set; } = "yyyy-MM-dd";

        [Display(Name = "Time Format")]
        public string? TimeFormat { get; set; } = "HH:mm";

        // ---------- Units & Formatting ----------
        [Display(Name = "Measurement System")]
        public string MeasurementSystem { get; set; } = "Metric";

        [Display(Name = "Display Weight Unit")]
        public string? DisplayWeightUnit { get; set; } = "kg";

        [Display(Name = "Display Length Unit")]
        public string? DisplayLengthUnit { get; set; } = "cm";

        [Display(Name = "Measurement Settings (JSON)")]
        public string? MeasurementSettingsJson { get; set; }

        [Display(Name = "Number Formatting Overrides (JSON)")]
        public string? NumberFormattingOverridesJson { get; set; }

        // ---------- SEO ----------
        [Display(Name = "Enable Canonical")]
        public bool EnableCanonical { get; set; } = true;

        [Display(Name = "Hreflang Enabled")]
        public bool HreflangEnabled { get; set; } = true;

        [Display(Name = "SEO Title Template"), MaxLength(150)]
        public string? SeoTitleTemplate { get; set; } = "{title} | {site}";

        [Display(Name = "SEO Meta Description Template"), MaxLength(200)]
        public string? SeoMetaDescriptionTemplate { get; set; }

        [Display(Name = "OpenGraph Defaults (JSON)"), MaxLength(2000)]
        public string? OpenGraphDefaultsJson { get; set; }

        // ---------- Analytics ----------
        [Display(Name = "Google Analytics ID")]
        public string? GoogleAnalyticsId { get; set; }

        [Display(Name = "Google Tag Manager ID")]
        public string? GoogleTagManagerId { get; set; }

        [Display(Name = "Google Search Console Verification")]
        public string? GoogleSearchConsoleVerification { get; set; }

        // ---------- Feature Flags ----------
        [Display(Name = "Feature Flags (JSON)")]
        public string? FeatureFlagsJson { get; set; }

        // ---------- WhatsApp ----------
        [Display(Name = "Enable WhatsApp")]
        public bool WhatsAppEnabled { get; set; }

        [Display(Name = "Business Phone ID")]
        public string? WhatsAppBusinessPhoneId { get; set; }

        [Display(Name = "Access Token")]
        public string? WhatsAppAccessToken { get; set; }

        [Display(Name = "From Phone (E.164)")]
        public string? WhatsAppFromPhoneE164 { get; set; }

        [Display(Name = "Admin Recipients (CSV)")]
        public string? WhatsAppAdminRecipientsCsv { get; set; }

        // ---------- WebAuthn ----------
        [Display(Name = "WebAuthn RP ID")]
        public string WebAuthnRelyingPartyId { get; set; } = "localhost";

        [Display(Name = "WebAuthn RP Name")]
        public string WebAuthnRelyingPartyName { get; set; } = "Darwin";

        [Display(Name = "WebAuthn Allowed Origins (CSV)")]
        public string WebAuthnAllowedOriginsCsv { get; set; } = "https://localhost:5001";

        [Display(Name = "Require User Verification")]
        public bool WebAuthnRequireUserVerification { get; set; } = false;

        // ---------- SMTP ----------
        [Display(Name = "Enable SMTP")]
        public bool SmtpEnabled { get; set; }

        [Display(Name = "SMTP Host")]
        public string? SmtpHost { get; set; }

        [Display(Name = "SMTP Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "Enable SSL")]
        public bool SmtpEnableSsl { get; set; } = true;

        [Display(Name = "SMTP Username")]
        public string? SmtpUsername { get; set; }

        [Display(Name = "SMTP Password")]
        public string? SmtpPassword { get; set; }

        [Display(Name = "From Address")]
        public string? SmtpFromAddress { get; set; }

        [Display(Name = "From Display Name")]
        public string? SmtpFromDisplayName { get; set; }

        // ---------- SMS ----------
        [Display(Name = "Enable SMS")]
        public bool SmsEnabled { get; set; }

        [Display(Name = "SMS Provider")]
        public string? SmsProvider { get; set; }

        [Display(Name = "From Phone (E.164)")]
        public string? SmsFromPhoneE164 { get; set; }

        [Display(Name = "API Key")]
        public string? SmsApiKey { get; set; }

        [Display(Name = "API Secret")]
        public string? SmsApiSecret { get; set; }

        [Display(Name = "Extra Settings (JSON)")]
        public string? SmsExtraSettingsJson { get; set; }

        // ---------- Admin Routing ----------
        [Display(Name = "Admin Alert Emails (CSV)")]
        public string? AdminAlertEmailsCsv { get; set; }

        [Display(Name = "Admin Alert SMS Recipients (CSV, E.164)")]
        public string? AdminAlertSmsRecipientsCsv { get; set; }
    }
}
