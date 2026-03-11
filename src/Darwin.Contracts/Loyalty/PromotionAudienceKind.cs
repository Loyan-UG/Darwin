namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Defines high-level audience segmentation labels exposed to clients.
    /// </summary>
    public static class PromotionAudienceKind
    {
        /// <summary>
        /// Audience includes all joined members of the scoped business/program.
        /// </summary>
        public const string JoinedMembers = "JoinedMembers";

        /// <summary>
        /// Audience targets members in a specific tier or segment.
        /// </summary>
        public const string TierSegment = "TierSegment";

        /// <summary>
        /// Audience targets members based on points threshold rules.
        /// </summary>
        public const string PointsThreshold = "PointsThreshold";

        /// <summary>
        /// Audience targets members inside an explicit campaign date window.
        /// </summary>
        public const string DateWindow = "DateWindow";
    }
}
