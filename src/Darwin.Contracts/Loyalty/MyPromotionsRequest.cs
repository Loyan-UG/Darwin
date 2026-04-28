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
        /// Maximum number of promotion items to return before server guardrails are applied.
        /// </summary>
        public int MaxItems { get; init; } = 20;

        /// <summary>
        /// Optional requested culture for user-facing promotion text.
        /// </summary>
        public string? Culture { get; init; }

        /// <summary>
        /// Optional policy override used to tune server-side guardrails.
        ///
        /// Precedence behavior for suppression controls:
        /// - When <see cref="PromotionFeedPolicy.FrequencyWindowMinutes"/> is provided,
        ///   campaign suppression uses it first.
        /// - Otherwise, server falls back to <see cref="PromotionFeedPolicy.SuppressionWindowMinutes"/>.
        ///
        /// Clients can safely omit this property to use server defaults.
        /// </summary>
        public PromotionFeedPolicy? Policy { get; init; }
    }
}
