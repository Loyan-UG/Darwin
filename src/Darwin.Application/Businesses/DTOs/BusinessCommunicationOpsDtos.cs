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

    /// <summary>
    /// Lightweight operational row for businesses with communication configuration gaps.
    /// </summary>
    public sealed class BusinessCommunicationSetupListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? SupportEmail { get; set; }
        public string? CommunicationSenderName { get; set; }
        public string? CommunicationReplyToEmail { get; set; }
        public bool CustomerEmailNotificationsEnabled { get; set; }
        public bool CustomerMarketingEmailsEnabled { get; set; }
        public bool OperationalAlertEmailsEnabled { get; set; }
        public bool MissingSupportEmail { get; set; }
        public bool MissingSenderIdentity { get; set; }
    }
}
