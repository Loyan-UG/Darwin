using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload for updating mutable fields of a business campaign.
    /// </summary>
    public sealed class UpdateBusinessCampaignRequest
    {
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
        public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();
        public string PayloadJson { get; init; } = "{}";
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }
}
