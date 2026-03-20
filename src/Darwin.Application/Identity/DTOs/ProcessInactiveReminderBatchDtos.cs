using System;
using System.Collections.Generic;

namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Input payload for running one inactive-reminder orchestration batch.
/// </summary>
public sealed class ProcessInactiveReminderBatchDto
{
    /// <summary>
    /// Minimum inactivity threshold in days.
    /// </summary>
    public int InactiveThresholdDays { get; set; } = 14;

    /// <summary>
    /// Cooldown suppression period in hours after successful reminder send.
    /// </summary>
    public int CooldownHours { get; set; } = 72;

    /// <summary>
    /// Maximum number of candidates to process in one batch.
    /// </summary>
    public int MaxItems { get; set; } = 200;
}

/// <summary>
/// Result summary for one inactive-reminder orchestration batch.
/// </summary>
public sealed class ProcessInactiveReminderBatchResultDto
{
    /// <summary>
    /// Total number of candidates inspected during the batch.
    /// </summary>
    public int CandidatesEvaluated { get; set; }

    /// <summary>
    /// Number of reminders successfully dispatched and persisted as sent.
    /// </summary>
    public int DispatchedCount { get; set; }

    /// <summary>
    /// Total number of suppressed candidates regardless of suppression reason.
    /// </summary>
    public int SuppressedCount { get; set; }

    /// <summary>
    /// Number of candidates suppressed because cooldown policy was still active.
    /// </summary>
    public int SuppressedByCooldownCount { get; set; }

    /// <summary>
    /// Number of candidates suppressed because no push destination/token was available.
    /// </summary>
    public int SuppressedByMissingDestinationCount { get; set; }

    /// <summary>
    /// Number of dispatch attempts that ended in failure.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Per-code failure breakdown used by worker logs and remediation playbooks.
    /// </summary>
    public Dictionary<string, int> FailureCodeCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-code suppression breakdown used by worker logs and remediation playbooks.
    /// </summary>
    public Dictionary<string, int> SuppressionCodeCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
}
