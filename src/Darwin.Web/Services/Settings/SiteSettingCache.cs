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
    /// </summary>
    public sealed class SiteSettingCache : ISiteSettingCache
    {
        private readonly IMemoryCache _cache;
        private readonly IAppDbContext _db;
        private const string CacheKey = "SiteSettingCache:Current";

        public SiteSettingCache(IMemoryCache cache, IAppDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        /// <inheritdoc />
        public async Task<SiteSettingDto> GetAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out SiteSettingDto cached))
            {
                return cached;
            }

            // Load the single SiteSetting entity. We assume the DB enforces exactly one row.
            var entity = await _db.Set<SiteSetting>()
                                  .AsNoTracking()
                                  .SingleAsync(ct);

            var dto = Map(entity);

            // Cache with sliding expiration. Adjust expiration to suit your application's needs.
            _cache.Set(CacheKey, dto, new MemoryCacheEntryOptions
            {
                SlidingExpiration = System.TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            });

            return dto;
        }

        /// <inheritdoc />
        public void Invalidate()
        {
            _cache.Remove(CacheKey);
        }

        /// <summary>
        /// Maps the persistence entity to a DTO. Keep this mapping in sync with
        /// <see cref="SiteSettingDto"/>. This method should be updated whenever
        /// new properties are added to either the entity or DTO.
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

                // Localization
                DefaultCulture = s.DefaultCulture,
                SupportedCulturesCsv = s.SupportedCulturesCsv,
                DefaultCountry = s.DefaultCountry,
                DefaultCurrency = s.DefaultCurrency,
                TimeZone = s.TimeZone,
                DateFormat = s.DateFormat,
                TimeFormat = s.TimeFormat,

                // Units
                MeasurementSystem = s.MeasurementSystem,
                DisplayWeightUnit = s.DisplayWeightUnit,
                DisplayLengthUnit = s.DisplayLengthUnit,

                // SEO
                EnableCanonical = s.EnableCanonical,
                HreflangEnabled = s.HreflangEnabled,
                SeoTitleTemplate = s.SeoTitleTemplate,
                // Map the meta description template directly to the DTO's corresponding property.
                SeoMetaDescriptionTemplate = s.SeoMetaDescriptionTemplate,
                // Map OpenGraph defaults JSON
                OpenGraphDefaultsJson = s.OpenGraphDefaultsJson,

                // Analytics
                GoogleAnalyticsId = s.GoogleAnalyticsId,
                GoogleTagManagerId = s.GoogleTagManagerId,
                GoogleSearchConsoleVerification = s.GoogleSearchConsoleVerification,

                // Feature flags and WhatsApp
                FeatureFlagsJson = s.FeatureFlagsJson,
                WhatsAppEnabled = s.WhatsAppEnabled,
                WhatsAppBusinessPhoneId = s.WhatsAppBusinessPhoneId,
                WhatsAppAccessToken = s.WhatsAppAccessToken,
                WhatsAppFromPhoneE164 = s.WhatsAppFromPhoneE164,
                WhatsAppAdminRecipientsCsv = s.WhatsAppAdminRecipientsCsv,

                // Additional measurement & formatting overrides
                MeasurementSettingsJson = s.MeasurementSettingsJson,
                NumberFormattingOverridesJson = s.NumberFormattingOverridesJson,

                // Routing
                HomeSlug = s.HomeSlug
            };
        }
    }
}