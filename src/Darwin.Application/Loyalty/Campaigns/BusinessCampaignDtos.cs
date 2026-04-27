using System;
using System.Collections.Generic;

namespace Darwin.Application.Loyalty.Campaigns
{
    public enum LoyaltyCampaignQueueFilter
    {
        All = 0,
        Active = 1,
        Scheduled = 2,
        Draft = 3,
        Expired = 4,
        PushEnabled = 5
    }

    public enum LoyaltyCampaignDeliveryQueueFilter
    {
        All = 0,
        Pending = 1,
        InProgress = 2,
        Failed = 3,
        Succeeded = 4,
        Cancelled = 5,
        NeedsAttention = 6
    }

    public sealed class BusinessCampaignItemDto
    {
        public Guid Id { get; init; }
        public Guid BusinessId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public string? Body { get; init; }
        public string? MediaUrl { get; init; }
        public string? LandingUrl { get; init; }
        public short Channels { get; init; }
        public DateTime? StartsAtUtc { get; init; }
        public DateTime? EndsAtUtc { get; init; }
        public bool IsActive { get; init; }
        public string CampaignState { get; init; } = "Draft";
        public string TargetingJson { get; init; } = "{}";
        public List<PromotionEligibilityRuleDto> EligibilityRules { get; init; } = new();
        public string PayloadJson { get; init; } = "{}";
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }

    public sealed class BusinessCampaignOpsSummaryDto
    {
        public int TotalCount { get; init; }
        public int ActiveCount { get; init; }
        public int ScheduledCount { get; init; }
        public int DraftCount { get; init; }
        public int ExpiredCount { get; init; }
        public int PushEnabledCount { get; init; }
    }

    public sealed class CampaignDeliveryItemDto
    {
        public Guid Id { get; init; }
        public Guid CampaignId { get; init; }
        public string CampaignName { get; init; } = string.Empty;
        public string CampaignTitle { get; init; } = string.Empty;
        public Guid? RecipientUserId { get; init; }
        public Guid? BusinessId { get; init; }
        public short Channel { get; init; }
        public short Status { get; init; }
        public string? Destination { get; init; }
        public int AttemptCount { get; init; }
        public DateTime? FirstAttemptAtUtc { get; init; }
        public DateTime? LastAttemptAtUtc { get; init; }
        public int? LastResponseCode { get; init; }
        public string? ProviderMessageId { get; init; }
        public string? LastError { get; init; }
        public string? IdempotencyKey { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }

    public sealed class CampaignDeliveryOpsSummaryDto
    {
        public int TotalCount { get; init; }
        public int PendingCount { get; init; }
        public int InProgressCount { get; init; }
        public int FailedCount { get; init; }
        public int SucceededCount { get; init; }
        public int CancelledCount { get; init; }
        public int NeedsAttentionCount { get; init; }
    }

    public sealed class GetCampaignDeliveriesResultDto
    {
        public List<CampaignDeliveryItemDto> Items { get; init; } = new();
        public int Total { get; init; }
    }


    public sealed class PromotionEligibilityRuleDto
    {
        public string AudienceKind { get; init; } = "JoinedMembers";
        public int? MinPoints { get; init; }
        public int? MaxPoints { get; init; }
        public string? TierKey { get; init; }
        public string? Note { get; init; }
    }

    public sealed class GetBusinessCampaignsResultDto
    {
        public List<BusinessCampaignItemDto> Items { get; init; } = new();
        public int Total { get; init; }
    }

    public sealed class CreateBusinessCampaignDto
    {
        public Guid BusinessId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public string? Body { get; init; }
        public string? MediaUrl { get; init; }
        public string? LandingUrl { get; init; }
        public short Channels { get; init; } = 1;
        public DateTime? StartsAtUtc { get; init; }
        public DateTime? EndsAtUtc { get; init; }
        public string TargetingJson { get; init; } = "{}";
        public List<PromotionEligibilityRuleDto> EligibilityRules { get; init; } = new();
        public string PayloadJson { get; init; } = "{}";
    }

    public sealed class UpdateBusinessCampaignDto
    {
        public Guid BusinessId { get; init; }
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public string? Body { get; init; }
        public string? MediaUrl { get; init; }
        public string? LandingUrl { get; init; }
        public short Channels { get; init; } = 1;
        public DateTime? StartsAtUtc { get; init; }
        public DateTime? EndsAtUtc { get; init; }
        public string TargetingJson { get; init; } = "{}";
        public List<PromotionEligibilityRuleDto> EligibilityRules { get; init; } = new();
        public string PayloadJson { get; init; } = "{}";
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }

    public sealed class SetCampaignActivationDto
    {
        public Guid BusinessId { get; init; }
        public Guid Id { get; init; }
        public bool IsActive { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }

    public sealed class UpdateCampaignDeliveryStatusDto
    {
        public Guid Id { get; init; }
        public Guid? BusinessId { get; init; }
        public short Status { get; init; }
        public string? OperatorNote { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }
}
