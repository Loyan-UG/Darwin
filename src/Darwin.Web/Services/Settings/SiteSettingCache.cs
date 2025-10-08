using System;
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
    /// Default implementation of <see cref="ISiteSettingCache"/> backed by <see cref="IMemoryCache"/>.
    /// Lazily loads the single SiteSetting record and caches it to avoid repeated database queries.
    /// Thread-safe retrieval; explicit invalidation after Admin saves.
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
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Gets the current site settings with caching semantics. If not cached, loads from DB and caches the DTO.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Mapped <see cref="SiteSettingDto"/>.</returns>
        public async Task<SiteSettingDto> GetAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out SiteSettingDto? cached) && cached is not null)
            {
                return cached;
            }

            // Load the single SiteSetting entity. Database schema enforces single row.
            var entity = await _db.Set<SiteSetting>()
                                  .AsNoTracking()
                                  .SingleAsync(ct)
                                  .ConfigureAwait(false);

            var dto = Map(entity);

            // Cache with sliding expiration. Adjust as needed.
            _cache.Set(CacheKey, dto, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            });

            return dto;
        }

        /// <summary>
        /// Invalidates the in-memory cache so that the next read hits the database.
        /// </summary>
        public void Invalidate()
        {
            _cache.Remove(CacheKey);
        }

        /// <summary>
        /// Maps the persistence entity to a DTO. Keep this mapping in sync with <see cref="SiteSettingDto"/>.
        /// Update whenever new properties are introduced on either side.
        /// </summary>
        private static SiteSettingDto Map(SiteSetting s)
        {
            // Defensive: handle nullables with coalescing to avoid null warnings.
            return new SiteSettingDto
            {
                Id = s.Id,
                RowVersion = s.RowVersion ?? Array.Empty<byte>(),

                // Basic
                Title = s.Title ?? string.Empty,
                LogoUrl = s.LogoUrl,
                ContactEmail = s.ContactEmail,

                // Localization
                DefaultCulture = string.IsNullOrWhiteSpace(s.DefaultCulture) ? "de-DE" : s.DefaultCulture,
                SupportedCulturesCsv = string.IsNullOrWhiteSpace(s.SupportedCulturesCsv) ? "de-DE,en-US" : s.SupportedCulturesCsv,
                DefaultCountry = string.IsNullOrWhiteSpace(s.DefaultCountry) ? "DE" : s.DefaultCountry,
                DefaultCurrency = string.IsNullOrWhiteSpace(s.DefaultCurrency) ? "EUR" : s.DefaultCurrency,
                TimeZone = string.IsNullOrWhiteSpace(s.TimeZone) ? "Europe/Berlin" : s.TimeZone,
                DateFormat = string.IsNullOrWhiteSpace(s.DateFormat) ? "yyyy-MM-dd" : s.DateFormat,
                TimeFormat = string.IsNullOrWhiteSpace(s.TimeFormat) ? "HH:mm" : s.TimeFormat,

                // Units
                MeasurementSystem = string.IsNullOrWhiteSpace(s.MeasurementSystem) ? "Metric" : s.MeasurementSystem,
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
                WebAuthnRelyingPartyId = string.IsNullOrWhiteSpace(s.WebAuthnRelyingPartyId) ? "localhost" : s.WebAuthnRelyingPartyId,
                WebAuthnRelyingPartyName = string.IsNullOrWhiteSpace(s.WebAuthnRelyingPartyName) ? "Darwin" : s.WebAuthnRelyingPartyName,
                WebAuthnAllowedOriginsCsv = string.IsNullOrWhiteSpace(s.WebAuthnAllowedOriginsCsv) ? "https://localhost:5001" : s.WebAuthnAllowedOriginsCsv,
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

                // Routing
                HomeSlug = string.IsNullOrWhiteSpace(s.HomeSlug) ? "home" : s.HomeSlug
            };
        }
    }
}
