using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Darwin.WebAdmin.Localization
{
    /// <summary>
    /// Parses and resolves platform-level admin text overrides layered on top of shared resx resources.
    /// </summary>
    public static class AdminTextOverrideCatalog
    {
        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Empty;
            }

            try
            {
                var root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                if (root is null || root.Count == 0)
                {
                    return Empty;
                }

                var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (culture, entries) in root)
                {
                    if (string.IsNullOrWhiteSpace(culture) || entries is null || entries.Count == 0)
                    {
                        continue;
                    }

                    var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);
                    var values = entries
                        .Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                        .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim(), StringComparer.OrdinalIgnoreCase);

                    if (values.Count > 0)
                    {
                        normalized[normalizedCulture] = values;
                    }
                }

                return normalized.Count == 0 ? Empty : normalized;
            }
            catch (JsonException)
            {
                return Empty;
            }
        }

        public static bool TryResolve(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> overrides,
            string culture,
            string key,
            out string value)
        {
            value = string.Empty;
            if (overrides.Count == 0)
            {
                return false;
            }

            var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);
            if (!overrides.TryGetValue(normalizedCulture, out var entries))
            {
                return false;
            }

            if (!entries.TryGetValue(key, out var resolvedValue) || string.IsNullOrWhiteSpace(resolvedValue))
            {
                return false;
            }

            value = resolvedValue;
            return true;
        }

        public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Empty =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    }
}
