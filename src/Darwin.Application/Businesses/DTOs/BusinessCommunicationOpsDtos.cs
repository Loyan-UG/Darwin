namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Aggregated communication-readiness counts used by WebAdmin operational dashboards.
    /// </summary>
    public sealed class BusinessCommunicationOpsSummaryDto
    {
        public int BusinessesWithCustomerEmailNotificationsEnabledCount { get; set; }
        public int BusinessesWithMarketingEmailsEnabledCount { get; set; }
        public int BusinessesWithOperationalAlertEmailsEnabledCount { get; set; }
        public int BusinessesMissingSupportEmailCount { get; set; }
        public int BusinessesMissingSenderIdentityCount { get; set; }
        public int BusinessesRequiringEmailSetupCount { get; set; }
    }
}
