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
        /// <summary>
        /// Enables deterministic de-duplication of cards by a stable business/title/cta key.
        /// </summary>
        public bool EnableDeduplication { get; init; } = true;

        /// <summary>
        /// Maximum number of cards allowed in the final response after sorting and guardrails.
        /// </summary>
        public int MaxCards { get; init; } = 6;

        /// <summary>
        /// Optional explicit frequency window for campaign-card suppression.
        /// When present, runtime suppression uses this value before considering <see cref="SuppressionWindowMinutes"/>.
        /// </summary>
        public int? FrequencyWindowMinutes { get; init; }

        /// <summary>
        /// Legacy suppression window kept for backward compatibility with older clients.
        /// Runtime suppression uses this value only when <see cref="FrequencyWindowMinutes"/> is not provided.
        /// </summary>
        public int? SuppressionWindowMinutes { get; init; } = 480;
    }

    public sealed class PromotionFeedDiagnosticsDto
    {
        /// <summary>Number of candidate cards before any guardrail filtering starts.</summary>
        public int InitialCandidates { get; init; }

        /// <summary>Number of campaign cards removed by frequency/suppression checks.</summary>
        public int SuppressedByFrequency { get; init; }

        /// <summary>Number of cards removed by de-duplication logic.</summary>
        public int Deduplicated { get; init; }

        /// <summary>Number of cards removed by the final max-card cap.</summary>
        public int TrimmedByCap { get; init; }

        /// <summary>Final number of cards returned to clients.</summary>
        public int FinalCount { get; init; }
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
        public PromotionFeedDiagnosticsDto Diagnostics { get; init; } = new();
    }
}
