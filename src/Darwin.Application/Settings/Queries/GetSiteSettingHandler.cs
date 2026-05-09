using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Settings.Queries
{
    /// <summary>
    /// Returns the singleton <see cref="SiteSetting"/> as a <see cref="SiteSettingDto"/> including all fields.
    /// </summary>
    public sealed class GetSiteSettingHandler
    {
        private readonly IAppDbContext _db;

        public GetSiteSettingHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Loads the single settings row (read-only) or returns null if missing.
        /// </summary>
        public async Task<SiteSettingDto?> HandleAsync(CancellationToken ct = default)
        {
            var s = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct);

            if (s is null) return null;

            return new SiteSettingDto
            {
                Id = s.Id,
                RowVersion = s.RowVersion ?? System.Array.Empty<byte>(),

                // Basics
                Title = s.Title ?? string.Empty,
                LogoUrl = s.LogoUrl,
                ContactEmail = s.ContactEmail,

                // Routing
                HomeSlug = s.HomeSlug ?? SiteSettingDto.HomeSlugDefault,

                // Localization
                DefaultCulture = s.DefaultCulture ?? SiteSettingDto.DefaultCultureDefault,
                SupportedCulturesCsv = s.SupportedCulturesCsv ?? SiteSettingDto.SupportedCulturesCsvDefault,
                DefaultCountry = s.DefaultCountry ?? SiteSettingDto.DefaultCountryDefault,
                DefaultCurrency = s.DefaultCurrency ?? SiteSettingDto.DefaultCurrencyDefault,
                TimeZone = s.TimeZone ?? SiteSettingDto.TimeZoneDefault,
                DateFormat = s.DateFormat ?? SiteSettingDto.DateFormatDefault,
                TimeFormat = s.TimeFormat ?? SiteSettingDto.TimeFormatDefault,
                AdminTextOverridesJson = s.AdminTextOverridesJson,

                // JWT
                JwtEnabled = s.JwtEnabled,
                JwtIssuer = s.JwtIssuer ?? "Darwin",
                JwtAudience = s.JwtAudience ?? "Darwin.PublicApi",
                JwtAccessTokenMinutes = s.JwtAccessTokenMinutes,
                JwtRefreshTokenDays = s.JwtRefreshTokenDays,
                JwtSigningKey = s.JwtSigningKey ?? string.Empty,
                JwtPreviousSigningKey = s.JwtPreviousSigningKey,
                JwtEmitScopes = s.JwtEmitScopes,
                JwtSingleDeviceOnly = s.JwtSingleDeviceOnly,
                JwtRequireDeviceBinding = s.JwtRequireDeviceBinding,
                JwtClockSkewSeconds = s.JwtClockSkewSeconds,

                // Mobile bootstrap
                MobileQrTokenRefreshSeconds = s.MobileQrTokenRefreshSeconds,
                MobileMaxOutboxItems = s.MobileMaxOutboxItems,
                BusinessManagementWebsiteUrl = s.BusinessManagementWebsiteUrl,
                ImpressumUrl = s.ImpressumUrl,
                PrivacyPolicyUrl = s.PrivacyPolicyUrl,
                BusinessTermsUrl = s.BusinessTermsUrl,
                AccountDeletionUrl = s.AccountDeletionUrl,
                StripeEnabled = s.StripeEnabled,
                StripePublishableKey = s.StripePublishableKey,
                StripeSecretKey = s.StripeSecretKey,
                StripeWebhookSecret = s.StripeWebhookSecret,
                StripeMerchantDisplayName = s.StripeMerchantDisplayName,
                VatEnabled = s.VatEnabled,
                DefaultVatRatePercent = s.DefaultVatRatePercent,
                PricesIncludeVat = s.PricesIncludeVat,
                AllowReverseCharge = s.AllowReverseCharge,
                InvoiceIssuerLegalName = s.InvoiceIssuerLegalName,
                InvoiceIssuerTaxId = s.InvoiceIssuerTaxId,
                InvoiceIssuerAddressLine1 = s.InvoiceIssuerAddressLine1,
                InvoiceIssuerPostalCode = s.InvoiceIssuerPostalCode,
                InvoiceIssuerCity = s.InvoiceIssuerCity,
                InvoiceIssuerCountry = s.InvoiceIssuerCountry,
                InvoiceArchiveRetentionYears = s.InvoiceArchiveRetentionYears,
                DhlEnabled = s.DhlEnabled,
                DhlEnvironment = s.DhlEnvironment,
                DhlApiBaseUrl = s.DhlApiBaseUrl,
                DhlApiKey = s.DhlApiKey,
                DhlApiSecret = s.DhlApiSecret,
                DhlAccountNumber = s.DhlAccountNumber,
                DhlShipperName = s.DhlShipperName,
                DhlShipperEmail = s.DhlShipperEmail,
                DhlShipperPhoneE164 = s.DhlShipperPhoneE164,
                DhlShipperStreet = s.DhlShipperStreet,
                DhlShipperPostalCode = s.DhlShipperPostalCode,
                DhlShipperCity = s.DhlShipperCity,
                DhlShipperCountry = s.DhlShipperCountry,
                ShipmentAttentionDelayHours = s.ShipmentAttentionDelayHours,
                ShipmentTrackingGraceHours = s.ShipmentTrackingGraceHours,


                // Soft delete
                SoftDeleteCleanupEnabled = s.SoftDeleteCleanupEnabled,
                SoftDeleteRetentionDays = s.SoftDeleteRetentionDays,
                SoftDeleteCleanupBatchSize = s.SoftDeleteCleanupBatchSize,

                // Measurement
                MeasurementSystem = s.MeasurementSystem ?? "Metric",
                DisplayWeightUnit = s.DisplayWeightUnit,
                DisplayLengthUnit = s.DisplayLengthUnit,
                MeasurementSettingsJson = s.MeasurementSettingsJson,
                NumberFormattingOverridesJson = s.NumberFormattingOverridesJson,

                // SEO
                EnableCanonical = s.EnableCanonical,
                HreflangEnabled = s.HreflangEnabled,
                SeoTitleTemplate = s.SeoTitleTemplate,
                SeoMetaDescriptionTemplate = s.SeoMetaDescriptionTemplate,
                OpenGraphDefaultsJson = s.OpenGraphDefaultsJson,

                // Analytics
                GoogleAnalyticsId = s.GoogleAnalyticsId,
                GoogleTagManagerId = s.GoogleTagManagerId,
                GoogleSearchConsoleVerification = s.GoogleSearchConsoleVerification,

                // Feature flags & WhatsApp
                FeatureFlagsJson = s.FeatureFlagsJson,
                WhatsAppEnabled = s.WhatsAppEnabled,
                WhatsAppBusinessPhoneId = s.WhatsAppBusinessPhoneId,
                WhatsAppAccessToken = s.WhatsAppAccessToken,
                WhatsAppFromPhoneE164 = s.WhatsAppFromPhoneE164,
                WhatsAppAdminRecipientsCsv = s.WhatsAppAdminRecipientsCsv,

                // WebAuthn
                WebAuthnRelyingPartyId = s.WebAuthnRelyingPartyId ?? "localhost",
                WebAuthnRelyingPartyName = s.WebAuthnRelyingPartyName ?? "Darwin",
                WebAuthnAllowedOriginsCsv = s.WebAuthnAllowedOriginsCsv ?? "https://localhost:5001",
                WebAuthnRequireUserVerification = s.WebAuthnRequireUserVerification,

                // SMTP
                SmtpEnabled = s.SmtpEnabled,
                SmtpHost = s.SmtpHost,
                SmtpPort = s.SmtpPort,
                SmtpEnableSsl = s.SmtpEnableSsl,
                SmtpUsername = s.SmtpUsername,
                SmtpPassword = s.SmtpPassword,
                SmtpFromAddress = s.SmtpFromAddress,
                SmtpFromDisplayName = s.SmtpFromDisplayName,

                // SMS
                SmsEnabled = s.SmsEnabled,
                SmsProvider = s.SmsProvider,
                SmsFromPhoneE164 = s.SmsFromPhoneE164,
                SmsApiKey = s.SmsApiKey,
                SmsApiSecret = s.SmsApiSecret,
                SmsExtraSettingsJson = s.SmsExtraSettingsJson,

                // Admin routing
                AdminAlertEmailsCsv = s.AdminAlertEmailsCsv,
                AdminAlertSmsRecipientsCsv = s.AdminAlertSmsRecipientsCsv,
                TransactionalEmailSubjectPrefix = s.TransactionalEmailSubjectPrefix,
                CommunicationTestInboxEmail = s.CommunicationTestInboxEmail,
                CommunicationTestSmsRecipientE164 = s.CommunicationTestSmsRecipientE164,
                CommunicationTestWhatsAppRecipientE164 = s.CommunicationTestWhatsAppRecipientE164,
                CommunicationTestEmailSubjectTemplate = s.CommunicationTestEmailSubjectTemplate,
                CommunicationTestEmailBodyTemplate = s.CommunicationTestEmailBodyTemplate,
                CommunicationTestSmsTemplate = s.CommunicationTestSmsTemplate,
                CommunicationTestWhatsAppTemplate = s.CommunicationTestWhatsAppTemplate,
                BusinessInvitationEmailSubjectTemplate = s.BusinessInvitationEmailSubjectTemplate,
                BusinessInvitationEmailBodyTemplate = s.BusinessInvitationEmailBodyTemplate,
                AccountActivationEmailSubjectTemplate = s.AccountActivationEmailSubjectTemplate,
                AccountActivationEmailBodyTemplate = s.AccountActivationEmailBodyTemplate,
                PasswordResetEmailSubjectTemplate = s.PasswordResetEmailSubjectTemplate,
                PasswordResetEmailBodyTemplate = s.PasswordResetEmailBodyTemplate,
                PhoneVerificationSmsTemplate = s.PhoneVerificationSmsTemplate,
                PhoneVerificationWhatsAppTemplate = s.PhoneVerificationWhatsAppTemplate,
                PhoneVerificationPreferredChannel = s.PhoneVerificationPreferredChannel,
                PhoneVerificationAllowFallback = s.PhoneVerificationAllowFallback
            };
        }
    }
}
