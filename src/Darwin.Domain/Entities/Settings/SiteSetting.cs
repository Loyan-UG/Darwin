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
        public string DefaultCulture { get; set; } = DomainDefaults.DefaultCulture;
        public string SupportedCulturesCsv { get; set; } = DomainDefaults.SupportedCulturesCsv; // comma-separated
        /// <summary>Default ISO 3166-1 alpha-2 country code, e.g., "DE".</summary>
        public string DefaultCountry { get; set; } = DomainDefaults.DefaultCountryCode;
        /// <summary>Default ISO 4217 currency code (e.g., "EUR").</summary>
        public string DefaultCurrency { get; set; } = DomainDefaults.DefaultCurrency;
        /// <summary>Application time zone identifier used for display (storage is UTC).</summary>
        public string TimeZone { get; set; } = DomainDefaults.DefaultTimezone;
        /// <summary>Date and time formatting patterns for display.</summary>
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        public string TimeFormat { get; set; } = "HH:mm";
        /// <summary>
        /// Optional JSON map of admin UI text overrides layered on top of shared resx resources.
        /// Format: { "de-DE": { "SomeKey": "..." }, "en-US": { "SomeKey": "..." } }.
        /// Intended for platform-level wording adjustments without changing source resource files.
        /// </summary>
        public string? AdminTextOverridesJson { get; set; }



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

        /// <summary>
        /// When true, only a single active device/session is allowed per user.
        /// New logins will revoke previously issued refresh tokens for that user.
        /// </summary>
        public bool JwtSingleDeviceOnly { get; set; } = false;

        /// <summary>
        /// When true, refresh tokens are bound to a device identifier (e.g., mobile installation id).
        /// The token purpose will be persisted as "JwtRefresh:{deviceId}" and validation requires matching device.
        /// </summary>
        public bool JwtRequireDeviceBinding { get; set; } = true;

        /// <summary>
        /// Allowed clock skew in seconds for token validation (used by API during JWT validation).
        /// </summary>
        public int JwtClockSkewSeconds { get; set; } = 120;



        // Mobile app bootstrap
        /// <summary>
        /// Refresh cadence (in seconds) for QR token refresh logic used by mobile clients.
        /// This value is returned by the App Bootstrap endpoint and allows changing client behavior
        /// without publishing a new mobile version.
        /// </summary>
        public int MobileQrTokenRefreshSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of outbox items that a mobile client should keep locally before forcing a flush.
        /// This value is returned by the App Bootstrap endpoint and allows server-side tuning of client sync behavior.
        /// </summary>
        public int MobileMaxOutboxItems { get; set; } = 200;

        /// <summary>
        /// Absolute HTTPS website URL where business users manage subscription, billing, and plan changes outside the mobile app.
        /// </summary>
        public string? BusinessManagementWebsiteUrl { get; set; }

        /// <summary>
        /// Absolute HTTPS URL of the impressum page used by business-facing apps.
        /// </summary>
        public string? ImpressumUrl { get; set; }

        /// <summary>
        /// Absolute HTTPS URL of the privacy policy page used by business-facing apps.
        /// </summary>
        public string? PrivacyPolicyUrl { get; set; }

        /// <summary>
        /// Absolute HTTPS URL of the business terms page used by business-facing apps.
        /// </summary>
        public string? BusinessTermsUrl { get; set; }

        /// <summary>
        /// Absolute HTTPS URL of the external account-deletion handoff page used by business-facing apps.
        /// </summary>
        public string? AccountDeletionUrl { get; set; }


        // Phase-1 payment provider (Stripe-first)
        /// <summary>Enable Stripe-backed payment operations as the primary phase-1 provider.</summary>
        public bool StripeEnabled { get; set; } = false;
        /// <summary>Stripe publishable key used by web/mobile clients where applicable.</summary>
        public string? StripePublishableKey { get; set; }
        /// <summary>Stripe secret key used by server-side payment operations.</summary>
        public string? StripeSecretKey { get; set; }
        /// <summary>Webhook signing secret used to verify Stripe callbacks.</summary>
        public string? StripeWebhookSecret { get; set; }
        /// <summary>Human-facing merchant label shown in hosted payment surfaces.</summary>
        public string? StripeMerchantDisplayName { get; set; }

        // Phase-1 tax / VAT foundation
        /// <summary>Enable VAT-aware operational support surfaces in admin and billing workbenches.</summary>
        public bool VatEnabled { get; set; } = true;
        /// <summary>Default VAT rate percent applied when no business-specific tax policy exists yet.</summary>
        public decimal DefaultVatRatePercent { get; set; } = 19m;
        /// <summary>Indicates whether listed prices are interpreted as VAT-inclusive by default.</summary>
        public bool PricesIncludeVat { get; set; } = true;
        /// <summary>Allows reverse-charge handling to be configured for B2B invoicing support paths.</summary>
        public bool AllowReverseCharge { get; set; } = false;
        /// <summary>Legal issuer name used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerLegalName { get; set; }
        /// <summary>Issuer tax/VAT identifier used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerTaxId { get; set; }
        /// <summary>Issuer address line used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerAddressLine1 { get; set; }
        /// <summary>Issuer postal code used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerPostalCode { get; set; }
        /// <summary>Issuer city used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerCity { get; set; }
        /// <summary>Issuer country code used in phase-1 invoice/compliance support surfaces.</summary>
        public string? InvoiceIssuerCountry { get; set; }
        /// <summary>Number of years issued invoice archive metadata must be retained.</summary>
        public int InvoiceArchiveRetentionYears { get; set; } = 10;

        // Phase-1 shipping provider (DHL-first)
        /// <summary>Enable DHL-backed shipment operations as the primary phase-1 carrier.</summary>
        public bool DhlEnabled { get; set; } = false;
        /// <summary>Environment label for DHL integration (for example Sandbox or Production).</summary>
        public string? DhlEnvironment { get; set; }
        /// <summary>Absolute HTTPS API base URL for the DHL integration endpoint.</summary>
        public string? DhlApiBaseUrl { get; set; }
        /// <summary>DHL API key or client identifier.</summary>
        public string? DhlApiKey { get; set; }
        /// <summary>DHL API secret/password.</summary>
        public string? DhlApiSecret { get; set; }
        /// <summary>DHL shipper account number / EKP.</summary>
        public string? DhlAccountNumber { get; set; }
        /// <summary>Default shipper legal/display name used on labels.</summary>
        public string? DhlShipperName { get; set; }
        /// <summary>Default shipper contact email used on labels/support payloads.</summary>
        public string? DhlShipperEmail { get; set; }
        /// <summary>Default shipper contact phone in E.164 format.</summary>
        public string? DhlShipperPhoneE164 { get; set; }
        /// <summary>Default shipper street line used on labels.</summary>
        public string? DhlShipperStreet { get; set; }
        /// <summary>Default shipper postal code used on labels.</summary>
        public string? DhlShipperPostalCode { get; set; }
        /// <summary>Default shipper city used on labels.</summary>
        public string? DhlShipperCity { get; set; }
        /// <summary>Default shipper country code used on labels.</summary>
        public string? DhlShipperCountry { get; set; }
        /// <summary>Number of hours a pending/packed shipment may remain without handoff before it should appear in the attention queue.</summary>
        public int ShipmentAttentionDelayHours { get; set; } = 24;
        /// <summary>Number of hours after shipment creation before missing tracking should be treated as overdue in support queues.</summary>
        public int ShipmentTrackingGraceHours { get; set; } = 12;




        // Data retention / soft-delete cleanup

        /// <summary>
        /// Enables periodic hard-deletion of entities that have been soft-deleted for longer
        /// than the configured retention window. When disabled, soft-deleted rows are never
        /// hard-deleted automatically by background jobs.
        /// </summary>
        public bool SoftDeleteCleanupEnabled { get; set; } = true;

        /// <summary>
        /// Number of days a soft-deleted entity should be retained before it becomes eligible
        /// for hard deletion. The cleanup worker will typically compare this value against
        /// the ModifiedAtUtc timestamp of entities where IsDeleted == true.
        /// </summary>
        public int SoftDeleteRetentionDays { get; set; } = 90;

        /// <summary>
        /// Maximum number of entities to hard-delete in a single cleanup run. This protects
        /// the database from excessively large delete batches and spreads the work across
        /// multiple executions of the background worker.
        /// </summary>
        public int SoftDeleteCleanupBatchSize { get; set; } = 500;




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

        /// <summary>Optional subject prefix added to phase-1 transactional emails for environment/ops signaling.</summary>
        public string? TransactionalEmailSubjectPrefix { get; set; }

        /// <summary>Optional override inbox used to reroute transactional emails for testing or staged go-live verification.</summary>
        public string? CommunicationTestInboxEmail { get; set; }
        /// <summary>Optional SMS test recipient used for operator-side transport validation in phase-1 support workflows.</summary>
        public string? CommunicationTestSmsRecipientE164 { get; set; }
        /// <summary>Optional WhatsApp test recipient used for operator-side transport validation in phase-1 support workflows.</summary>
        public string? CommunicationTestWhatsAppRecipientE164 { get; set; }
        /// <summary>Optional subject template for operator-side diagnostic email tests. Supports placeholders such as {requested_by}, {attempted_at_utc}, and {test_target}.</summary>
        public string? CommunicationTestEmailSubjectTemplate { get; set; }
        /// <summary>Optional HTML body template for operator-side diagnostic email tests. Supports placeholders such as {requested_by}, {attempted_at_utc}, {transport_state}, and {test_target}.</summary>
        public string? CommunicationTestEmailBodyTemplate { get; set; }
        /// <summary>Optional plain-text template for operator-side diagnostic SMS tests. Supports placeholders such as {requested_by}, {attempted_at_utc}, and {test_target}.</summary>
        public string? CommunicationTestSmsTemplate { get; set; }
        /// <summary>Optional plain-text template for operator-side diagnostic WhatsApp tests. Supports placeholders such as {requested_by}, {attempted_at_utc}, and {test_target}.</summary>
        public string? CommunicationTestWhatsAppTemplate { get; set; }

        /// <summary>Optional subject template for business invitations. Supports placeholders such as {business_name}, {role}, and {invitation_action}.</summary>
        public string? BusinessInvitationEmailSubjectTemplate { get; set; }

        /// <summary>Optional HTML body template for business invitations. Supports placeholders such as {business_name}, {role}, {token}, {expires_at_utc}, and {acceptance_link_html}.</summary>
        public string? BusinessInvitationEmailBodyTemplate { get; set; }

        /// <summary>Optional subject template for account activation emails. Supports placeholders such as {email} and {expires_at_utc}.</summary>
        public string? AccountActivationEmailSubjectTemplate { get; set; }

        /// <summary>Optional HTML body template for account activation emails. Supports placeholders such as {email}, {token}, and {expires_at_utc}.</summary>
        public string? AccountActivationEmailBodyTemplate { get; set; }

        /// <summary>Optional subject template for password-reset emails. Supports placeholders such as {email} and {expires_at_utc}.</summary>
        public string? PasswordResetEmailSubjectTemplate { get; set; }

        /// <summary>Optional HTML body template for password-reset emails. Supports placeholders such as {email}, {token}, and {expires_at_utc}.</summary>
        public string? PasswordResetEmailBodyTemplate { get; set; }

        /// <summary>Optional plain-text template for SMS-based phone verification. Supports placeholders such as {phone_e164}, {token}, and {expires_at_utc}.</summary>
        public string? PhoneVerificationSmsTemplate { get; set; }

        /// <summary>Optional plain-text template for WhatsApp-based phone verification. Supports placeholders such as {phone_e164}, {token}, and {expires_at_utc}.</summary>
        public string? PhoneVerificationWhatsAppTemplate { get; set; }

        /// <summary>Preferred delivery channel for current-user phone verification. Supported values: Sms, WhatsApp.</summary>
        public string? PhoneVerificationPreferredChannel { get; set; }

        /// <summary>When enabled, phone verification may fall back to the other supported channel if the requested or preferred channel is unavailable.</summary>
        public bool PhoneVerificationAllowFallback { get; set; }

    }
}
