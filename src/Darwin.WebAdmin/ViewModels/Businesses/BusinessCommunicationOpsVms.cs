using Darwin.Application.Businesses.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Businesses
{
    /// <summary>
    /// Row displayed in the business communication operations workspace.
    /// </summary>
    public sealed class BusinessCommunicationSetupListItemVm
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
    /// Read-only communication operations workspace used by WebAdmin operators.
    /// </summary>
    public sealed class BusinessCommunicationOpsVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public bool SetupOnly { get; set; } = true;
        public BusinessCommunicationSetupFilter Filter { get; set; } = BusinessCommunicationSetupFilter.NeedsSetup;
        public BusinessCommunicationOpsTransportVm Transport { get; set; } = new();
        public BusinessCommunicationOpsSummaryPanelVm Summary { get; set; } = new();
        public List<BusinessCommunicationSetupListItemVm> Items { get; set; } = new();
        public List<BuiltInCommunicationFlowVm> BuiltInFlows { get; set; } = new();
        public List<CommunicationTemplateInventoryVm> TemplateInventory { get; set; } = new();
        public List<CommunicationCapabilityCoverageVm> CapabilityCoverage { get; set; } = new();
        public List<CommunicationChannelOpsVm> ChannelOperations { get; set; } = new();
        public List<CommunicationResendPolicyVm> ResendPolicies { get; set; } = new();
        public List<EmailDispatchAuditListItemVm> RecentEmailAudits { get; set; } = new();
        public ChannelDispatchAuditSummaryVm ChannelAuditSummary { get; set; } = new();
        public List<ChannelDispatchAuditListItemVm> RecentChannelAudits { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class BusinessCommunicationOpsTransportVm
    {
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
        public bool TransactionalSubjectPrefixConfigured { get; set; }
        public bool TestInboxConfigured { get; set; }
        public bool SmsTestRecipientConfigured { get; set; }
        public bool WhatsAppTestRecipientConfigured { get; set; }
        public string? TransactionalSubjectPrefix { get; set; }
        public string? TestInboxEmail { get; set; }
        public string? TestSmsRecipientE164 { get; set; }
        public string? TestWhatsAppRecipientE164 { get; set; }
        public bool CanSendTestEmail { get; set; }
        public bool CanSendTestSms { get; set; }
        public bool CanSendTestWhatsApp { get; set; }
    }

    public sealed class BusinessCommunicationOpsSummaryPanelVm
    {
        public int TransactionalEmailBusinessesCount { get; set; }
        public int MarketingEmailBusinessesCount { get; set; }
        public int OperationalAlertBusinessesCount { get; set; }
        public int MissingSupportEmailCount { get; set; }
        public int MissingSenderIdentityCount { get; set; }
        public int BusinessesRequiringEmailSetupCount { get; set; }
    }

    /// <summary>
    /// Describes currently implemented transactional communication flows without implying a full template engine.
    /// </summary>
    public sealed class BuiltInCommunicationFlowVm
    {
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public string DeliveryPath { get; set; } = string.Empty;
        public string CurrentImplementationStatus { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    public sealed class CommunicationCapabilityCoverageVm
    {
        public string Capability { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string OperatorVisibility { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    public sealed class CommunicationChannelOpsVm
    {
        public string Channel { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string LiveFlows { get; set; } = string.Empty;
        public string SafeOperatorActions { get; set; } = string.Empty;
        public string RiskBoundary { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    public sealed class CommunicationTemplateInventoryVm
    {
        public string FlowName { get; set; } = string.Empty;
        public string TemplateSurface { get; set; } = string.Empty;
        public string SubjectSource { get; set; } = string.Empty;
        public string BodySource { get; set; } = string.Empty;
        public string CurrentSubjectTemplate { get; set; } = string.Empty;
        public string CurrentBodyTemplate { get; set; } = string.Empty;
        public string SupportedTokens { get; set; } = string.Empty;
        public string OperatorControl { get; set; } = string.Empty;
        public string AuditFlowKey { get; set; } = string.Empty;
        public string OperatorActionLabel { get; set; } = string.Empty;
        public string OperatorActionTarget { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    public sealed class CommunicationResendPolicyVm
    {
        public string FlowName { get; set; } = string.Empty;
        public string CurrentSafeAction { get; set; } = string.Empty;
        public string GenericRetryStatus { get; set; } = string.Empty;
        public string OperatorEntryPoint { get; set; } = string.Empty;
        public string AuditFlowKey { get; set; } = string.Empty;
        public string OperatorActionLabel { get; set; } = string.Empty;
        public string OperatorActionTarget { get; set; } = string.Empty;
        public string EscalationRule { get; set; } = string.Empty;
    }

    public sealed class EmailDispatchAuditListItemVm
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public int AttemptAgeMinutes { get; set; }
        public int? CompletionLatencySeconds { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
        public string Severity { get; set; } = string.Empty;
        public bool CanRetryNow { get; set; }
        public string RetryPolicyState { get; set; } = string.Empty;
        public string? RetryBlockedReason { get; set; }
        public DateTime? RetryAvailableAtUtc { get; set; }
        public int RecentAttemptCount24h { get; set; }
        public DateTime? ChainStartedAtUtc { get; set; }
        public DateTime? ChainLastAttemptAtUtc { get; set; }
        public int? ChainSpanHours { get; set; }
        public string ChainStatusMix { get; set; } = string.Empty;
        public int PriorAttemptCount { get; set; }
        public int PriorFailureCount { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public sealed class ChannelDispatchAuditSummaryVm
    {
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int Recent24HourCount { get; set; }
        public int SmsCount { get; set; }
        public int WhatsAppCount { get; set; }
        public int PhoneVerificationCount { get; set; }
        public int AdminTestCount { get; set; }
        public int RepeatedFailureCount { get; set; }
        public int PriorSuccessContextCount { get; set; }
        public int ActionReadyCount { get; set; }
        public int ActionBlockedCount { get; set; }
    }

    public sealed class ChannelDispatchAuditListItemVm
    {
        public Guid Id { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string RecipientAddress { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public bool NeedsOperatorFollowUp { get; set; }
        public int PriorAttemptCount { get; set; }
        public int PriorFailureCount { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public bool CanRerunNow { get; set; }
        public string ActionPolicyState { get; set; } = string.Empty;
        public string? ActionBlockedReason { get; set; }
        public DateTime? ActionAvailableAtUtc { get; set; }
    }

    public sealed class ChannelDispatchAuditChainSummaryVm
    {
        public int TotalAttempts { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; }
        public int NeedsOperatorFollowUpCount { get; set; }
        public DateTime? FirstAttemptAtUtc { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public string StatusMix { get; set; } = string.Empty;
        public List<ChannelDispatchAuditChainHistoryItemVm> RecentHistory { get; set; } = new();
    }

    public sealed class ChannelDispatchAuditChainHistoryItemVm
    {
        public DateTime AttemptedAtUtc { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string? FailureMessage { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class ChannelDispatchAuditsListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public bool FailedOnly { get; set; }
        public bool PhoneVerificationOnly { get; set; }
        public bool AdminTestOnly { get; set; }
        public bool RepeatedFailuresOnly { get; set; }
        public bool PriorSuccessOnly { get; set; }
        public bool ActionReadyOnly { get; set; }
        public bool ActionBlockedOnly { get; set; }
        public bool ChainFollowUpOnly { get; set; }
        public bool ChainResolvedOnly { get; set; }
        public bool CanSendTestSms { get; set; }
        public bool CanSendTestWhatsApp { get; set; }
        public ChannelDispatchAuditSummaryVm Summary { get; set; } = new();
        public ChannelDispatchAuditChainSummaryVm? ChainSummary { get; set; }
        public List<ChannelDispatchAuditListItemVm> Items { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> ChannelItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> FlowItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> StatusItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class EmailDispatchAuditsListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public bool StalePendingOnly { get; set; }
        public bool BusinessLinkedFailuresOnly { get; set; }
        public bool RepeatedFailuresOnly { get; set; }
        public bool PriorSuccessOnly { get; set; }
        public bool RetryReadyOnly { get; set; }
        public bool RetryBlockedOnly { get; set; }
        public bool HighChainVolumeOnly { get; set; }
        public bool ChainFollowUpOnly { get; set; }
        public bool ChainResolvedOnly { get; set; }
        public Guid? BusinessId { get; set; }
        public bool CanSendTestEmail { get; set; }
        public EmailDispatchAuditSummaryVm Summary { get; set; } = new();
        public EmailDispatchAuditChainSummaryVm? ChainSummary { get; set; }
        public List<EmailDispatchAuditListItemVm> Items { get; set; } = new();
        public List<CommunicationFlowPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> StatusItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> FlowItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class EmailDispatchAuditSummaryVm
    {
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; }
        public int StalePendingCount { get; set; }
        public int BusinessLinkedFailureCount { get; set; }
        public int Recent24HourCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
        public int NeedsOperatorFollowUpCount { get; set; }
        public int SlowCompletedCount { get; set; }
        public int RetriedFlowCount { get; set; }
        public int PriorSuccessContextCount { get; set; }
        public int RepeatedFailureCount { get; set; }
        public int RetryReadyCount { get; set; }
        public int RetryBlockedCount { get; set; }
        public int HighChainVolumeCount { get; set; }
    }

    public sealed class EmailDispatchAuditChainSummaryVm
    {
        public int TotalAttempts { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; }
        public int NeedsOperatorFollowUpCount { get; set; }
        public DateTime? FirstAttemptAtUtc { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? LastSuccessfulAttemptAtUtc { get; set; }
        public string StatusMix { get; set; } = string.Empty;
        public List<EmailDispatchAuditChainHistoryItemVm> RecentHistory { get; set; } = new();
    }

    public sealed class EmailDispatchAuditChainHistoryItemVm
    {
        public DateTime AttemptedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? FailureMessage { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class CommunicationFlowPlaybookVm
    {
        public string FlowKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string AllowedAction { get; set; } = string.Empty;
        public string EscalationRule { get; set; } = string.Empty;
    }

    public sealed class BusinessCommunicationProfileVm
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
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
        public bool TransactionalSubjectPrefixConfigured { get; set; }
        public bool TestInboxConfigured { get; set; }
        public bool CanSendTestEmail { get; set; }
        public bool CanSendTestSms { get; set; }
        public bool CanSendTestWhatsApp { get; set; }
        public string? TransactionalSubjectPrefix { get; set; }
        public string? TestInboxEmail { get; set; }
        public List<string> ActiveFlowNames { get; set; } = new();
        public List<CommunicationTemplateInventoryVm> TemplateInventory { get; set; } = new();
        public List<CommunicationChannelOpsVm> ChannelOperations { get; set; } = new();
        public List<CommunicationResendPolicyVm> ResendPolicies { get; set; } = new();
        public List<string> ReadinessIssues { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public List<EmailDispatchAuditListItemVm> RecentEmailAudits { get; set; } = new();
        public ChannelDispatchAuditSummaryVm ChannelAuditSummary { get; set; } = new();
        public List<ChannelDispatchAuditListItemVm> RecentChannelAudits { get; set; } = new();
    }
}
