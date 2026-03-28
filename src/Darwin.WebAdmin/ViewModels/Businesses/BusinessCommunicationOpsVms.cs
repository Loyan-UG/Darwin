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
        public List<CommunicationCapabilityCoverageVm> CapabilityCoverage { get; set; } = new();
        public List<EmailDispatchAuditListItemVm> RecentEmailAudits { get; set; } = new();
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
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public sealed class EmailDispatchAuditsListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FlowKey { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public List<EmailDispatchAuditListItemVm> Items { get; set; } = new();
        public List<CommunicationFlowPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> StatusItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> FlowItems { get; set; } = Array.Empty<SelectListItem>();
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
        public string? TransactionalSubjectPrefix { get; set; }
        public string? TestInboxEmail { get; set; }
        public List<string> ActiveFlowNames { get; set; } = new();
        public List<string> ReadinessIssues { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public List<EmailDispatchAuditListItemVm> RecentEmailAudits { get; set; } = new();
    }
}
