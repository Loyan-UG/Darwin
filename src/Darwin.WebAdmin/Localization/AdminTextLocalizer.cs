using System;
using System.Globalization;
using System.Linq;
using Darwin.Application.Abstractions.Persistence;
using Darwin.WebAdmin;
using Darwin.Domain.Entities.Businesses;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.WebAdmin.Localization
{
    public interface IAdminTextLocalizer
    {
        string T(string key);
        IReadOnlyList<(string Culture, string Label)> GetSupportedLanguageOptions();
    }

    /// <summary>
    /// Thin facade over the shared ASP.NET Core localization pipeline for admin UI text.
    /// SharedResource*.resx is now the single source of truth for admin-facing labels.
    /// </summary>
    public sealed class AdminTextLocalizer : IAdminTextLocalizer
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IAppDbContext _db;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminTextLocalizer(
            IStringLocalizer<SharedResource> localizer,
            IAppDbContext db,
            ISiteSettingCache siteSettingCache,
            IHttpContextAccessor httpContextAccessor)
        {
            _localizer = localizer;
            _db = db;
            _siteSettingCache = siteSettingCache;
            _httpContextAccessor = httpContextAccessor;
        }

        public string T(string key)
        {
            var currentCulture = CultureInfo.CurrentUICulture?.Name ?? AdminCultureCatalog.DefaultCulture;

            var businessOverrides = GetCurrentBusinessOverrides();
            if (AdminTextOverrideCatalog.TryResolve(businessOverrides, currentCulture, key, out var businessOverrideValue))
            {
                return businessOverrideValue;
            }

            var platformOverrides = GetCurrentPlatformOverrides();
            if (AdminTextOverrideCatalog.TryResolve(platformOverrides, currentCulture, key, out var overrideValue))
            {
                return overrideValue;
            }

            var localized = _localizer[key];
            return !localized.ResourceNotFound && !string.Equals(localized.Value, key, StringComparison.Ordinal)
                ? localized.Value
                : key;
        }

        public IReadOnlyList<(string Culture, string Label)> GetSupportedLanguageOptions()
        {
            return AdminCultureCatalog.LanguageOptions;
        }

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GetCurrentPlatformOverrides()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                if (httpContext.Items.TryGetValue(typeof(AdminTextOverrideCatalog), out var cached) &&
                    cached is IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> parsed)
                {
                    return parsed;
                }

                var settings = _siteSettingCache.GetAsync().GetAwaiter().GetResult();
                var parsedOverrides = AdminTextOverrideCatalog.Parse(settings.AdminTextOverridesJson);
                httpContext.Items[typeof(AdminTextOverrideCatalog)] = parsedOverrides;
                return parsedOverrides;
            }

            var fallbackSettings = _siteSettingCache.GetAsync().GetAwaiter().GetResult();
            return AdminTextOverrideCatalog.Parse(fallbackSettings.AdminTextOverridesJson);
        }

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GetCurrentBusinessOverrides()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return AdminTextOverrideCatalog.Empty;
            }

            const string cacheKey = "AdminTextLocalizer.BusinessOverrides";
            if (httpContext.Items.TryGetValue(cacheKey, out var cached) &&
                cached is IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> parsed)
            {
                return parsed;
            }

            var businessId = TryResolveCurrentBusinessId(httpContext);
            if (!businessId.HasValue)
            {
                httpContext.Items[cacheKey] = AdminTextOverrideCatalog.Empty;
                return AdminTextOverrideCatalog.Empty;
            }

            var businessOverridesJson = _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == businessId.Value)
                .Select(x => x.AdminTextOverridesJson)
                .FirstOrDefaultAsync()
                .GetAwaiter()
                .GetResult();

            var businessOverrides = AdminTextOverrideCatalog.Parse(businessOverridesJson);
            httpContext.Items[cacheKey] = businessOverrides;
            return businessOverrides;
        }

        private static Guid? TryResolveCurrentBusinessId(HttpContext httpContext)
        {
            if (TryParseGuid(httpContext.Request.RouteValues["businessId"]?.ToString(), out var routeBusinessId))
            {
                return routeBusinessId;
            }

            var controller = httpContext.Request.RouteValues["controller"]?.ToString();
            if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                TryParseGuid(httpContext.Request.RouteValues["id"]?.ToString(), out var routeId))
            {
                return routeId;
            }

            if (TryParseGuid(httpContext.Request.Query["businessId"].ToString(), out var queryBusinessId))
            {
                return queryBusinessId;
            }

            if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                TryParseGuid(httpContext.Request.Query["id"].ToString(), out var queryId))
            {
                return queryId;
            }

            if (httpContext.Request.HasFormContentType)
            {
                if (TryParseGuid(httpContext.Request.Form["BusinessId"].ToString(), out var formBusinessId))
                {
                    return formBusinessId;
                }

                if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                    TryParseGuid(httpContext.Request.Form["Id"].ToString(), out var formId))
                {
                    return formId;
                }
            }

            return null;
        }

        private static bool TryParseGuid(string? value, out Guid id)
        {
            return Guid.TryParse(value, out id) && id != Guid.Empty;
        }
    }
}
