using System;

namespace Darwin.Application.Businesses.DTOs
{
    public sealed class ChannelDispatchAuditListItemDto
    {
        public Guid Id { get; set; }
        public bool IsQueueOperation { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string RecipientAddress { get; set; } = string.Empty;
        public string? IntendedRecipientAddress { get; set; }
        public string MessagePreview { get; set; } = string.Empty;
        public string? ProviderMessageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public int QueueAttemptCount { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
        public int ChainAttemptCount { get; set; }
        public string ChainStatusMix { get; set; } = string.Empty;
        public int PriorAttemptCount { get; set; }
        public int PriorFailureCount { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public bool CanRerunNow { get; set; }
        public string ActionPolicyState { get; set; } = string.Empty;
        public string? ActionBlockedReason { get; set; }
        public DateTime? ActionAvailableAtUtc { get; set; }
        public bool NeedsEscalationReview { get; set; }
        public string? EscalationReason { get; set; }
        public int ProviderRecentAttemptCount24h { get; set; }
        public int ProviderFailureCount24h { get; set; }
        public string ProviderPressureState { get; set; } = string.Empty;
        public string ProviderRecoveryState { get; set; } = string.Empty;
        public DateTime? ProviderLastSuccessfulAttemptAtUtc { get; set; }
    }

    public sealed class ChannelDispatchAuditSummaryDto
    {
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int QueuedPendingCount { get; set; }
        public int QueuedFailedCount { get; set; }
        public int Recent24HourCount { get; set; }
        public int SmsCount { get; set; }
        public int WhatsAppCount { get; set; }
        public int PhoneVerificationCount { get; set; }
        public int AdminTestCount { get; set; }
        public int RepeatedFailureCount { get; set; }
        public int PriorSuccessContextCount { get; set; }
        public int ActionReadyCount { get; set; }
        public int ActionBlockedCount { get; set; }
        public int EscalationCandidateCount { get; set; }
        public int HeavyChainCount { get; set; }
        public int ProviderReviewCount { get; set; }
        public int ProviderRecoveredCount { get; set; }
    }

    public sealed class ChannelDispatchAuditFilterDto
    {
        public string Channel { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public bool FailedOnly { get; set; }
        public bool PhoneVerificationOnly { get; set; }
        public bool AdminTestOnly { get; set; }
        public bool RepeatedFailuresOnly { get; set; }
        public bool PriorSuccessOnly { get; set; }
        public bool ActionReadyOnly { get; set; }
        public bool ActionBlockedOnly { get; set; }
        public bool EscalationCandidatesOnly { get; set; }
        public bool HeavyChainsOnly { get; set; }
        public bool ProviderReviewOnly { get; set; }
        public bool ChainFollowUpOnly { get; set; }
        public bool ChainResolvedOnly { get; set; }
    }

    public sealed class ChannelDispatchAuditChainSummaryDto
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
        public string RecommendedAction { get; set; } = string.Empty;
        public string EscalationHint { get; set; } = string.Empty;
        public List<ChannelDispatchAuditChainHistoryItemDto> RecentHistory { get; set; } = new();
    }

    public sealed class ChannelDispatchProviderSummaryDto
    {
        public string Provider { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public int RecentAttemptCount24h { get; set; }
        public int FailureCount24h { get; set; }
        public int SentCount24h { get; set; }
        public int PendingCount24h { get; set; }
        public string PressureState { get; set; } = string.Empty;
        public string RecoveryState { get; set; } = string.Empty;
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public string EscalationHint { get; set; } = string.Empty;
    }

    public sealed class ChannelDispatchAuditChainHistoryItemDto
    {
        public DateTime AttemptedAtUtc { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public string MessagePreview { get; set; } = string.Empty;
        public string? IntendedRecipientAddress { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? FailureMessage { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
