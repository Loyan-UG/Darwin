using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Notifications;

/// <summary>
/// Internal inbox message shared by one or more recipients.
/// </summary>
public sealed class NotificationMessage : BaseEntity
{
    public NotificationCategory Category { get; set; } = NotificationCategory.System;

    public NotificationTargetApp TargetApp { get; set; } = NotificationTargetApp.Both;

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? DeepLink { get; set; }

    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
