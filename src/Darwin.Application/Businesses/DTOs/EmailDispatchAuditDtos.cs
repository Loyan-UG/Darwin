using System;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Lightweight row for phase-1 email delivery audit visibility.
    /// </summary>
    public sealed class EmailDispatchAuditListItemDto
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public int AttemptAgeMinutes { get; set; }
        public int? CompletionLatencySeconds { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public sealed class EmailDispatchAuditSummaryDto
    {
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; }
        public int StalePendingCount { get; set; }
        public int BusinessLinkedFailureCount { get; set; }
        public int Recent24HourCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
        public int NeedsOperatorFollowUpCount { get; set; }
        public int SlowCompletedCount { get; set; }
    }
}
