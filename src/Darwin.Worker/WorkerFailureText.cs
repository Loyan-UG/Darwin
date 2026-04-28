namespace Darwin.Worker;

internal static class WorkerFailureText
{
    public static string Truncate(string? message, int maxLength = 1024)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown error";
        }

        return message.Length <= maxLength ? message : message[..maxLength];
    }
}
