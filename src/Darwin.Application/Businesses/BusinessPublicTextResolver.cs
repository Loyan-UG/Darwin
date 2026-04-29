using System.Collections.Generic;
using Darwin.Application.AdminTextOverrides;

namespace Darwin.Application.Businesses;

internal static class BusinessPublicTextResolver
{
    public const string PublicBusinessNameKey = "PublicBusinessName";
    public const string PublicBusinessShortDescriptionKey = "PublicBusinessShortDescription";

    public static string ResolveName(
        string name,
        string? adminTextOverridesJson,
        string? culture,
        string? defaultCulture)
    {
        return ResolveText(
            adminTextOverridesJson,
            culture,
            defaultCulture,
            PublicBusinessNameKey) ?? name;
    }

    public static string? ResolveShortDescription(
        string? shortDescription,
        string? adminTextOverridesJson,
        string? culture,
        string? defaultCulture)
    {
        return ResolveText(
            adminTextOverridesJson,
            culture,
            defaultCulture,
            PublicBusinessShortDescriptionKey) ?? shortDescription;
    }

    private static string? ResolveText(
        string? adminTextOverridesJson,
        string? culture,
        string? defaultCulture,
        string key)
    {
        var catalog = AdminTextOverrideJsonCatalog.Parse(adminTextOverridesJson);
        if (catalog.Count == 0)
        {
            return null;
        }

        foreach (var candidateCulture in GetCandidateCultures(culture, defaultCulture))
        {
            if (catalog.TryGetValue(candidateCulture, out var entries)
                && entries.TryGetValue(key, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateCultures(string? culture, string? defaultCulture)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in new[] { culture, defaultCulture, "de-DE", "en-US" })
        {
            var normalized = NormalizeCulture(value);
            if (normalized.Length > 0 && seen.Add(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static string NormalizeCulture(string? culture)
    {
        return string.IsNullOrWhiteSpace(culture) ? string.Empty : culture.Trim();
    }
}
