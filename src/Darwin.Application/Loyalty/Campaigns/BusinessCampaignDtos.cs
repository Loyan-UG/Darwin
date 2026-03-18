using System;
using System.Collections.Generic;

namespace Darwin.Application.Loyalty.Campaigns
{
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
}
