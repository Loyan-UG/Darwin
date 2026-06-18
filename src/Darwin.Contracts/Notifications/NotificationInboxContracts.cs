using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Notifications;

/// <summary>
/// Client-facing app discriminator for the internal notification inbox.
/// </summary>
public enum NotificationInboxTargetApp : short
{
    Both = 0,
    Consumer = 1,
    Business = 2
}

/// <summary>
/// Client-facing notification category.
/// </summary>
public enum NotificationInboxCategory : short
{
    System = 0,
    Campaign = 1,
    Reward = 2,
    Billing = 3,
    ScannerSession = 4,
    Account = 5
}

public sealed class NotificationInboxItem
{
    public Guid Id { get; init; }
    public Guid MessageId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Body { get; init; }
    public NotificationInboxCategory Category { get; init; } = NotificationInboxCategory.System;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public string? DeepLink { get; init; }
    public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
}

public sealed class NotificationInboxListResponse
{
    public IReadOnlyList<NotificationInboxItem> Items { get; init; } = Array.Empty<NotificationInboxItem>();
    public int Total { get; init; }
}

public sealed class NotificationUnreadCountResponse
{
    public int UnreadCount { get; init; }
}

public sealed class NotificationReadResponse
{
    public int UnreadCount { get; init; }
}
