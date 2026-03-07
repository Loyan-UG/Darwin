namespace Darwin.Application.Identity;

/// <summary>
/// Centralizes metadata keys stored inside <c>UserEngagementSnapshot.SnapshotJson</c>
/// for reminder policy and engagement tracking workflows.
/// </summary>
internal static class ReminderMetadataKeys
{
    /// <summary>
    /// Number of authenticated device heartbeats observed for the user.
    /// </summary>
    public const string DeviceHeartbeatCount = "deviceHeartbeatCount";

    /// <summary>
    /// UTC timestamp of the last authenticated device heartbeat.
    /// </summary>
    public const string LastDeviceHeartbeatAtUtc = "lastDeviceHeartbeatAtUtc";

    /// <summary>
    /// UTC timestamp of the last successful inactive reminder dispatch.
    /// </summary>
    public const string LastInactiveReminderSentAtUtc = "lastInactiveReminderSentAtUtc";

    /// <summary>
    /// Total count of successful inactive reminder dispatches.
    /// </summary>
    public const string InactiveReminderSentCount = "inactiveReminderSentCount";

    /// <summary>
    /// UTC timestamp of the last reminder attempt regardless of outcome.
    /// </summary>
    public const string LastInactiveReminderAttemptAtUtc = "lastInactiveReminderAttemptAtUtc";

    /// <summary>
    /// Last reminder outcome value (Sent, Failed, Suppressed).
    /// </summary>
    public const string LastInactiveReminderOutcome = "lastInactiveReminderOutcome";

    /// <summary>
    /// Optional code associated with the last reminder outcome.
    /// </summary>
    public const string LastInactiveReminderOutcomeCode = "lastInactiveReminderOutcomeCode";

    /// <summary>
    /// Total count of failed inactive reminder dispatches.
    /// </summary>
    public const string InactiveReminderFailedCount = "inactiveReminderFailedCount";

    /// <summary>
    /// Total count of suppressed inactive reminder outcomes.
    /// </summary>
    public const string InactiveReminderSuppressedCount = "inactiveReminderSuppressedCount";
}
