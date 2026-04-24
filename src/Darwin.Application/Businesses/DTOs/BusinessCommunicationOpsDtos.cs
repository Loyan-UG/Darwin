namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Queue filters for phase-1 business communication setup work.
    /// </summary>
    public enum BusinessCommunicationSetupFilter
    {
        NeedsSetup = 0,
        MissingSupportEmail = 1,
        MissingSenderIdentity = 2,
        TransactionalEnabled = 3,
        MarketingEnabled = 4,
        OperationalAlertsEnabled = 5,
        All = 6
    }

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
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
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

    /// <summary>
    /// Read-only communication profile used to troubleshoot business-level setup readiness.
    /// </summary>
    public sealed class BusinessCommunicationProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? ContactEmail { get; set; }
        public string DefaultCulture { get; set; } = string.Empty;
        public string DefaultTimeZoneId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string OperationalStatus { get; set; } = string.Empty;
        public string? SupportEmail { get; set; }
        public string? CommunicationSenderName { get; set; }
        public string? CommunicationReplyToEmail { get; set; }
        public bool CustomerEmailNotificationsEnabled { get; set; }
        public bool CustomerMarketingEmailsEnabled { get; set; }
        public bool OperationalAlertEmailsEnabled { get; set; }
        public bool MissingSupportEmail { get; set; }
        public bool MissingSenderIdentity { get; set; }
        public int PendingInvitationCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
    }
}
