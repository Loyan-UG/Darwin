namespace Darwin.Worker;

internal static class WorkerFailureText
{
    public static string Truncate(string? message, int maxLength = 1024)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown error";
        }

        var sanitized = Sanitize(message);
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    private static string Sanitize(string message)
    {
        var sanitized = new char[message.Length];
        var writeIndex = 0;
        var previousWasWhitespace = false;

        foreach (var ch in message)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    sanitized[writeIndex++] = ' ';
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (char.IsControl(ch))
            {
                continue;
            }

            sanitized[writeIndex++] = ch;
            previousWasWhitespace = false;
        }

        var value = new string(sanitized, 0, writeIndex).Trim();
        return string.IsNullOrWhiteSpace(value) ? "Unknown error" : value;
    }
}
