using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Darwin.Web.Services.Settings
{
    /// <summary>
    /// Default in-memory implementation of <see cref="ISiteSettingCache"/>.
    /// </summary>
    public sealed class SiteSettingCache : ISiteSettingCache
    {
        private readonly IMemoryCache _cache;
        private readonly IAppDbContext _db;
        private const string CacheKey = "SiteSettingCache:Current";

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteSettingCache"/> class.
        /// </summary>
        public SiteSettingCache(IMemoryCache cache, IAppDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        /// <inheritdoc />
        public async Task<SiteSettingDto> GetAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out SiteSettingDto cached))
                return cached;

            // Single-row table - enforce via migration/seeder.
            var entity = await _db.Set<SiteSetting>()
                                  .AsNoTracking()
                                  .SingleAsync(ct);

            var dto = Map(entity);

            _cache.Set(CacheKey, dto, new MemoryCacheEntryOptions
            {
                SlidingExpiration = System.TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            });

            return dto;
        }

        /// <inheritdoc />
        public void Invalidate() => _cache.Remove(CacheKey);

        /// <summary>
        /// Maps the persistence entity to a DTO. Keep this mapping in sync with
        /// <see cref="SiteSettingDto"/> (Application layer). Update whenever fields change.
        /// </summary>
        private static SiteSettingDto Map(SiteSetting s)
        {
            return new SiteSettingDto
            {
                Id = s.Id,
                RowVersion = s.RowVersion,

                // Basic
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

                // Units / formatting
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

                // Admin notification defaults
                AdminAlertEmailsCsv = s.AdminAlertEmailsCsv,
                AdminAlertSmsRecipientsCsv = s.AdminAlertSmsRecipientsCsv
            };
        }
    }
}
