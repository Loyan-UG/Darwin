using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
        var catalog = Parse(adminTextOverridesJson);
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

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(string? json)
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

                var entryValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, value) in entries)
                {
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                    {
                        entryValues[key.Trim()] = value.Trim();
                    }
                }

                if (entryValues.Count > 0)
                {
                    normalized[NormalizeCulture(culture)] = entryValues;
                }
            }

            return normalized.Count == 0 ? Empty : normalized;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return Empty;
        }
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

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Empty =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
}
