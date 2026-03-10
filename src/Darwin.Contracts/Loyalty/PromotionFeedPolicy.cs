namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Defines optional server-side delivery guardrails for promotions feed generation.
    /// </summary>
    public sealed class PromotionFeedPolicy
    {
        /// <summary>
        /// Enables deterministic de-duplication by business/title/CTA key.
        /// </summary>
        public bool EnableDeduplication { get; init; } = true;

        /// <summary>
        /// Maximum number of cards emitted by the server after policy application.
        /// </summary>
        public int MaxCards { get; init; } = 6;

        /// <summary>
        /// Optional suppression window in minutes for recently delivered campaign cards.
        /// Null keeps server default behavior.
        /// </summary>
        public int? SuppressionWindowMinutes { get; init; } = 480;
    }
}
