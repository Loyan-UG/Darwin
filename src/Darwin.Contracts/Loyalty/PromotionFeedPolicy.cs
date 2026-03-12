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
        /// Optional frequency-cap window in minutes for campaign cards that were already delivered recently.
        ///
        /// Precedence rule:
        /// - When this field is provided by the client, server-side campaign suppression uses this value first.
        /// - When omitted, the server falls back to <see cref="SuppressionWindowMinutes"/>.
        ///
        /// Compatibility rule:
        /// - Legacy clients that only send <see cref="SuppressionWindowMinutes"/> keep existing behavior.
        /// - New clients can gradually migrate to this explicit field without breaking older integrations.
        /// </summary>
        public int? FrequencyWindowMinutes { get; init; }
            = null;

        /// <summary>
        /// Optional suppression window in minutes for recently delivered campaign cards.
        /// Null keeps server default behavior.
        /// </summary>
        public int? SuppressionWindowMinutes { get; init; } = 480;
    }
}
