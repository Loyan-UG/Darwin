using System;

namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Captures one orchestration outcome for inactive reminder measurement.
/// </summary>
public sealed class MarkInactiveReminderAttemptDto
{
    /// <summary>
    /// Target user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// UTC timestamp of the outcome event.
    /// </summary>
    public DateTime? OccurredAtUtc { get; set; }

    /// <summary>
    /// Outcome kind. Supported values: Sent, Failed, Suppressed.
    /// </summary>
    public string Outcome { get; set; } = "Suppressed";

    /// <summary>
    /// Optional short failure/suppression code for analytics.
    /// </summary>
    public string? OutcomeCode { get; set; }
}
