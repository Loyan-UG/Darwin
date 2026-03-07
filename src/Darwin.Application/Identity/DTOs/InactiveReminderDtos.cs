using System;

namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Input parameters for selecting inactive reminder candidates.
/// </summary>
public sealed class GetInactiveReminderCandidatesDto
{
    /// <summary>
    /// Minimum inactivity age in days required to become a candidate.
    /// </summary>
    public int InactiveThresholdDays { get; set; } = 14;

    /// <summary>
    /// Minimum cooldown period after the last reminder dispatch.
    /// </summary>
    public int CooldownHours { get; set; } = 72;

    /// <summary>
    /// Maximum number of candidates to return in one query call.
    /// </summary>
    public int MaxItems { get; set; } = 200;
}

/// <summary>
/// Candidate row used by reminder orchestration workers/jobs.
/// </summary>
public sealed class InactiveReminderCandidateDto
{
    public Guid UserId { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public int InactiveDays { get; set; }
    public DateTime? LastReminderSentAtUtc { get; set; }
    public DateTime? CooldownEndsAtUtc { get; set; }
    public string? PushDestinationDeviceId { get; set; }
    public string? PushToken { get; set; }
    public string Platform { get; set; } = "Unknown";
}
