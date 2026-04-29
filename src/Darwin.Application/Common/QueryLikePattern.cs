namespace Darwin.Application.Common;

internal static class QueryLikePattern
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string value)
    {
        return $"%{Escape(value.Trim())}%";
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
