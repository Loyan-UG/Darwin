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
    /// Support-focused business onboarding and member-help summary.
    /// </summary>
    public sealed class BusinessSupportSummaryVm
    {
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int SuspendedBusinessCount { get; set; }
        public int MissingOwnerBusinessCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int SelectedBusinessOpenInvitationCount { get; set; }
        public int SelectedBusinessPendingActivationCount { get; set; }
        public int SelectedBusinessLockedMemberCount { get; set; }
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
    }
}
