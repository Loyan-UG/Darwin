using System;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Lightweight row for phase-1 email delivery audit visibility.
    /// </summary>
    public sealed class EmailDispatchAuditListItemDto
    {
        public Guid Id { get; set; }
        public bool IsQueueOperation { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string? IntendedRecipientEmail { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? ProviderMessageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public int QueueAttemptCount { get; set; }
        public int AttemptAgeMinutes { get; set; }
        public int? CompletionLatencySeconds { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
        public string Severity { get; set; } = string.Empty;
        public bool CanRetryNow { get; set; }
        public string RetryPolicyState { get; set; } = string.Empty;
        public string? RetryBlockedReason { get; set; }
        public DateTime? RetryAvailableAtUtc { get; set; }
        public int RecentAttemptCount24h { get; set; }
        public DateTime? ChainStartedAtUtc { get; set; }
        public DateTime? ChainLastAttemptAtUtc { get; set; }
        public int? ChainSpanHours { get; set; }
        public string ChainStatusMix { get; set; } = string.Empty;
        public int PriorAttemptCount { get; set; }
        public int PriorFailureCount { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
    }

    public sealed class RetryEmailDispatchAuditDto
    {
        public Guid AuditId { get; set; }
    }

    public sealed class EmailDispatchAuditChainSummaryDto
    {
        public int TotalAttempts { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; }
        public int NeedsOperatorFollowUpCount { get; set; }
        public DateTime? FirstAttemptAtUtc { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public string StatusMix { get; set; } = string.Empty;
        public List<EmailDispatchAuditChainHistoryItemDto> RecentHistory { get; set; } = new();
    }

    public sealed class EmailDispatchAuditChainHistoryItemDto
    {
        public DateTime AttemptedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? IntendedRecipientEmail { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? FailureMessage { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
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
        public int RetriedFlowCount { get; set; }
        public int PriorSuccessContextCount { get; set; }
        public int RepeatedFailureCount { get; set; }
        public int RetryReadyCount { get; set; }
        public int RetryBlockedCount { get; set; }
        public int HighChainVolumeCount { get; set; }
        public int QueuedPendingCount { get; set; }
        public int QueuedFailedCount { get; set; }
    }
}
