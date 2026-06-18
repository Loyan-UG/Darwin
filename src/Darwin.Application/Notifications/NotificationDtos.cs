using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Notifications;

public sealed class NotificationInboxPageDto
{
    public IReadOnlyList<NotificationInboxItemDto> Items { get; init; } = Array.Empty<NotificationInboxItemDto>();
    public int Total { get; init; }
}

public sealed class NotificationInboxQueryDto
{
    public NotificationTargetApp TargetApp { get; init; } = NotificationTargetApp.Both;
    public NotificationCategory? Category { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
}

public sealed class NotificationInboxItemDto
{
    public Guid Id { get; init; }
    public Guid MessageId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public NotificationCategory Category { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public string? DeepLink { get; init; }
    public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
}
