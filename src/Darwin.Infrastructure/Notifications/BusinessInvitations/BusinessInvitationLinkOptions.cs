namespace Darwin.Infrastructure.Notifications.BusinessInvitations;

/// <summary>
/// Configuration for business-invitation acceptance links embedded into onboarding emails.
/// The base URL may point to an HTTPS landing page, a universal link, or a custom app scheme.
/// </summary>
public sealed class BusinessInvitationLinkOptions
{
    /// <summary>
    /// Base URL used to construct invitation-acceptance links.
    /// Example values:
    /// - https://business.example.com/invitations/accept
    /// - darwin-business://InvitationAcceptance
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
