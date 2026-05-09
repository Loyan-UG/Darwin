using System;
using System.ComponentModel.DataAnnotations;
using Darwin.Application.Settings.DTOs;
using Darwin.WebAdmin.Localization;

namespace Darwin.WebAdmin.ViewModels.Settings
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
        [Display(Name = "SiteSettingTitle"), Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "SiteSettingLogoUrl"), MaxLength(500)]
        public string? LogoUrl { get; set; }

        [Display(Name = "SiteSettingContactEmail"), EmailAddress]
        public string? ContactEmail { get; set; }

        // ---------- Routing ----------
        [Display(Name = "SiteSettingHomeSlug")]
        public string? HomeSlug { get; set; } = "home";

        // ---------- Localization ----------
        [Display(Name = "SiteSettingDefaultCulture"), Required, MaxLength(10)]
        public string DefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;

        [Display(Name = "SiteSettingSupportedCulturesCsv"), Required]
        public string SupportedCulturesCsv { get; set; } = AdminCultureCatalog.SupportedCulturesCsvDefault;

        [Display(Name = "SiteSettingMultilingualEnabled")]
        public bool MultilingualEnabled { get; set; } = true;

        [Display(Name = "SiteSettingDefaultCountry"), MaxLength(2)]
        public string? DefaultCountry { get; set; } = SiteSettingDto.DefaultCountryDefault;

        [Display(Name = "SiteSettingDefaultCurrency"), Required, MaxLength(3)]
        public string DefaultCurrency { get; set; } = string.Empty;

        [Display(Name = "SiteSettingTimeZone")]
        public string? TimeZone { get; set; }

        [Display(Name = "SiteSettingDateFormat")]
        public string? DateFormat { get; set; } = "yyyy-MM-dd";

        [Display(Name = "SiteSettingTimeFormat")]
        public string? TimeFormat { get; set; } = "HH:mm";

        [Display(Name = "SiteSettingAdminTextOverridesJson")]
        public string? AdminTextOverridesJson { get; set; }

        // ---------- Security / JWT ----------
        [Display(Name = "SiteSettingJwtEnabled")]
        public bool JwtEnabled { get; set; } = true;

        [Display(Name = "SiteSettingJwtIssuer")]
        public string JwtIssuer { get; set; } = "Darwin";

        [Display(Name = "SiteSettingJwtAudience")]
        public string JwtAudience { get; set; } = "Darwin.PublicApi";

        [Display(Name = "SiteSettingJwtAccessTokenMinutes")]
        public int JwtAccessTokenMinutes { get; set; } = 15;

        [Display(Name = "SiteSettingJwtRefreshTokenDays")]
        public int JwtRefreshTokenDays { get; set; } = 30;

        [Display(Name = "SiteSettingJwtSigningKey")]
        public string JwtSigningKey { get; set; } = string.Empty;

        [Display(Name = "SiteSettingJwtPreviousSigningKey")]
        public string? JwtPreviousSigningKey { get; set; }

        [Display(Name = "SiteSettingJwtEmitScopes")]
        public bool JwtEmitScopes { get; set; }

        [Display(Name = "SiteSettingJwtSingleDeviceOnly")]
        public bool JwtSingleDeviceOnly { get; set; }

        [Display(Name = "SiteSettingJwtRequireDeviceBinding")]
        public bool JwtRequireDeviceBinding { get; set; } = true;

        [Display(Name = "SiteSettingJwtClockSkewSeconds")]
        public int JwtClockSkewSeconds { get; set; } = 60;

        // ---------- Mobile bootstrap ----------
        [Display(Name = "SiteSettingMobileQrTokenRefreshSeconds")]
        public int MobileQrTokenRefreshSeconds { get; set; }

        [Display(Name = "SiteSettingMobileMaxOutboxItems")]
        public int MobileMaxOutboxItems { get; set; }

        [Display(Name = "SiteSettingBusinessManagementWebsiteUrl")]
        public string? BusinessManagementWebsiteUrl { get; set; }

        [Display(Name = "SiteSettingImpressumUrl")]
        public string? ImpressumUrl { get; set; }

        [Display(Name = "SiteSettingPrivacyPolicyUrl")]
        public string? PrivacyPolicyUrl { get; set; }

        [Display(Name = "SiteSettingBusinessTermsUrl")]
        public string? BusinessTermsUrl { get; set; }

        [Display(Name = "SiteSettingAccountDeletionUrl")]
        public string? AccountDeletionUrl { get; set; }

        // ---------- Payments (Stripe-first) ----------
        [Display(Name = "SiteSettingStripeEnabled")]
        public bool StripeEnabled { get; set; }

        [Display(Name = "SiteSettingStripePublishableKey")]
        public string? StripePublishableKey { get; set; }

        [Display(Name = "SiteSettingStripeSecretKey")]
        public string? StripeSecretKey { get; set; }

        [Display(Name = "SiteSettingStripeWebhookSecret")]
        public string? StripeWebhookSecret { get; set; }

        [Display(Name = "SiteSettingStripeMerchantDisplayName")]
        public string? StripeMerchantDisplayName { get; set; }

        // ---------- Tax / VAT / Invoicing ----------
        [Display(Name = "SiteSettingVatEnabled")]
        public bool VatEnabled { get; set; } = true;

        [Display(Name = "SiteSettingDefaultVatRatePercent")]
        public decimal DefaultVatRatePercent { get; set; } = 19m;

        [Display(Name = "SiteSettingPricesIncludeVat")]
        public bool PricesIncludeVat { get; set; } = true;

        [Display(Name = "SiteSettingAllowReverseCharge")]
        public bool AllowReverseCharge { get; set; } = false;

        [Display(Name = "SiteSettingInvoiceIssuerLegalName")]
        public string? InvoiceIssuerLegalName { get; set; }

        [Display(Name = "SiteSettingInvoiceIssuerTaxId")]
        public string? InvoiceIssuerTaxId { get; set; }

        [Display(Name = "SiteSettingInvoiceIssuerAddressLine1")]
        public string? InvoiceIssuerAddressLine1 { get; set; }

        [Display(Name = "SiteSettingInvoiceIssuerPostalCode")]
        public string? InvoiceIssuerPostalCode { get; set; }

        [Display(Name = "SiteSettingInvoiceIssuerCity")]
        public string? InvoiceIssuerCity { get; set; }

        [Display(Name = "SiteSettingInvoiceIssuerCountry")]
        public string? InvoiceIssuerCountry { get; set; }

        [Display(Name = "SiteSettingInvoiceArchiveRetentionYears")]
        public int InvoiceArchiveRetentionYears { get; set; } = 10;

        // ---------- Shipping (DHL-first) ----------
        [Display(Name = "SiteSettingDhlEnabled")]
        public bool DhlEnabled { get; set; }

        [Display(Name = "SiteSettingDhlEnvironment")]
        public string? DhlEnvironment { get; set; }

        [Display(Name = "SiteSettingDhlApiBaseUrl")]
        public string? DhlApiBaseUrl { get; set; }

        [Display(Name = "SiteSettingDhlApiKey")]
        public string? DhlApiKey { get; set; }

        [Display(Name = "SiteSettingDhlApiSecret")]
        public string? DhlApiSecret { get; set; }

        [Display(Name = "SiteSettingDhlAccountNumber")]
        public string? DhlAccountNumber { get; set; }

        [Display(Name = "SiteSettingDhlShipperName")]
        public string? DhlShipperName { get; set; }

        [Display(Name = "SiteSettingDhlShipperEmail")]
        public string? DhlShipperEmail { get; set; }

        [Display(Name = "SiteSettingDhlShipperPhoneE164")]
        public string? DhlShipperPhoneE164 { get; set; }

        [Display(Name = "SiteSettingDhlShipperStreet")]
        public string? DhlShipperStreet { get; set; }

        [Display(Name = "SiteSettingDhlShipperPostalCode")]
        public string? DhlShipperPostalCode { get; set; }

        [Display(Name = "SiteSettingDhlShipperCity")]
        public string? DhlShipperCity { get; set; }

        [Display(Name = "SiteSettingDhlShipperCountry")]
        public string? DhlShipperCountry { get; set; }

        [Display(Name = "SiteSettingShipmentAttentionDelayHours")]
        public int ShipmentAttentionDelayHours { get; set; } = 24;

        [Display(Name = "SiteSettingShipmentTrackingGraceHours")]
        public int ShipmentTrackingGraceHours { get; set; } = 12;

        // ---------- Soft delete / retention ----------
        [Display(Name = "SiteSettingSoftDeleteCleanupEnabled")]
        public bool SoftDeleteCleanupEnabled { get; set; } = true;

        [Display(Name = "SiteSettingSoftDeleteRetentionDays")]
        public int SoftDeleteRetentionDays { get; set; } = 90;

        [Display(Name = "SiteSettingSoftDeleteCleanupBatchSize")]
        public int SoftDeleteCleanupBatchSize { get; set; } = 500;

        // ---------- Units & Formatting ----------
        [Display(Name = "SiteSettingMeasurementSystem")]
        public string MeasurementSystem { get; set; } = "Metric";

        [Display(Name = "SiteSettingDisplayWeightUnit")]
        public string? DisplayWeightUnit { get; set; } = "kg";

        [Display(Name = "SiteSettingDisplayLengthUnit")]
        public string? DisplayLengthUnit { get; set; } = "cm";

        [Display(Name = "SiteSettingMeasurementSettingsJson")]
        public string? MeasurementSettingsJson { get; set; }

        [Display(Name = "SiteSettingNumberFormattingOverridesJson")]
        public string? NumberFormattingOverridesJson { get; set; }

        // ---------- SEO ----------
        [Display(Name = "SiteSettingEnableCanonical")]
        public bool EnableCanonical { get; set; } = true;

        [Display(Name = "SiteSettingHreflangEnabled")]
        public bool HreflangEnabled { get; set; } = true;

        [Display(Name = "SiteSettingSeoTitleTemplate"), MaxLength(150)]
        public string? SeoTitleTemplate { get; set; } = "{title} | {site}";

        [Display(Name = "SiteSettingSeoMetaDescriptionTemplate"), MaxLength(200)]
        public string? SeoMetaDescriptionTemplate { get; set; }

        [Display(Name = "SiteSettingOpenGraphDefaultsJson"), MaxLength(2000)]
        public string? OpenGraphDefaultsJson { get; set; }

        // ---------- Analytics ----------
        [Display(Name = "SiteSettingGoogleAnalyticsId")]
        public string? GoogleAnalyticsId { get; set; }

        [Display(Name = "SiteSettingGoogleTagManagerId")]
        public string? GoogleTagManagerId { get; set; }

        [Display(Name = "SiteSettingGoogleSearchConsoleVerification")]
        public string? GoogleSearchConsoleVerification { get; set; }

        // ---------- Feature Flags ----------
        [Display(Name = "SiteSettingFeatureFlagsJson")]
        public string? FeatureFlagsJson { get; set; }

        // ---------- WhatsApp ----------
        [Display(Name = "SiteSettingWhatsAppEnabled")]
        public bool WhatsAppEnabled { get; set; }

        [Display(Name = "SiteSettingWhatsAppBusinessPhoneId")]
        public string? WhatsAppBusinessPhoneId { get; set; }

        [Display(Name = "SiteSettingWhatsAppAccessToken")]
        public string? WhatsAppAccessToken { get; set; }

        [Display(Name = "SiteSettingWhatsAppFromPhoneE164")]
        public string? WhatsAppFromPhoneE164 { get; set; }

        [Display(Name = "SiteSettingWhatsAppAdminRecipientsCsv")]
        public string? WhatsAppAdminRecipientsCsv { get; set; }

        // ---------- WebAuthn ----------
        [Display(Name = "SiteSettingWebAuthnRelyingPartyId")]
        public string WebAuthnRelyingPartyId { get; set; } = "localhost";

        [Display(Name = "SiteSettingWebAuthnRelyingPartyName")]
        public string WebAuthnRelyingPartyName { get; set; } = "Darwin";

        [Display(Name = "SiteSettingWebAuthnAllowedOriginsCsv")]
        public string WebAuthnAllowedOriginsCsv { get; set; } = "https://localhost:5001";

        [Display(Name = "SiteSettingWebAuthnRequireUserVerification")]
        public bool WebAuthnRequireUserVerification { get; set; } = false;

        // ---------- SMTP ----------
        [Display(Name = "SiteSettingSmtpEnabled")]
        public bool SmtpEnabled { get; set; }

        [Display(Name = "SiteSettingSmtpHost")]
        public string? SmtpHost { get; set; }

        [Display(Name = "SiteSettingSmtpPort")]
        public int? SmtpPort { get; set; }

        [Display(Name = "SiteSettingSmtpEnableSsl")]
        public bool SmtpEnableSsl { get; set; } = true;

        [Display(Name = "SiteSettingSmtpUsername")]
        public string? SmtpUsername { get; set; }

        [Display(Name = "SiteSettingSmtpPassword")]
        public string? SmtpPassword { get; set; }

        [Display(Name = "SiteSettingSmtpFromAddress")]
        public string? SmtpFromAddress { get; set; }

        [Display(Name = "SiteSettingSmtpFromDisplayName")]
        public string? SmtpFromDisplayName { get; set; }

        // ---------- SMS ----------
        [Display(Name = "SiteSettingSmsEnabled")]
        public bool SmsEnabled { get; set; }

        [Display(Name = "SiteSettingSmsProvider")]
        public string? SmsProvider { get; set; }

        [Display(Name = "SiteSettingSmsFromPhoneE164")]
        public string? SmsFromPhoneE164 { get; set; }

        [Display(Name = "SiteSettingSmsApiKey")]
        public string? SmsApiKey { get; set; }

        [Display(Name = "SiteSettingSmsApiSecret")]
        public string? SmsApiSecret { get; set; }

        [Display(Name = "SiteSettingSmsExtraSettingsJson")]
        public string? SmsExtraSettingsJson { get; set; }

        // ---------- Admin Routing ----------
        [Display(Name = "SiteSettingAdminAlertEmailsCsv")]
        public string? AdminAlertEmailsCsv { get; set; }

        [Display(Name = "SiteSettingAdminAlertSmsRecipientsCsv")]
        public string? AdminAlertSmsRecipientsCsv { get; set; }

        [Display(Name = "SiteSettingTransactionalEmailSubjectPrefix")]
        public string? TransactionalEmailSubjectPrefix { get; set; }

        [Display(Name = "SiteSettingCommunicationTestInboxEmail")]
        public string? CommunicationTestInboxEmail { get; set; }

        [Display(Name = "SiteSettingCommunicationTestSmsRecipientE164")]
        public string? CommunicationTestSmsRecipientE164 { get; set; }

        [Display(Name = "SiteSettingCommunicationTestWhatsAppRecipientE164")]
        public string? CommunicationTestWhatsAppRecipientE164 { get; set; }

        [Display(Name = "SiteSettingCommunicationTestEmailSubjectTemplate")]
        public string? CommunicationTestEmailSubjectTemplate { get; set; }

        [Display(Name = "SiteSettingCommunicationTestEmailBodyTemplate")]
        public string? CommunicationTestEmailBodyTemplate { get; set; }

        [Display(Name = "SiteSettingCommunicationTestSmsTemplate")]
        public string? CommunicationTestSmsTemplate { get; set; }

        [Display(Name = "SiteSettingCommunicationTestWhatsAppTemplate")]
        public string? CommunicationTestWhatsAppTemplate { get; set; }

        [Display(Name = "SiteSettingBusinessInvitationEmailSubjectTemplate")]
        public string? BusinessInvitationEmailSubjectTemplate { get; set; }

        [Display(Name = "SiteSettingBusinessInvitationEmailBodyTemplate")]
        public string? BusinessInvitationEmailBodyTemplate { get; set; }

        [Display(Name = "SiteSettingAccountActivationEmailSubjectTemplate")]
        public string? AccountActivationEmailSubjectTemplate { get; set; }

        [Display(Name = "SiteSettingAccountActivationEmailBodyTemplate")]
        public string? AccountActivationEmailBodyTemplate { get; set; }

        [Display(Name = "SiteSettingPasswordResetEmailSubjectTemplate")]
        public string? PasswordResetEmailSubjectTemplate { get; set; }

        [Display(Name = "SiteSettingPasswordResetEmailBodyTemplate")]
        public string? PasswordResetEmailBodyTemplate { get; set; }

        [Display(Name = "SiteSettingPhoneVerificationSmsTemplate")]
        public string? PhoneVerificationSmsTemplate { get; set; }

        [Display(Name = "SiteSettingPhoneVerificationWhatsAppTemplate")]
        public string? PhoneVerificationWhatsAppTemplate { get; set; }

        [Display(Name = "SiteSettingPhoneVerificationPreferredChannel")]
        public string? PhoneVerificationPreferredChannel { get; set; }

        [Display(Name = "SiteSettingPhoneVerificationAllowFallback")]
        public bool PhoneVerificationAllowFallback { get; set; }
    }
}
