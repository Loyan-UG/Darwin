using System;
using System.Collections.Generic;

namespace Darwin.Application.Loyalty.DTOs
{
    public sealed class MyPromotionsDto
    {
        public Guid? BusinessId { get; init; }
        public int MaxItems { get; init; } = 20;
        public PromotionFeedPolicyDto? Policy { get; init; }
    }

    public sealed class PromotionFeedPolicyDto
    {
        public bool EnableDeduplication { get; init; } = true;
        public int MaxCards { get; init; } = 6;
        public int? SuppressionWindowMinutes { get; init; } = 480;
    }

    public sealed class PromotionEligibilityRuleDto
    {
        public string AudienceKind { get; init; } = "JoinedMembers";
        public int? MinPoints { get; init; }
        public int? MaxPoints { get; init; }
        public string? TierKey { get; init; }
        public string? Note { get; init; }
    }

    public sealed class PromotionEligibilityRuleDto
    {
        public string AudienceKind { get; init; } = "JoinedMembers";
        public int? MinPoints { get; init; }
        public int? MaxPoints { get; init; }
        public string? TierKey { get; init; }
        public string? Note { get; init; }
    }

    public sealed class PromotionFeedItemDto
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string CtaKind { get; init; } = "OpenRewards";
        public int Priority { get; init; }
        public Guid? CampaignId { get; init; }
        public string CampaignState { get; init; } = "Active";
        public DateTime? StartsAtUtc { get; init; }
        public DateTime? EndsAtUtc { get; init; }
        public List<PromotionEligibilityRuleDto> EligibilityRules { get; init; } = new();
    }

    public sealed class MyPromotionsResultDto
    {
        public List<PromotionFeedItemDto> Items { get; init; } = new();
        public PromotionFeedPolicyDto AppliedPolicy { get; init; } = new();
    }
}
