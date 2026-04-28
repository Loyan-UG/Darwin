using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Darwin.Application.Billing;

internal static class BillingLocalizedTextResolver
{
    public static string ResolvePlanName(string name, string? featuresJson, string? culture = null)
        => Resolve(featuresJson, culture, "name", name) ?? name;

    public static string? ResolvePlanDescription(string? description, string? featuresJson, string? culture = null)
        => Resolve(featuresJson, culture, "description", description);

    private static string? Resolve(string? featuresJson, string? culture, string key, string? fallback)
    {
        foreach (var candidateCulture in GetCandidateCultures(culture))
        {
            var value = TryResolve(featuresJson, candidateCulture, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return fallback;
    }

    private static string? TryResolve(string? featuresJson, string culture, string key)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(featuresJson);
            if (!doc.RootElement.TryGetProperty("localized", out var localized)
                || localized.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var cultureProperty in localized.EnumerateObject())
            {
                if (!string.Equals(cultureProperty.Name, culture, StringComparison.OrdinalIgnoreCase)
                    || cultureProperty.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var entry in cultureProperty.Value.EnumerateObject())
                {
                    if (string.Equals(entry.Name, key, StringComparison.OrdinalIgnoreCase)
                        && entry.Value.ValueKind == JsonValueKind.String)
                    {
                        return entry.Value.GetString();
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateCultures(string? culture)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in new[]
        {
            culture,
            CultureInfo.CurrentUICulture?.Name,
            "de-DE",
            "en-US"
        })
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value.Trim()))
            {
                yield return value.Trim();
            }
        }
    }
}
