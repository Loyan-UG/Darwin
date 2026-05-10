namespace Darwin.Infrastructure.Notifications.Brevo;

/// <summary>
/// Options for Brevo transactional email delivery. Bind from "Email:Brevo".
/// </summary>
public sealed class BrevoEmailOptions
{
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3/";
    public string? ApiKey { get; set; }
    public string SenderEmail { get; set; } = "no-reply@darwin.com";
    public string SenderName { get; set; } = "Darwin";
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
    public string? WebhookUsername { get; set; }
    public string? WebhookPassword { get; set; }
    public bool SandboxMode { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string[] DefaultTags { get; set; } = ["darwin", "transactional"];
    public Dictionary<string, int> TemplateIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
