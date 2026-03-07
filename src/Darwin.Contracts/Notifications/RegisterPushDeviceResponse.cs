using System;

namespace Darwin.Contracts.Notifications;

/// <summary>
/// Response payload for push-device registration/upsert operations.
/// </summary>
public sealed class RegisterPushDeviceResponse
{
    /// <summary>
    /// Echo of the registered installation identifier.
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp when registration was accepted by server.
    /// </summary>
    public DateTime RegisteredAtUtc { get; init; }
}
