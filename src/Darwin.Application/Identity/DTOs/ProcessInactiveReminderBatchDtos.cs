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
    public int CandidatesEvaluated { get; set; }
    public int DispatchedCount { get; set; }
    public int SuppressedCount { get; set; }
    public int FailedCount { get; set; }
}
