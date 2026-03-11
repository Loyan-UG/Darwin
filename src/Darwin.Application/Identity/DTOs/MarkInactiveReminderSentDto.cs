using System;

namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Captures a successful inactive reminder dispatch for cooldown tracking.
/// </summary>
public sealed class MarkInactiveReminderSentDto
{
    /// <summary>
    /// Target user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// UTC timestamp of dispatch success.
    /// </summary>
    public DateTime? SentAtUtc { get; set; }
}

