using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request model for fetching personalized promotions feed items for the current consumer.
    /// </summary>
    public sealed class MyPromotionsRequest
    {
        /// <summary>
        /// Optional business filter. When null, server returns cross-business promotions across joined programs.
        /// </summary>
        public Guid? BusinessId { get; init; }

        /// <summary>
        /// Maximum number of promotion items to return.
        /// </summary>
        public int MaxItems { get; init; } = 20;
    }
}