namespace Darwin.Infrastructure.Notifications.InactiveReminders;

/// <summary>
/// Configuration for HTTP push gateway dispatch used by inactive-reminder orchestration.
/// </summary>
public sealed class InactiveReminderPushGatewayOptions
{
    /// <summary>
    /// Enables gateway dispatch. When disabled, dispatcher returns a controlled failure.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Relative or absolute endpoint path used for reminder push dispatch.
    /// </summary>
    public string Endpoint { get; set; } = "/api/push/reminders/inactive";

    /// <summary>
    /// Optional bearer token used for authenticating gateway requests.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Push title template. Supports <c>{inactiveDays}</c> placeholder.
    /// </summary>
    public string TitleTemplate { get; set; } = "We miss you at Darwin";

    /// <summary>
    /// Push body template. Supports <c>{inactiveDays}</c> placeholder.
    /// </summary>
    public string BodyTemplate { get; set; } = "It has been {inactiveDays} days since your last visit. Come back and discover new rewards.";

    /// <summary>
    /// Maximum dispatch attempts for transient gateway failures.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Initial retry backoff in milliseconds for transient failures.
    /// Subsequent attempts use exponential backoff.
    /// </summary>
    public int InitialBackoffMilliseconds { get; set; } = 300;

    /// <summary>
    /// Optional Android notification channel id forwarded to the gateway for FCM-native dispatch.
    /// </summary>
    public string? AndroidChannelId { get; set; }

    /// <summary>
    /// Optional APNs topic (bundle identifier) forwarded to the gateway for APNs-native dispatch.
    /// </summary>
    public string? ApnsTopic { get; set; }

    /// <summary>
    /// Optional deep-link URL that the mobile client should open when the reminder is tapped.
    /// </summary>
    public string? DeepLinkUrl { get; set; }

    /// <summary>
    /// Optional collapse key or collapse identifier to de-duplicate reminder notifications.
    /// </summary>
    public string? CollapseKey { get; set; }

    /// <summary>
    /// Optional analytics label forwarded to the downstream gateway/provider payload.
    /// </summary>
    public string? AnalyticsLabel { get; set; }
}
