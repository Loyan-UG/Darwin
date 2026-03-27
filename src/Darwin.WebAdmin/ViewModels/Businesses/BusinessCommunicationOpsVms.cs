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
        public BusinessCommunicationOpsTransportVm Transport { get; set; } = new();
        public BusinessCommunicationOpsSummaryPanelVm Summary { get; set; } = new();
        public List<BusinessCommunicationSetupListItemVm> Items { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class BusinessCommunicationOpsTransportVm
    {
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
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
}
