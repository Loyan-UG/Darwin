using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload for creating a business-scoped campaign.
    /// </summary>
    public sealed class CreateBusinessCampaignRequest
    {
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
        public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();
        public string PayloadJson { get; init; } = "{}";
    }
}
