using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Darwin.Application.Loyalty;

internal static class LoyaltyLocalizedTextResolver
{
    public static string Resolve(
        string? json,
        string? culture,
        string key,
        string fallback,
        string? defaultCulture = "de-DE")
    {
        var value = TryResolve(json, culture, key)
            ?? TryResolve(json, defaultCulture, key)
            ?? TryResolve(json, "de-DE", key)
            ?? TryResolve(json, "en-US", key);

        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? TryResolve(string? json, string? culture, string key)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
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
                        var value = entry.Value.GetString();
                        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
}
