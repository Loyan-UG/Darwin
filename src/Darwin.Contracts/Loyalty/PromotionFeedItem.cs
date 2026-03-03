using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// A single promotion card displayed in consumer Feed.
    /// </summary>
    public sealed class PromotionFeedItem
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string CtaKind { get; init; } = "OpenRewards";
        public int Priority { get; init; }
    }
}