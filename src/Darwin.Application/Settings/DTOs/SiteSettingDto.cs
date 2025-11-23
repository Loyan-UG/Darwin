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

        // -------- Basic site information --------
        public string Title { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ContactEmail { get; set; }

        // -------- Routing --------
        public string? HomeSlug { get; set; } = "home";

        // -------- Localization settings --------
        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";
        public string? DefaultCountry { get; set; } = "DE";
        public string DefaultCurrency { get; set; } = "EUR";
        public string? TimeZone { get; set; } = "Europe/Berlin";
        public string? DateFormat { get; set; } = "yyyy-MM-dd";
        public string? TimeFormat { get; set; } = "HH:mm";

        // -------- JWT / Access-Refresh token settings --------

        /// <summary>
        /// Enables issuing and accepting JWT access/refresh tokens for APIs/mobile apps.
        /// When disabled, the issuing side should refuse token issuance.
        /// </summary>
        public bool JwtEnabled { get; set; } = true;

        /// <summary>
        /// JWT issuer (iss claim). Must match validation configuration.
        /// </summary>
        public string JwtIssuer { get; set; } = "Darwin";

        /// <summary>
        /// JWT audience (aud claim). Must match validation configuration.
        /// </summary>
        public string JwtAudience { get; set; } = "Darwin.PublicApi";

        /// <summary>
        /// Access token lifetime in minutes.
        /// </summary>
        public int JwtAccessTokenMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh token lifetime in days.
        /// </summary>
        public int JwtRefreshTokenDays { get; set; } = 30;

        /// <summary>
        /// Current symmetric signing key used to sign access tokens.
        /// Must be high-entropy in production.
        /// </summary>
        public string JwtSigningKey { get; set; } = string.Empty;

        /// <summary>
        /// Previous signing key used for key rotation. Optional.
        /// When set, validators should accept tokens signed with either key.
        /// </summary>
        public string? JwtPreviousSigningKey { get; set; }

        /// <summary>
        /// When true, access tokens emit user scopes/permissions claims.
        /// </summary>
        public bool JwtEmitScopes { get; set; } = false;

        /// <summary>
        /// When true, only a single active device/session is allowed per user.
        /// </summary>
        public bool JwtSingleDeviceOnly { get; set; } = false;

        /// <summary>
        /// When true, refresh tokens are bound to a device identifier.
        /// </summary>
        public bool JwtRequireDeviceBinding { get; set; } = true;

        /// <summary>
        /// Allowed clock skew in seconds for JWT validation.
        /// </summary>
        public int JwtClockSkewSeconds { get; set; } = 60;

        // -------- Soft delete / data retention --------
        public bool SoftDeleteCleanupEnabled { get; set; } = true;
        public int SoftDeleteRetentionDays { get; set; } = 90;
        public int SoftDeleteCleanupBatchSize { get; set; } = 500;

        // -------- Measurement & Units --------
        public string MeasurementSystem { get; set; } = "Metric";
        public string? DisplayWeightUnit { get; set; } = "kg";
        public string? DisplayLengthUnit { get; set; } = "cm";
        public string? MeasurementSettingsJson { get; set; }
        public string? NumberFormattingOverridesJson { get; set; }

        // -------- SEO / URLs --------
        public bool EnableCanonical { get; set; } = true;
        public bool HreflangEnabled { get; set; } = true;
        public string? SeoTitleTemplate { get; set; }
        public string? SeoMetaDescriptionTemplate { get; set; }
        public string? OpenGraphDefaultsJson { get; set; }

        // -------- Analytics --------
        public string? GoogleAnalyticsId { get; set; }
        public string? GoogleTagManagerId { get; set; }
        public string? GoogleSearchConsoleVerification { get; set; }

        // -------- Feature flags & WhatsApp --------
        public string? FeatureFlagsJson { get; set; }
        public bool WhatsAppEnabled { get; set; } = false;
        public string? WhatsAppBusinessPhoneId { get; set; }
        public string? WhatsAppAccessToken { get; set; }
        public string? WhatsAppFromPhoneE164 { get; set; }
        public string? WhatsAppAdminRecipientsCsv { get; set; }

        // -------- WebAuthn (Passkeys) --------
        public string WebAuthnRelyingPartyId { get; set; } = "localhost";
        public string WebAuthnRelyingPartyName { get; set; } = "Darwin";
        public string WebAuthnAllowedOriginsCsv { get; set; } = "https://localhost:5001";
        public bool WebAuthnRequireUserVerification { get; set; } = false;

        // -------- Email (SMTP) --------
        public bool SmtpEnabled { get; set; } = false;
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public bool SmtpEnableSsl { get; set; } = true;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public string? SmtpFromAddress { get; set; }
        public string? SmtpFromDisplayName { get; set; }

        // -------- SMS --------
        public bool SmsEnabled { get; set; } = false;
        public string? SmsProvider { get; set; }
        public string? SmsFromPhoneE164 { get; set; }
        public string? SmsApiKey { get; set; }
        public string? SmsApiSecret { get; set; }
        public string? SmsExtraSettingsJson { get; set; }

        // -------- Admin routing --------
        public string? AdminAlertEmailsCsv { get; set; }
        public string? AdminAlertSmsRecipientsCsv { get; set; }
    }
}
