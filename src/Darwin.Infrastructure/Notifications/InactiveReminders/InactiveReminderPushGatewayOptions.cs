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
}
