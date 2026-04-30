namespace Darwin.Application.Common;

internal static class QueryLikePattern
{
    public const string EscapeCharacter = "\\";
    private const int MaxSearchTermLength = 256;

    public static string Contains(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > MaxSearchTermLength)
        {
            trimmed = trimmed[..MaxSearchTermLength];
        }

        return $"%{Escape(trimmed)}%";
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
