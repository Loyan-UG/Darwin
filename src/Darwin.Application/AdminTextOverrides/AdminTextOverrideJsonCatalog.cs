using System.Text.Json;

namespace Darwin.Application.AdminTextOverrides;

public static class AdminTextOverrideJsonCatalog
{
    public static bool IsValid(string? json)
    {
        return TryParse(json, out _);
    }

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(string? json)
    {
        return TryParse(json, out var catalog) ? catalog : Empty;
    }

    public static bool TryParse(
        string? json,
        out IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalog)
    {
        catalog = Empty;
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var cultureProperty in document.RootElement.EnumerateObject())
            {
                var culture = Normalize(cultureProperty.Name);
                if (culture.Length == 0 || cultureProperty.Value.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var textProperty in cultureProperty.Value.EnumerateObject())
                {
                    var key = Normalize(textProperty.Name);
                    if (key.Length == 0)
                    {
                        return false;
                    }

                    if (textProperty.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }

                    if (textProperty.Value.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    var value = textProperty.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entries[key] = value.Trim();
                    }
                }

                normalized[culture] = entries;
            }

            catalog = normalized;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Empty =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
}
