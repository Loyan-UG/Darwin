using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Settings.Queries
{
    /// <summary>
    /// Returns the singleton SiteSetting as a <see cref="SiteSettingDto"/> including all fields.
    /// </summary>
    public sealed class GetSiteSettingHandler
    {
        private readonly IAppDbContext _db;
        public GetSiteSettingHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Loads the single settings row (read-only) or returns null if missing.
        /// </summary>
        public async Task<SiteSettingDto?> HandleAsync(CancellationToken ct = default)
        {
            var s = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
            if (s == null) return null;

            return new SiteSettingDto
            {
                Id = s.Id,
                RowVersion = s.RowVersion,

                // Basics
                Title = s.Title,
                LogoUrl = s.LogoUrl,
                ContactEmail = s.ContactEmail,

                // Routing
                HomeSlug = s.HomeSlug,

                // Localization
                DefaultCulture = s.DefaultCulture,
                SupportedCulturesCsv = s.SupportedCulturesCsv,
                DefaultCountry = s.DefaultCountry,
                DefaultCurrency = s.DefaultCurrency,
                TimeZone = s.TimeZone,
                DateFormat = s.DateFormat,
                TimeFormat = s.TimeFormat,

                // Units & formatting
                MeasurementSystem = s.MeasurementSystem,
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

                // Feature flags
                FeatureFlagsJson = s.FeatureFlagsJson,

                // WhatsApp
                WhatsAppEnabled = s.WhatsAppEnabled,
                WhatsAppBusinessPhoneId = s.WhatsAppBusinessPhoneId,
                WhatsAppAccessToken = s.WhatsAppAccessToken,
                WhatsAppFromPhoneE164 = s.WhatsAppFromPhoneE164,
                WhatsAppAdminRecipientsCsv = s.WhatsAppAdminRecipientsCsv,

                // WebAuthn
                WebAuthnRelyingPartyId = s.WebAuthnRelyingPartyId,
                WebAuthnRelyingPartyName = s.WebAuthnRelyingPartyName,
                WebAuthnAllowedOriginsCsv = s.WebAuthnAllowedOriginsCsv,
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
                AdminAlertSmsRecipientsCsv = s.AdminAlertSmsRecipientsCsv
            };
        }
    }
}
