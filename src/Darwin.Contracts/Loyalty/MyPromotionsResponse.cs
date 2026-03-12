using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload for consumer promotions feed.
    /// </summary>
    public sealed class MyPromotionsResponse
    {
        public List<PromotionFeedItem> Items { get; init; } = new();

        /// <summary>
        /// Effective policy applied by the server for this response.
        /// </summary>
        public PromotionFeedPolicy AppliedPolicy { get; init; } = new();

        /// <summary>
        /// Optional diagnostics snapshot describing how feed guardrails changed candidate counts.
        /// </summary>
        public PromotionFeedDiagnostics Diagnostics { get; init; } = new();
    }
}
