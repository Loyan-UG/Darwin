namespace Darwin.Contracts.Notifications;

/// <summary>
/// Registers or updates the current authenticated user's mobile device installation for push delivery.
/// </summary>
public sealed class RegisterPushDeviceRequest
{
    /// <summary>
    /// Stable app installation/device identifier (not the push token).
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Mobile platform for provider routing/diagnostics.
    /// </summary>
    public MobileDevicePlatform Platform { get; init; } = MobileDevicePlatform.Unknown;

    /// <summary>
    /// Provider-specific push token (FCM/APNS). May be empty when notifications are disabled.
    /// </summary>
    public string? PushToken { get; init; }

    /// <summary>
    /// Indicates whether user granted notifications permission on device.
    /// </summary>
    public bool NotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Optional semantic app version for segmentation and rollout diagnostics.
    /// </summary>
    public string? AppVersion { get; init; }

    /// <summary>
    /// Optional device model string for diagnostics only.
    /// </summary>
    public string? DeviceModel { get; init; }
}
