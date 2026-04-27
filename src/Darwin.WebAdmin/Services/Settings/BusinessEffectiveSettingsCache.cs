using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Darwin.WebAdmin.Services.Settings
{
    public sealed class BusinessEffectiveSettingsDto
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public string DefaultCulture { get; init; } = SiteSettingDto.DefaultCultureDefault;
        public string DefaultCurrency { get; init; } = SiteSettingDto.DefaultCurrencyDefault;
        public string DefaultTimeZoneId { get; init; } = SiteSettingDto.TimeZoneDefault;
        public string? AdminTextOverridesJson { get; init; }
        public string BrandDisplayName { get; init; } = string.Empty;
        public string? BrandLogoUrl { get; init; }
        public string? BrandPrimaryColorHex { get; init; }
        public string? BrandSecondaryColorHex { get; init; }
        public string? SupportEmail { get; init; }
        public string? CommunicationSenderName { get; init; }
        public string? CommunicationReplyToEmail { get; init; }
        public bool CustomerEmailNotificationsEnabled { get; init; }
        public bool CustomerMarketingEmailsEnabled { get; init; }
        public bool OperationalAlertEmailsEnabled { get; init; }
        public bool UsesPlatformCultureFallback { get; init; }
        public bool UsesPlatformCurrencyFallback { get; init; }
        public bool UsesPlatformTimeZoneFallback { get; init; }
        public bool UsesPlatformContactEmailFallback { get; init; }
        public bool UsesPlatformSenderFallback { get; init; }
    }

    public interface IBusinessEffectiveSettingsCache
    {
        Task<BusinessEffectiveSettingsDto?> GetAsync(Guid businessId, CancellationToken ct = default);
        void Invalidate(Guid businessId);
        void InvalidateAll();
    }

    /// <summary>
    /// Resolves business-scoped settings layered over the platform SiteSetting row.
    /// This keeps tenant-aware defaults consistent across WebAdmin consumers.
    /// </summary>
    public sealed class BusinessEffectiveSettingsCache : IBusinessEffectiveSettingsCache
    {
        private const string KeyIndexCacheKey = "BusinessEffectiveSettings:KeyIndex";
        private readonly IMemoryCache _cache;
        private readonly IAppDbContext _db;
        private readonly ISiteSettingCache _siteSettings;

        public BusinessEffectiveSettingsCache(
            IMemoryCache cache,
            IAppDbContext db,
            ISiteSettingCache siteSettings)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _siteSettings = siteSettings ?? throw new ArgumentNullException(nameof(siteSettings));
        }

        public async Task<BusinessEffectiveSettingsDto?> GetAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return null;
            }

            var cacheKey = BuildCacheKey(businessId);
            if (_cache.TryGetValue(cacheKey, out BusinessEffectiveSettingsDto? cached) && cached is not null)
            {
                return cached;
            }

            var platform = await _siteSettings.GetAsync(ct).ConfigureAwait(false);
            var business = await _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == businessId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.DefaultCulture,
                    x.DefaultCurrency,
                    x.DefaultTimeZoneId,
                    x.AdminTextOverridesJson,
                    x.BrandDisplayName,
                    x.BrandLogoUrl,
                    x.BrandPrimaryColorHex,
                    x.BrandSecondaryColorHex,
                    x.ContactEmail,
                    x.SupportEmail,
                    x.CommunicationSenderName,
                    x.CommunicationReplyToEmail,
                    x.CustomerEmailNotificationsEnabled,
                    x.CustomerMarketingEmailsEnabled,
                    x.OperationalAlertEmailsEnabled
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                return null;
            }

            var effective = new BusinessEffectiveSettingsDto
            {
                BusinessId = business.Id,
                BusinessName = business.Name,
                DefaultCulture = Resolve(business.DefaultCulture, platform.DefaultCulture, SiteSettingDto.DefaultCultureDefault),
                DefaultCurrency = Resolve(business.DefaultCurrency, platform.DefaultCurrency, SiteSettingDto.DefaultCurrencyDefault).ToUpperInvariant(),
                DefaultTimeZoneId = Resolve(business.DefaultTimeZoneId, platform.TimeZone, SiteSettingDto.TimeZoneDefault),
                AdminTextOverridesJson = business.AdminTextOverridesJson,
                BrandDisplayName = Resolve(business.BrandDisplayName, platform.Title, business.Name),
                BrandLogoUrl = ResolveOptional(business.BrandLogoUrl, platform.LogoUrl),
                BrandPrimaryColorHex = NormalizeOptional(business.BrandPrimaryColorHex),
                BrandSecondaryColorHex = NormalizeOptional(business.BrandSecondaryColorHex),
                SupportEmail = ResolveOptional(business.SupportEmail, business.ContactEmail, platform.ContactEmail, platform.SmtpFromAddress),
                CommunicationSenderName = ResolveOptional(business.CommunicationSenderName, platform.SmtpFromDisplayName, business.BrandDisplayName, platform.Title, business.Name),
                CommunicationReplyToEmail = ResolveOptional(business.CommunicationReplyToEmail, business.SupportEmail, business.ContactEmail, platform.ContactEmail, platform.SmtpFromAddress),
                CustomerEmailNotificationsEnabled = business.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = business.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = business.OperationalAlertEmailsEnabled,
                UsesPlatformCultureFallback = string.IsNullOrWhiteSpace(business.DefaultCulture),
                UsesPlatformCurrencyFallback = string.IsNullOrWhiteSpace(business.DefaultCurrency),
                UsesPlatformTimeZoneFallback = string.IsNullOrWhiteSpace(business.DefaultTimeZoneId),
                UsesPlatformContactEmailFallback = string.IsNullOrWhiteSpace(business.SupportEmail) && string.IsNullOrWhiteSpace(business.ContactEmail),
                UsesPlatformSenderFallback = string.IsNullOrWhiteSpace(business.CommunicationSenderName)
            };

            _cache.Set(cacheKey, effective, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            });
            TrackCacheKey(cacheKey);

            return effective;
        }

        public void Invalidate(Guid businessId)
        {
            if (businessId != Guid.Empty)
            {
                _cache.Remove(BuildCacheKey(businessId));
            }
        }

        public void InvalidateAll()
        {
            var keys = GetTrackedCacheKeys();
            lock (keys)
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                keys.Clear();
            }
        }

        private void TrackCacheKey(string cacheKey)
        {
            var keys = GetTrackedCacheKeys();
            lock (keys)
            {
                keys.Add(cacheKey);
            }
        }

        private HashSet<string> GetTrackedCacheKeys() =>
            _cache.GetOrCreate(KeyIndexCacheKey, entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                return new HashSet<string>(StringComparer.Ordinal);
            })!;

        private static string BuildCacheKey(Guid businessId) => $"BusinessEffectiveSettings:{businessId:N}";

        private static string Resolve(params string?[] values) =>
            values.Select(NormalizeOptional).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        private static string? ResolveOptional(params string?[] values) =>
            values.Select(NormalizeOptional).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
