using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Input DTO for registering/updating an authenticated user's mobile device for push delivery.
/// </summary>
public sealed class RegisterUserDeviceDto
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public MobilePlatform Platform { get; set; } = MobilePlatform.Unknown;
    public string? PushToken { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public string? AppVersion { get; set; }
    public string? DeviceModel { get; set; }
}

/// <summary>
/// Output DTO returned after successful device registration/upsert.
/// </summary>
public sealed class RegisterUserDeviceResultDto
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; set; }
}
