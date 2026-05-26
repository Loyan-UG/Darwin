using Darwin.Application.Abstractions.Notifications;
using Darwin.Domain.Entities.Settings;

namespace Darwin.Infrastructure.Notifications;

/// <summary>
/// Resolves provider-neutral sender roles to configured Site Settings email identities.
/// </summary>
public static class EmailSenderIdentityResolver
{
    public const string DefaultDisplayName = "Loyan";

    public static EmailSenderRole NormalizeRole(EmailSenderRole role) =>
        Enum.IsDefined(role) ? role : EmailSenderRole.NoReply;

    public static EmailSenderRole ParseRole(string? role)
    {
        return Enum.TryParse<EmailSenderRole>(role, ignoreCase: true, out var parsed)
            ? NormalizeRole(parsed)
            : EmailSenderRole.NoReply;
    }

    public static string RoleName(EmailSenderRole role) => NormalizeRole(role).ToString();

    public static string ResolveFromEmail(SiteSetting? settings, EmailSenderRole role)
    {
        var normalized = NormalizeRole(role);
        var configured = normalized switch
        {
            EmailSenderRole.Billing => settings?.BillingEmail,
            EmailSenderRole.Support => settings?.SupportEmail,
            EmailSenderRole.Admin => settings?.NoReplyEmail,
            _ => settings?.NoReplyEmail
        };

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return normalized switch
        {
            EmailSenderRole.Billing => "billing@loyan.de",
            EmailSenderRole.Support => "support@loyan.de",
            _ => "no-reply@loyan.de"
        };
    }

    public static string ResolveReplyToEmail(SiteSetting? settings)
    {
        return string.IsNullOrWhiteSpace(settings?.SupportEmail)
            ? "support@loyan.de"
            : settings.SupportEmail.Trim();
    }
}
