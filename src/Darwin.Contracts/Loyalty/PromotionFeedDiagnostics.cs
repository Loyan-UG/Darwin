namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Provides observability counters for server-side promotions feed guardrails.
    /// Values are emitted per response so operations teams can validate policy outcomes.
    /// </summary>
    public sealed class PromotionFeedDiagnostics
    {
        /// <summary>
        /// Number of cards before any guardrail filters are applied.
        /// </summary>
        public int InitialCandidates { get; init; }

        /// <summary>
        /// Number of campaign cards removed by suppression/frequency window checks.
        /// </summary>
        public int SuppressedByFrequency { get; init; }

        /// <summary>
        /// Number of cards removed by de-duplication guardrail.
        /// </summary>
        public int Deduplicated { get; init; }

        /// <summary>
        /// Number of cards removed by max-card cap after ordering.
        /// </summary>
        public int TrimmedByCap { get; init; }

        /// <summary>
        /// Final number of cards in the response payload.
        /// </summary>
        public int FinalCount { get; init; }
    }
}
