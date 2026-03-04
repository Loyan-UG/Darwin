using System;

namespace Darwin.Mobile.Shared.Models.Notifications;

/// <summary>
/// Client model for successful push-device registration.
/// </summary>
public sealed class PushDeviceRegistrationClientModel
{
    public string DeviceId { get; init; } = string.Empty;
    public DateTime RegisteredAtUtc { get; init; }
}
