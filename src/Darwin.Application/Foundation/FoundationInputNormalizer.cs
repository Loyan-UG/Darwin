namespace Darwin.Application.Foundation;

internal static class FoundationInputNormalizer
{
    private static readonly string[] SensitiveFragments =
    [
        "password",
        "secret",
        "token",
        "credential",
        "apikey",
        "api_key",
        "privatekey",
        "private_key",
        "refresh"
    ];

    public static string? Required(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string? Optional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string? Key(string? value)
    {
        var normalized = Required(value);
        return normalized?.ToLowerInvariant();
    }

    public static string Json(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized;
    }

    public static bool LooksSensitive(string? value)
    {
        var normalized = value?.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) &&
               SensitiveFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }
}
