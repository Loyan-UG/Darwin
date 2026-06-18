using System;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Domain.Entities.Notifications;

/// <summary>
/// Per-user inbox state for a notification message.
/// </summary>
public sealed class NotificationRecipient : BaseEntity
{
    public Guid NotificationMessageId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public NotificationMessage? Message { get; set; }

    public User? User { get; set; }
}
