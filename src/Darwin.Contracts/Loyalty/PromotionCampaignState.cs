namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Defines lifecycle states for campaign-driven promotions surfaced to mobile clients.
    /// </summary>
    public static class PromotionCampaignState
    {
        /// <summary>
        /// Draft campaign that is not visible to end users.
        /// </summary>
        public const string Draft = "Draft";

        /// <summary>
        /// Scheduled campaign waiting for its start window.
        /// </summary>
        public const string Scheduled = "Scheduled";

        /// <summary>
        /// Active campaign eligible for feed delivery.
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// Campaign that finished or was deactivated.
        /// </summary>
        public const string Expired = "Expired";
    }
}
