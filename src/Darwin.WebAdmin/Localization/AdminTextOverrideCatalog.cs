using System.Collections.Generic;
using Darwin.Application.AdminTextOverrides;

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

            var parsed = AdminTextOverrideJsonCatalog.Parse(json);
            if (parsed.Count == 0)
            {
                return Empty;
            }

            var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (culture, entries) in parsed)
            {
                var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);
                if (!normalized.TryGetValue(normalizedCulture, out var existingEntries))
                {
                    normalized[normalizedCulture] = entries;
                    continue;
                }

                var merged = new Dictionary<string, string>(existingEntries, StringComparer.OrdinalIgnoreCase);
                foreach (var (key, value) in entries)
                {
                    merged[key] = value;
                }

                normalized[normalizedCulture] = merged;
            }

            return normalized.Count == 0 ? Empty : normalized;
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
