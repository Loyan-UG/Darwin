using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Admin
{
    /// <summary>
    /// Represents the lightweight operational dashboard shown on the WebAdmin home screen.
    /// </summary>
    public sealed class AdminDashboardVm
    {
        /// <summary>
        /// Gets or sets the CRM summary metrics rendered on the dashboard.
        /// </summary>
        public CrmSummaryVm Crm { get; set; } = new();

        /// <summary>
        /// Gets or sets the top-level dashboard KPI cards.
        /// </summary>
        public IReadOnlyList<DashboardKpiVm> Kpis { get; set; } = Array.Empty<DashboardKpiVm>();

        /// <summary>
        /// Gets or sets the prioritized issues rendered in the dashboard attention panel.
        /// </summary>
        public IReadOnlyList<DashboardAttentionItemVm> AttentionItems { get; set; } = Array.Empty<DashboardAttentionItemVm>();

        /// <summary>
        /// Gets or sets compact summaries for module workspaces.
        /// </summary>
        public IReadOnlyList<DashboardModuleSummaryVm> ModuleSummaries { get; set; } = Array.Empty<DashboardModuleSummaryVm>();

        /// <summary>
        /// Gets or sets the selected-business operational context, when a business is selected.
        /// </summary>
        public DashboardBusinessContextVm? BusinessContext { get; set; }

        /// <summary>
        /// Gets or sets the total number of active businesses available to the back-office.
        /// </summary>
        public int BusinessCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of products.
        /// </summary>
        public int ProductCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of CMS pages.
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of orders.
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Gets or sets the count of non-finalized orders.
        /// </summary>
        public int OpenOrderCount { get; set; }

        /// <summary>
        /// Gets or sets orders with failed payments.
        /// </summary>
        public int OrderPaymentIssueCount { get; set; }

        /// <summary>
        /// Gets or sets paid or partially shipped orders that need fulfilment attention.
        /// </summary>
        public int OrderFulfillmentAttentionCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of admin-manageable users.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Gets or sets the selected business context identifier for business-scoped metrics.
        /// </summary>
        public Guid? SelectedBusinessId { get; set; }

        /// <summary>
        /// Gets or sets the display label of the selected business context.
        /// </summary>
        public string SelectedBusinessLabel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payments count for the selected business context.
        /// </summary>
        public int? PaymentCount { get; set; }

        /// <summary>
        /// Gets or sets the warehouse count for the selected business context.
        /// </summary>
        public int? WarehouseCount { get; set; }

        /// <summary>
        /// Gets or sets the supplier count for the selected business context.
        /// </summary>
        public int? SupplierCount { get; set; }

        /// <summary>
        /// Gets or sets the purchase order count for the selected business context.
        /// </summary>
        public int? PurchaseOrderCount { get; set; }

        /// <summary>
        /// Gets or sets the loyalty-account count for the selected business context.
        /// </summary>
        public int? LoyaltyAccountCount { get; set; }

        /// <summary>
        /// Gets or sets the pending-redemption count for the selected business context.
        /// </summary>
        public int? PendingRedemptionCount { get; set; }

        /// <summary>
        /// Gets or sets the recent scan-session count for the selected business context.
        /// </summary>
        public int? ScanSessionCount { get; set; }

        /// <summary>
        /// Gets or sets the total active mobile-device count.
        /// </summary>
        public int MobileActiveDeviceCount { get; set; }

        /// <summary>
        /// Gets or sets the stale mobile-device count.
        /// </summary>
        public int MobileStaleDeviceCount { get; set; }

        /// <summary>
        /// Gets or sets the count of devices missing push tokens.
        /// </summary>
        public int MobileMissingPushTokenCount { get; set; }

        /// <summary>
        /// Gets or sets business-support queue metrics used by onboarding and support operators.
        /// </summary>
        public BusinessSupportSummaryVm BusinessSupport { get; set; } = new();

        /// <summary>
        /// Gets or sets communication-readiness metrics used by operators.
        /// </summary>
        public BusinessCommunicationOpsSummaryVm CommunicationOps { get; set; } = new();

        /// <summary>
        /// Gets or sets the business selector options shown on the dashboard.
        /// </summary>
        public IReadOnlyList<SelectListItem> BusinessOptions { get; set; } = Array.Empty<SelectListItem>();
    }

    /// <summary>
    /// Compact dashboard KPI card.
    /// </summary>
    public sealed class DashboardKpiVm
    {
        public string LabelKey { get; set; } = string.Empty;
        public int Count { get; set; }
        public string BadgeKey { get; set; } = string.Empty;
        public string BadgeCssClass { get; set; } = "text-bg-secondary";
        public string ActionLabelKey { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public object? RouteValues { get; set; }
    }

    /// <summary>
    /// Prioritized dashboard attention item.
    /// </summary>
    public sealed class DashboardAttentionItemVm
    {
        public string Severity { get; set; } = "info";
        public string TitleKey { get; set; } = string.Empty;
        public int Count { get; set; }
        public string ModuleKey { get; set; } = string.Empty;
        public string ActionLabelKey { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public object? RouteValues { get; set; }
    }

    /// <summary>
    /// Compact module workspace summary.
    /// </summary>
    public sealed class DashboardModuleSummaryVm
    {
        public string TitleKey { get; set; } = string.Empty;
        public IReadOnlyList<DashboardMetricVm> Metrics { get; set; } = Array.Empty<DashboardMetricVm>();
        public string PrimaryActionLabelKey { get; set; } = string.Empty;
        public string PrimaryController { get; set; } = string.Empty;
        public string PrimaryAction { get; set; } = "Index";
        public object? PrimaryRouteValues { get; set; }
        public string? SecondaryActionLabelKey { get; set; }
        public string? SecondaryController { get; set; }
        public string? SecondaryAction { get; set; }
        public object? SecondaryRouteValues { get; set; }
    }

    /// <summary>
    /// Label/value metric used by compact dashboard cards.
    /// </summary>
    public sealed class DashboardMetricVm
    {
        public string LabelKey { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Selected-business dashboard context row set.
    /// </summary>
    public sealed class DashboardBusinessContextVm
    {
        public string BusinessName { get; set; } = string.Empty;
        public IReadOnlyList<DashboardModuleSummaryVm> Rows { get; set; } = Array.Empty<DashboardModuleSummaryVm>();
    }

    /// <summary>
    /// Support-focused business onboarding and member-help summary.
    /// </summary>
    public sealed class BusinessSupportSummaryVm
    {
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int SuspendedBusinessCount { get; set; }
        public int MissingOwnerBusinessCount { get; set; }
        public int PendingInvitationCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
        public int SelectedBusinessPendingInvitationCount { get; set; }
        public int SelectedBusinessOpenInvitationCount { get; set; }
        public int SelectedBusinessPendingActivationCount { get; set; }
        public int SelectedBusinessLockedMemberCount { get; set; }
        public int SelectedBusinessFailedInvitationCount { get; set; }
        public int SelectedBusinessFailedActivationCount { get; set; }
        public int SelectedBusinessFailedPasswordResetCount { get; set; }
        public int SelectedBusinessFailedAdminTestCount { get; set; }
    }

    /// <summary>
    /// Global and business-level communication readiness snapshot for the dashboard.
    /// </summary>
    public sealed class BusinessCommunicationOpsSummaryVm
    {
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
        public int TransactionalEmailBusinessesCount { get; set; }
        public int MarketingEmailBusinessesCount { get; set; }
        public int OperationalAlertBusinessesCount { get; set; }
        public int MissingSupportEmailCount { get; set; }
        public int MissingSenderIdentityCount { get; set; }
        public int BusinessesRequiringEmailSetupCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
    }
}
