using Darwin.Web.Services.Settings;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Renders a &lt;select&gt; element whose options are populated from site settings.
/// Use this tag instead of asp-items for fields like Locale, Currency or Timezone,
/// so that options come from <see cref="SiteSettingDto"/> rather than being hard‑coded
/// in each view.
/// Example:
///   &lt;setting-select asp-for="Locale" setting="SupportedCulturesCsv"&gt;&lt;/setting-select&gt;
/// 
/// It looks up the given setting name on <see cref="SiteSettingDto"/> via reflection.
/// If the value is a comma‑separated string (e.g., "de-DE,en-US") it is split into items.
/// When the value is a single string (e.g., DefaultCurrency = "EUR") a single‑item list
/// is produced.
/// For Time zones, if the property is null or empty it falls back to all system time zone
/// identifiers via <see cref="TimeZoneInfo.GetSystemTimeZones()"/>.
/// </summary>
namespace Darwin.Web.TagHelpers
{
    [HtmlTargetElement("setting-select", Attributes = "asp-for, setting")]
    public sealed class SettingSelectTagHelper : TagHelper
    {
        private readonly ISiteSettingCache _siteSettingCache;

        /// <summary>
        /// Creates a new instance injecting the site setting cache.
        /// </summary>
        public SettingSelectTagHelper(ISiteSettingCache siteSettingCache)
        {
            _siteSettingCache = siteSettingCache;
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

            // Use reflection to get the property value
            var property = siteSettings.GetType().GetProperty(Setting, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            string? rawValue = property?.GetValue(siteSettings) as string;

            // Build list of options
            string[] options;
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                // If the property contains commas, split into multiple options
                if (rawValue.Contains(','))
                {
                    options = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                else
                {
                    // Single value becomes a single option
                    options = new[] { rawValue.Trim() };
                }
            }
            else
            {
                // Fallbacks: all system time zones when setting is "TimeZone",
                // otherwise a minimal list containing the current value
                if (string.Equals(Setting, "TimeZone", StringComparison.OrdinalIgnoreCase))
                {
                    options = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToArray();
                }
                else
                {
                    options = string.IsNullOrEmpty(currentValue) ? Array.Empty<string>() : new[] { currentValue };
                }
            }

            // Build option tags with selection
            var innerHtml = string.Empty;
            foreach (var option in options)
            {
                var selected = string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase) ? "selected" : null;
                innerHtml += $"<option value=\"{option}\"{(selected != null ? " selected" : string.Empty)}>{option}</option>";
            }

            output.Content.SetHtmlContent(innerHtml);
        }
    }
}
