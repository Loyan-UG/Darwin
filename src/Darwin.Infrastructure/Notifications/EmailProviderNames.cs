namespace Darwin.Infrastructure.Notifications;

public static class EmailProviderNames
{
    public const string Smtp = "SMTP";
    public const string Brevo = "Brevo";

    public static string Normalize(string? provider)
    {
        if (string.Equals(provider, Brevo, StringComparison.OrdinalIgnoreCase))
        {
            return Brevo;
        }

        if (string.Equals(provider, Smtp, StringComparison.OrdinalIgnoreCase))
        {
            return Smtp;
        }

        return string.IsNullOrWhiteSpace(provider) ? Smtp : provider.Trim();
    }
}
