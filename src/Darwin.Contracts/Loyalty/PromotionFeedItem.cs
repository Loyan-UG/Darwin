using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// A single promotion card displayed in consumer Feed.
    /// Supports both legacy derived cards and campaign-driven cards.
    /// </summary>
    public sealed class PromotionFeedItem
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string CtaKind { get; init; } = "OpenRewards";
        public int Priority { get; init; }

        /// <summary>
        /// Optional campaign identifier when the card originates from a campaign model.
        /// Null means a derived non-campaign card.
        /// </summary>
        public Guid? CampaignId { get; init; }

        /// <summary>
        /// Campaign lifecycle state exposed to clients for UX gating.
        /// Defaults to Active for backward-compatible behavior.
        /// </summary>
        public string CampaignState { get; init; } = PromotionCampaignState.Active;

        /// <summary>
        /// Optional campaign start window (UTC).
        /// </summary>
        public DateTime? StartsAtUtc { get; init; }

        /// <summary>
        /// Optional campaign end window (UTC).
        /// </summary>
        public DateTime? EndsAtUtc { get; init; }

        /// <summary>
        /// Optional collection of audience/eligibility summaries used by clients for labels or filtering.
        /// </summary>
        public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();
    }
}
