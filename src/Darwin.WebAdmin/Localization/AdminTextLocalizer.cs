using System;
using System.Globalization;
using System.Linq;
using Darwin.WebAdmin;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminTextLocalizer(
            IStringLocalizer<SharedResource> localizer,
            IHttpContextAccessor httpContextAccessor)
        {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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
                return AdminTextOverrideRequestContext.GetOverrides(
                    httpContext.Items,
                    AdminTextOverrideRequestContext.PlatformOverridesItemKey);
            }

            return AdminTextOverrideCatalog.Empty;
        }

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GetCurrentBusinessOverrides()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return AdminTextOverrideCatalog.Empty;
            }

            return AdminTextOverrideRequestContext.GetOverrides(
                httpContext.Items,
                AdminTextOverrideRequestContext.BusinessOverridesItemKey);
        }
    }
}
