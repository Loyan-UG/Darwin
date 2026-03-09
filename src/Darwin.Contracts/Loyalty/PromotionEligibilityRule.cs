namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Represents a normalized eligibility summary for a promotion card.
    /// This object is intentionally compact to keep mobile payloads light.
    /// </summary>
    public sealed class PromotionEligibilityRule
    {
        /// <summary>
        /// Audience kind label (for example: JoinedMembers, TierSegment, PointsThreshold, DateWindow).
        /// </summary>
        public string AudienceKind { get; init; } = PromotionAudienceKind.JoinedMembers;

        /// <summary>
        /// Optional minimum points required to become eligible.
        /// </summary>
        public int? MinPoints { get; init; }

        /// <summary>
        /// Optional maximum points allowed by the audience rule.
        /// </summary>
        public int? MaxPoints { get; init; }

        /// <summary>
        /// Optional tier key used when the audience is tier-based.
        /// </summary>
        public string? TierKey { get; init; }

        /// <summary>
        /// Optional human-readable rule note for explanatory UI.
        /// </summary>
        public string? Note { get; init; }
    }
}
