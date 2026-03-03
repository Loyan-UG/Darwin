using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload for consumer promotions feed.
    /// </summary>
    public sealed class MyPromotionsResponse
    {
        public List<PromotionFeedItem> Items { get; init; } = new();
    }
}