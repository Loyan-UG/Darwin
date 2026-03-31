using System;

namespace Darwin.Application.Businesses.DTOs
{
    public sealed class ChannelDispatchAuditListItemDto
    {
        public Guid Id { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string RecipientAddress { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
    }

    public sealed class ChannelDispatchAuditSummaryDto
    {
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int Recent24HourCount { get; set; }
        public int SmsCount { get; set; }
        public int WhatsAppCount { get; set; }
        public int PhoneVerificationCount { get; set; }
        public int AdminTestCount { get; set; }
    }

    public sealed class ChannelDispatchAuditFilterDto
    {
        public string Channel { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public bool FailedOnly { get; set; }
        public bool PhoneVerificationOnly { get; set; }
        public bool AdminTestOnly { get; set; }
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
        public string StatusMix { get; set; } = string.Empty;
        public List<ChannelDispatchAuditChainHistoryItemDto> RecentHistory { get; set; } = new();
    }

    public sealed class ChannelDispatchAuditChainHistoryItemDto
    {
        public DateTime AttemptedAtUtc { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string? FailureMessage { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
