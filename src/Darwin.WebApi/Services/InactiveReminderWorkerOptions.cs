using System;

namespace Darwin.WebApi.Services;

/// <summary>
/// Configuration for the periodic inactive-reminder orchestration worker.
/// </summary>
public sealed class InactiveReminderWorkerOptions
{
    /// <summary>
    /// Enables or disables the worker execution loop.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Interval between worker runs.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Minimum inactivity threshold in days.
    /// </summary>
    public int InactiveThresholdDays { get; set; } = 14;

    /// <summary>
    /// Cooldown suppression period in hours.
    /// </summary>
    public int CooldownHours { get; set; } = 72;

    /// <summary>
    /// Maximum candidates processed per run.
    /// </summary>
    public int MaxItemsPerRun { get; set; } = 200;
}
