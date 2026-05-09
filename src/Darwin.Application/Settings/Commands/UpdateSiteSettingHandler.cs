using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Settings.Commands
{
    /// <summary>
    /// Updates the singleton SiteSetting row. Enforces FluentValidation rules and
    /// optimistic concurrency via RowVersion. All fields are mapped explicitly.
    /// </summary>
    public sealed class UpdateSiteSettingHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<SiteSettingDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateSiteSettingHandler(
            IAppDbContext db,
            IValidator<SiteSettingDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Validates and persists the provided settings DTO into the single SiteSetting row.
        /// </summary>
        public async Task HandleAsync(SiteSettingDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var s = await _db.Set<SiteSetting>().FirstOrDefaultAsync(x => !x.IsDeleted, ct);
            if (s is null)
                throw new ValidationException(_localizer["SiteSettingRowNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
                throw new ValidationException(_localizer["RowVersionRequired"]);

            var currentRowVersion = s.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["SettingsModifiedByAnotherUser"]);

            // -------- Basics --------
            s.Title = dto.Title.Trim();
            s.LogoUrl = dto.LogoUrl;
            s.ContactEmail = dto.ContactEmail ?? string.Empty;

            // -------- Routing --------
            s.HomeSlug = dto.HomeSlug ?? SiteSettingDto.HomeSlugDefault;

            // -------- Localization --------
            s.DefaultCulture = dto.DefaultCulture.Trim();
            s.SupportedCulturesCsv = string.Join(",",
                (dto.SupportedCulturesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct());
            s.DefaultCountry = dto.DefaultCountry ?? SiteSettingDto.DefaultCountryDefault;
            s.DefaultCurrency = dto.DefaultCurrency;
            s.TimeZone = dto.TimeZone ?? SiteSettingDto.TimeZoneDefault;
            s.DateFormat = dto.DateFormat ?? SiteSettingDto.DateFormatDefault;
            s.TimeFormat = dto.TimeFormat ?? SiteSettingDto.TimeFormatDefault;
            s.AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson)
                ? null
                : dto.AdminTextOverridesJson.Trim();

            // -------- Security / JWT --------
            s.JwtEnabled = dto.JwtEnabled;
            s.JwtIssuer = dto.JwtIssuer.Trim();
            s.JwtAudience = dto.JwtAudience.Trim();
            s.JwtAccessTokenMinutes = dto.JwtAccessTokenMinutes;
            s.JwtRefreshTokenDays = dto.JwtRefreshTokenDays;
            s.JwtSigningKey = dto.JwtSigningKey;
            s.JwtPreviousSigningKey = dto.JwtPreviousSigningKey;
            s.JwtEmitScopes = dto.JwtEmitScopes;

            s.JwtSingleDeviceOnly = dto.JwtSingleDeviceOnly;
            s.JwtRequireDeviceBinding = dto.JwtRequireDeviceBinding;
            s.JwtClockSkewSeconds = dto.JwtClockSkewSeconds;


            // Mobile bootstrap
            s.MobileQrTokenRefreshSeconds = dto.MobileQrTokenRefreshSeconds;
            s.MobileMaxOutboxItems = dto.MobileMaxOutboxItems;
            s.BusinessManagementWebsiteUrl = dto.BusinessManagementWebsiteUrl;
            s.ImpressumUrl = dto.ImpressumUrl;
            s.PrivacyPolicyUrl = dto.PrivacyPolicyUrl;
            s.BusinessTermsUrl = dto.BusinessTermsUrl;
            s.AccountDeletionUrl = dto.AccountDeletionUrl;
            s.StripeEnabled = dto.StripeEnabled;
            s.StripePublishableKey = dto.StripePublishableKey;
            s.StripeSecretKey = dto.StripeSecretKey;
            s.StripeWebhookSecret = dto.StripeWebhookSecret;
            s.StripeMerchantDisplayName = dto.StripeMerchantDisplayName;
            s.VatEnabled = dto.VatEnabled;
            s.DefaultVatRatePercent = dto.DefaultVatRatePercent;
            s.PricesIncludeVat = dto.PricesIncludeVat;
            s.AllowReverseCharge = dto.AllowReverseCharge;
            s.InvoiceIssuerLegalName = dto.InvoiceIssuerLegalName;
            s.InvoiceIssuerTaxId = dto.InvoiceIssuerTaxId;
            s.InvoiceIssuerAddressLine1 = dto.InvoiceIssuerAddressLine1;
            s.InvoiceIssuerPostalCode = dto.InvoiceIssuerPostalCode;
            s.InvoiceIssuerCity = dto.InvoiceIssuerCity;
            s.InvoiceIssuerCountry = dto.InvoiceIssuerCountry;
            s.InvoiceArchiveRetentionYears = dto.InvoiceArchiveRetentionYears;
            s.DhlEnabled = dto.DhlEnabled;
            s.DhlEnvironment = dto.DhlEnvironment;
            s.DhlApiBaseUrl = dto.DhlApiBaseUrl;
            s.DhlApiKey = dto.DhlApiKey;
            s.DhlApiSecret = dto.DhlApiSecret;
            s.DhlAccountNumber = dto.DhlAccountNumber;
            s.DhlShipperName = dto.DhlShipperName;
            s.DhlShipperEmail = dto.DhlShipperEmail;
            s.DhlShipperPhoneE164 = dto.DhlShipperPhoneE164;
            s.DhlShipperStreet = dto.DhlShipperStreet;
            s.DhlShipperPostalCode = dto.DhlShipperPostalCode;
            s.DhlShipperCity = dto.DhlShipperCity;
            s.DhlShipperCountry = dto.DhlShipperCountry;
            s.ShipmentAttentionDelayHours = dto.ShipmentAttentionDelayHours;
            s.ShipmentTrackingGraceHours = dto.ShipmentTrackingGraceHours;


            // -------- Soft delete / data retention --------
            s.SoftDeleteCleanupEnabled = dto.SoftDeleteCleanupEnabled;
            s.SoftDeleteRetentionDays = dto.SoftDeleteRetentionDays;
            s.SoftDeleteCleanupBatchSize = dto.SoftDeleteCleanupBatchSize;

            // -------- Units & Formatting --------
            s.MeasurementSystem = dto.MeasurementSystem;
            s.DisplayWeightUnit = dto.DisplayWeightUnit ?? "kg";
            s.DisplayLengthUnit = dto.DisplayLengthUnit ?? "cm";
            s.MeasurementSettingsJson = dto.MeasurementSettingsJson;
            s.NumberFormattingOverridesJson = dto.NumberFormattingOverridesJson;

            // -------- SEO --------
            s.EnableCanonical = dto.EnableCanonical;
            s.HreflangEnabled = dto.HreflangEnabled;
            s.SeoTitleTemplate = dto.SeoTitleTemplate;
            s.SeoMetaDescriptionTemplate = dto.SeoMetaDescriptionTemplate;
            s.OpenGraphDefaultsJson = dto.OpenGraphDefaultsJson;

            // -------- Analytics --------
            s.GoogleAnalyticsId = dto.GoogleAnalyticsId;
            s.GoogleTagManagerId = dto.GoogleTagManagerId;
            s.GoogleSearchConsoleVerification = dto.GoogleSearchConsoleVerification;

            // -------- Feature flags & WhatsApp --------
            s.FeatureFlagsJson = dto.FeatureFlagsJson;
            s.WhatsAppEnabled = dto.WhatsAppEnabled;
            s.WhatsAppBusinessPhoneId = dto.WhatsAppBusinessPhoneId;
            s.WhatsAppAccessToken = dto.WhatsAppAccessToken;
            s.WhatsAppFromPhoneE164 = dto.WhatsAppFromPhoneE164;
            s.WhatsAppAdminRecipientsCsv = dto.WhatsAppAdminRecipientsCsv;

            // -------- WebAuthn --------
            s.WebAuthnRelyingPartyId = dto.WebAuthnRelyingPartyId?.Trim() ?? "localhost";
            s.WebAuthnRelyingPartyName = dto.WebAuthnRelyingPartyName?.Trim() ?? "Darwin";
            s.WebAuthnAllowedOriginsCsv = string.Join(",",
                (dto.WebAuthnAllowedOriginsCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct());
            s.WebAuthnRequireUserVerification = dto.WebAuthnRequireUserVerification;

            // -------- Email (SMTP) --------
            s.SmtpEnabled = dto.SmtpEnabled;
            s.SmtpHost = dto.SmtpHost;
            s.SmtpPort = dto.SmtpPort;
            s.SmtpEnableSsl = dto.SmtpEnableSsl;
            s.SmtpUsername = dto.SmtpUsername;
            s.SmtpPassword = dto.SmtpPassword;
            s.SmtpFromAddress = dto.SmtpFromAddress;
            s.SmtpFromDisplayName = dto.SmtpFromDisplayName;

            // -------- SMS --------
            s.SmsEnabled = dto.SmsEnabled;
            s.SmsProvider = dto.SmsProvider;
            s.SmsFromPhoneE164 = dto.SmsFromPhoneE164;
            s.SmsApiKey = dto.SmsApiKey;
            s.SmsApiSecret = dto.SmsApiSecret;
            s.SmsExtraSettingsJson = dto.SmsExtraSettingsJson;

            // -------- Admin routing --------
            s.AdminAlertEmailsCsv = dto.AdminAlertEmailsCsv;
            s.AdminAlertSmsRecipientsCsv = dto.AdminAlertSmsRecipientsCsv;
            s.TransactionalEmailSubjectPrefix = dto.TransactionalEmailSubjectPrefix;
            s.CommunicationTestInboxEmail = dto.CommunicationTestInboxEmail;
            s.CommunicationTestSmsRecipientE164 = dto.CommunicationTestSmsRecipientE164;
            s.CommunicationTestWhatsAppRecipientE164 = dto.CommunicationTestWhatsAppRecipientE164;
            s.CommunicationTestEmailSubjectTemplate = dto.CommunicationTestEmailSubjectTemplate;
            s.CommunicationTestEmailBodyTemplate = dto.CommunicationTestEmailBodyTemplate;
            s.CommunicationTestSmsTemplate = dto.CommunicationTestSmsTemplate;
            s.CommunicationTestWhatsAppTemplate = dto.CommunicationTestWhatsAppTemplate;
            s.BusinessInvitationEmailSubjectTemplate = dto.BusinessInvitationEmailSubjectTemplate;
            s.BusinessInvitationEmailBodyTemplate = dto.BusinessInvitationEmailBodyTemplate;
            s.AccountActivationEmailSubjectTemplate = dto.AccountActivationEmailSubjectTemplate;
            s.AccountActivationEmailBodyTemplate = dto.AccountActivationEmailBodyTemplate;
            s.PasswordResetEmailSubjectTemplate = dto.PasswordResetEmailSubjectTemplate;
            s.PasswordResetEmailBodyTemplate = dto.PasswordResetEmailBodyTemplate;
            s.PhoneVerificationSmsTemplate = dto.PhoneVerificationSmsTemplate;
            s.PhoneVerificationWhatsAppTemplate = dto.PhoneVerificationWhatsAppTemplate;
            s.PhoneVerificationPreferredChannel = dto.PhoneVerificationPreferredChannel;
            s.PhoneVerificationAllowFallback = dto.PhoneVerificationAllowFallback;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["SettingsModifiedByAnotherUser"]);
            }
        }
    }
}
