using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Identity.DTOs;

public sealed class MobileDeviceOpsSummaryDto
{
    public int TotalActiveDevices { get; set; }
    public int BusinessMemberDevicesCount { get; set; }
    public int StaleDevicesCount { get; set; }
    public int DevicesMissingPushTokenCount { get; set; }
    public int NotificationsDisabledCount { get; set; }
    public int AndroidDevicesCount { get; set; }
    public int IosDevicesCount { get; set; }
    public IReadOnlyList<MobileAppVersionSnapshotDto> RecentVersions { get; set; } = Array.Empty<MobileAppVersionSnapshotDto>();
}

public sealed class MobileAppVersionSnapshotDto
{
    public MobilePlatform Platform { get; set; }
    public string AppVersion { get; set; } = string.Empty;
    public int DeviceCount { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

public sealed class MobileDeviceOpsListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public MobilePlatform Platform { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceModel { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool HasPushToken { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public int BusinessMembershipCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
