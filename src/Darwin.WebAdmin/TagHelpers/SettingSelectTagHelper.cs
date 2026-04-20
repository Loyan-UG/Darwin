using Darwin.Application.Settings.DTOs;
using Darwin.WebAdmin.Localization;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

/// <summary>
/// Renders a &lt;select&gt; element whose options are populated from site settings.
/// Use this tag instead of asp-items for fields like Locale, Currency or Timezone,
/// so that options come from <see cref="SiteSettingDto"/> rather than being hard-coded
/// in each view.
/// Example:
///   &lt;setting-select asp-for="Locale" setting="SupportedCulturesCsv"&gt;&lt;/setting-select&gt;
/// 
/// It looks up the given setting name on <see cref="SiteSettingDto"/> via reflection.
/// If the value is a comma-separated string (e.g., "de-DE,en-US") it is split into items.
/// When the value is a single string (e.g., DefaultCurrency = "EUR") a single-item list
/// is produced.
/// For Time zones, if the property is null or empty it falls back to all system time zone
/// identifiers via <see cref="TimeZoneInfo.GetSystemTimeZones()"/>.
/// </summary>
namespace Darwin.WebAdmin.TagHelpers
{
    [HtmlTargetElement("setting-select", Attributes = "asp-for")]
    public sealed class SettingSelectTagHelper : TagHelper
    {
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IAdminTextLocalizer _textLocalizer;

        /// <summary>
        /// Creates a new instance injecting the site setting cache.
        /// </summary>
        public SettingSelectTagHelper(ISiteSettingCache siteSettingCache, IAdminTextLocalizer textLocalizer)
        {
            _siteSettingCache = siteSettingCache;
            _textLocalizer = textLocalizer;
        }

        /// <summary>
        /// Binds the select element to a model property (required).
        /// </summary>
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; } = default!;

        /// <summary>
        /// Name of the property in <see cref="SiteSettingDto"/> to read the options from.
        /// For example "SupportedCulturesCsv", "DefaultCurrency" or "TimeZone".
        /// </summary>
        [HtmlAttributeName("setting")]
        public string Setting { get; set; } = string.Empty;

        /// <summary>
        /// Backward-compatible alias for <see cref="Setting"/>.
        /// Older views used "key" before the tag helper settled on the "setting" name.
        /// </summary>
        [HtmlAttributeName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Generates the select element with options from site settings.
        /// </summary>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Replace the tag name with a standard select
            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "form-select");

            // Set id/name based on the model expression
            var name = For.Name;
            output.Attributes.SetAttribute("id", name);
            output.Attributes.SetAttribute("name", name);

            // Read the current value of the bound model property
            var currentValue = For.Model?.ToString() ?? string.Empty;

            // Retrieve current site settings from the cache (cached in memory)
            var siteSettings = await _siteSettingCache.GetAsync();

            // Build list of options
            var options = BuildOptions(siteSettings, currentValue);

            // Build option tags with selection
            var innerHtml = string.Empty;
            foreach (var option in options)
            {
                var selected = string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase) ? "selected" : null;
                var encodedValue = HtmlEncoder.Default.Encode(option);
                var encodedLabel = HtmlEncoder.Default.Encode(GetOptionLabel(option));
                innerHtml += $"<option value=\"{encodedValue}\"{(selected != null ? " selected" : string.Empty)}>{encodedLabel}</option>";
            }

            output.Content.SetHtmlContent(innerHtml);
        }

        private string[] BuildOptions(SiteSettingDto siteSettings, string currentValue)
        {
            var configuredName = string.IsNullOrWhiteSpace(Setting) ? Key : Setting;

            if (string.Equals(configuredName, "SupportedLocalesCsv", StringComparison.OrdinalIgnoreCase))
            {
                return SplitCsvOrFallback(siteSettings.SupportedCulturesCsv, currentValue);
            }

            if (string.Equals(configuredName, nameof(SiteSettingDto.SupportedCulturesCsv), StringComparison.OrdinalIgnoreCase))
            {
                return SplitCsvOrFallback(siteSettings.SupportedCulturesCsv, currentValue);
            }

            if (string.Equals(configuredName, "SupportedCurrenciesCsv", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    siteSettings.DefaultCurrency,
                    SiteSettingDto.DefaultCurrencyDefault,
                    "USD",
                    currentValue
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            }

            if (string.Equals(configuredName, "SupportedTimezonesCsv", StringComparison.OrdinalIgnoreCase))
            {
                return TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => tz.Id)
                    .Prepend(siteSettings.TimeZone ?? SiteSettingDto.TimeZoneDefault)
                    .Prepend("UTC")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            var property = siteSettings.GetType().GetProperty(configuredName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            string? rawValue = property?.GetValue(siteSettings) as string;

            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                return SplitCsvOrFallback(rawValue, currentValue);
            }

            if (string.Equals(configuredName, nameof(SiteSettingDto.TimeZone), StringComparison.OrdinalIgnoreCase))
            {
                return TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => tz.Id)
                    .Prepend("UTC")
                    .Prepend(siteSettings.TimeZone ?? SiteSettingDto.TimeZoneDefault)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return string.IsNullOrEmpty(currentValue) ? Array.Empty<string>() : new[] { currentValue };
        }

        private string GetOptionLabel(string option)
        {
            var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(option);
            if (string.Equals(normalizedCulture, AdminCultureCatalog.German, StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("LocaleGermanGermany");
            }

            if (string.Equals(normalizedCulture, AdminCultureCatalog.English, StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("LocaleEnglishUnitedStates");
            }

            if (string.Equals(option, "EUR", StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("CurrencyEuro");
            }

            if (string.Equals(option, "USD", StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("CurrencyUsd");
            }

            if (string.Equals(option, SiteSettingDto.TimeZoneDefault, StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("TimeZoneEuropeBerlin");
            }

            if (string.Equals(option, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                return _textLocalizer.T("Utc");
            }

            return option;
        }

        private static string[] SplitCsvOrFallback(string rawValue, string currentValue)
        {
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                if (rawValue.Contains(','))
                {
                    return rawValue
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }

                return new[] { rawValue.Trim() };
            }

            return string.IsNullOrEmpty(currentValue) ? Array.Empty<string>() : new[] { currentValue };
        }
    }
}
