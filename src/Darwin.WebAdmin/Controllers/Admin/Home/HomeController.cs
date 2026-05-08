using Darwin.Application.Billing.Queries;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application.Orders.Queries;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Admin;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Darwin.Domain.Enums;

namespace Darwin.WebAdmin.Controllers.Admin
{
    /// <summary>
    /// Landing endpoint for the Admin area. The <c>Index</c> view will later evolve
    /// into the main dashboard (KPIs, quick links, system health).
    /// </summary>
    public sealed class HomeController : AdminBaseController
    {
        private readonly GetCrmSummaryHandler _getCrmSummary;
        private readonly GetOrdersPageHandler _getOrdersPage;
        private readonly GetPaymentsPageHandler _getPaymentsPage;
        private readonly GetWarehousesPageHandler _getWarehousesPage;
        private readonly GetSuppliersPageHandler _getSuppliersPage;
        private readonly GetPurchaseOrdersPageHandler _getPurchaseOrdersPage;
        private readonly GetBusinessSupportSummaryHandler _getBusinessSupportSummary;
        private readonly GetBusinessCommunicationOpsSummaryHandler _getBusinessCommunicationOpsSummary;
        private readonly GetLoyaltyAccountsPageHandler _getLoyaltyAccountsPage;
        private readonly GetLoyaltyRedemptionsPageHandler _getLoyaltyRedemptionsPage;
        private readonly GetRecentLoyaltyScanSessionsPageHandler _getLoyaltyScanSessionsPage;
        private readonly GetMobileDeviceOpsSummaryHandler _getMobileDeviceOpsSummary;
        private readonly AdminReferenceDataService _referenceData;
        private readonly ISiteSettingCache _siteSettingCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        public HomeController(
            GetCrmSummaryHandler getCrmSummary,
            GetOrdersPageHandler getOrdersPage,
            GetPaymentsPageHandler getPaymentsPage,
            GetWarehousesPageHandler getWarehousesPage,
            GetSuppliersPageHandler getSuppliersPage,
            GetPurchaseOrdersPageHandler getPurchaseOrdersPage,
            GetBusinessSupportSummaryHandler getBusinessSupportSummary,
            GetBusinessCommunicationOpsSummaryHandler getBusinessCommunicationOpsSummary,
            GetLoyaltyAccountsPageHandler getLoyaltyAccountsPage,
            GetLoyaltyRedemptionsPageHandler getLoyaltyRedemptionsPage,
            GetRecentLoyaltyScanSessionsPageHandler getLoyaltyScanSessionsPage,
            GetMobileDeviceOpsSummaryHandler getMobileDeviceOpsSummary,
            AdminReferenceDataService referenceData,
            ISiteSettingCache siteSettingCache)
        {
            _getCrmSummary = getCrmSummary ?? throw new ArgumentNullException(nameof(getCrmSummary));
            _getOrdersPage = getOrdersPage ?? throw new ArgumentNullException(nameof(getOrdersPage));
            _getPaymentsPage = getPaymentsPage ?? throw new ArgumentNullException(nameof(getPaymentsPage));
            _getWarehousesPage = getWarehousesPage ?? throw new ArgumentNullException(nameof(getWarehousesPage));
            _getSuppliersPage = getSuppliersPage ?? throw new ArgumentNullException(nameof(getSuppliersPage));
            _getPurchaseOrdersPage = getPurchaseOrdersPage ?? throw new ArgumentNullException(nameof(getPurchaseOrdersPage));
            _getBusinessSupportSummary = getBusinessSupportSummary ?? throw new ArgumentNullException(nameof(getBusinessSupportSummary));
            _getBusinessCommunicationOpsSummary = getBusinessCommunicationOpsSummary ?? throw new ArgumentNullException(nameof(getBusinessCommunicationOpsSummary));
            _getLoyaltyAccountsPage = getLoyaltyAccountsPage ?? throw new ArgumentNullException(nameof(getLoyaltyAccountsPage));
            _getLoyaltyRedemptionsPage = getLoyaltyRedemptionsPage ?? throw new ArgumentNullException(nameof(getLoyaltyRedemptionsPage));
            _getLoyaltyScanSessionsPage = getLoyaltyScanSessionsPage ?? throw new ArgumentNullException(nameof(getLoyaltyScanSessionsPage));
            _getMobileDeviceOpsSummary = getMobileDeviceOpsSummary ?? throw new ArgumentNullException(nameof(getMobileDeviceOpsSummary));
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
        }

        /// <summary>
        /// Renders the Admin dashboard with lightweight operational summaries and quick links.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(Guid? businessId = null, CancellationToken ct = default)
        {
            var (selectedBusinessId, businessOptions) = await BuildBusinessSelectionAsync(businessId, ct).ConfigureAwait(false);

            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var crmSummary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var orders = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, ct: ct).ConfigureAwait(false);
            var openOrders = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, query: null, filter: Darwin.Application.Orders.DTOs.OrderQueueFilter.Open, ct: ct).ConfigureAwait(false);
            var orderPaymentIssues = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, query: null, filter: Darwin.Application.Orders.DTOs.OrderQueueFilter.PaymentIssues, ct: ct).ConfigureAwait(false);
            var fulfillmentAttention = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, query: null, filter: Darwin.Application.Orders.DTOs.OrderQueueFilter.FulfillmentAttention, ct: ct).ConfigureAwait(false);
            var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);
            var communicationOps = await _getBusinessCommunicationOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            var mobileDeviceOps = await _getMobileDeviceOpsSummary.HandleAsync(ct).ConfigureAwait(false);

            int? paymentCount = null;
            int? warehouseCount = null;
            int? supplierCount = null;
            int? purchaseOrderCount = null;
            int? loyaltyAccountCount = null;
            int? pendingRedemptionCount = null;
            int? scanSessionCount = null;

            if (selectedBusinessId.HasValue)
            {
                paymentCount = (await _getPaymentsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: null, ct).ConfigureAwait(false)).Total;
                warehouseCount = (await _getWarehousesPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.WarehouseQueueFilter.All, ct).ConfigureAwait(false)).Total;
                supplierCount = (await _getSuppliersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.SupplierQueueFilter.All, ct).ConfigureAwait(false)).Total;
                purchaseOrderCount = (await _getPurchaseOrdersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.PurchaseOrderQueueFilter.All, ct).ConfigureAwait(false)).Total;
                loyaltyAccountCount = (await _getLoyaltyAccountsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, status: null, ct).ConfigureAwait(false)).Total;
                pendingRedemptionCount = (await _getLoyaltyRedemptionsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, status: LoyaltyRedemptionStatus.Pending, ct).ConfigureAwait(false)).Total;
                scanSessionCount = (await _getLoyaltyScanSessionsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, mode: null, status: null, ct).ConfigureAwait(false)).Total;
            }

            var mappedBusinessSupport = MapBusinessSupportSummary(businessSupport);
            var mappedCommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings);

            var vm = new AdminDashboardVm
            {
                BusinessCount = businessOptions.Count,
                BusinessOptions = businessOptions,
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                OrderCount = orders.Total,
                OpenOrderCount = openOrders.Total,
                OrderPaymentIssueCount = orderPaymentIssues.Total,
                OrderFulfillmentAttentionCount = fulfillmentAttention.Total,
                Crm = MapCrmSummary(crmSummary, siteSettings.DefaultCurrency),
                BusinessSupport = mappedBusinessSupport,
                CommunicationOps = mappedCommunicationOps,
                PaymentCount = paymentCount,
                WarehouseCount = warehouseCount,
                SupplierCount = supplierCount,
                PurchaseOrderCount = purchaseOrderCount,
                LoyaltyAccountCount = loyaltyAccountCount,
                PendingRedemptionCount = pendingRedemptionCount,
                ScanSessionCount = scanSessionCount,
                MobileActiveDeviceCount = mobileDeviceOps.TotalActiveDevices,
                MobileStaleDeviceCount = mobileDeviceOps.StaleDevicesCount,
                MobileMissingPushTokenCount = mobileDeviceOps.DevicesMissingPushTokenCount
            };

            vm.Kpis = BuildKpis(vm);
            vm.AttentionItems = BuildAttentionItems(vm);
            vm.BusinessContext = BuildBusinessContext(vm);
            vm.ModuleSummaries = BuildModuleSummaries(vm);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CommunicationOpsFragment(Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildCommunicationOpsCardVmAsync(businessId, ct).ConfigureAwait(false);
            return PartialView("~/Views/Home/_CommunicationOpsCard.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> BusinessSupportQueueFragment(Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildBusinessSupportCardVmAsync(businessId, ct).ConfigureAwait(false);
            return PartialView("~/Views/Home/_BusinessSupportQueueCard.cshtml", vm);
        }

        /// <summary>
        /// Returns the shared alerts partial so HTMX flows can refresh feedback banners without
        /// coupling the shared layout to a feature-specific controller.
        /// </summary>
        [HttpGet]
        public IActionResult AlertsFragment()
        {
            return PartialView("~/Views/Shared/_Alerts.cshtml");
        }

        private async Task<AdminDashboardVm> BuildCommunicationOpsCardVmAsync(Guid? businessId, CancellationToken ct)
        {
            var (selectedBusinessId, businessOptions) = await BuildBusinessSelectionAsync(businessId, ct).ConfigureAwait(false);

            var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);
            var communicationOps = await _getBusinessCommunicationOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);

            return new AdminDashboardVm
            {
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                BusinessSupport = MapBusinessSupportSummary(businessSupport),
                CommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings)
            };
        }

        private async Task<AdminDashboardVm> BuildBusinessSupportCardVmAsync(Guid? businessId, CancellationToken ct)
        {
            var (selectedBusinessId, businessOptions) = await BuildBusinessSelectionAsync(businessId, ct).ConfigureAwait(false);

            var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);

            return new AdminDashboardVm
            {
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                BusinessSupport = MapBusinessSupportSummary(businessSupport)
            };
        }

        private async Task<(Guid? SelectedBusinessId, List<SelectListItem> BusinessOptions)> BuildBusinessSelectionAsync(Guid? businessId, CancellationToken ct)
        {
            var selectedBusinessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId, ct).ConfigureAwait(false);
            return (selectedBusinessId, businessOptions);
        }

        private static CrmSummaryVm MapCrmSummary(CrmSummaryDto dto, string currency)
        {
            return new CrmSummaryVm
            {
                CustomerCount = dto.CustomerCount,
                LeadCount = dto.LeadCount,
                QualifiedLeadCount = dto.QualifiedLeadCount,
                OpenOpportunityCount = dto.OpenOpportunityCount,
                Currency = string.IsNullOrWhiteSpace(currency) ? string.Empty : currency.Trim().ToUpperInvariant(),
                OpenPipelineMinor = dto.OpenPipelineMinor,
                SegmentCount = dto.SegmentCount,
                RecentInteractionCount = dto.RecentInteractionCount
            };
        }

        private static BusinessSupportSummaryVm MapBusinessSupportSummary(Darwin.Application.Businesses.DTOs.BusinessSupportSummaryDto dto)
        {
            return new BusinessSupportSummaryVm
            {
                AttentionBusinessCount = dto.AttentionBusinessCount,
                PendingApprovalBusinessCount = dto.PendingApprovalBusinessCount,
                SuspendedBusinessCount = dto.SuspendedBusinessCount,
                MissingOwnerBusinessCount = dto.MissingOwnerBusinessCount,
                PendingInvitationCount = dto.PendingInvitationCount,
                OpenInvitationCount = dto.OpenInvitationCount,
                PendingActivationMemberCount = dto.PendingActivationMemberCount,
                LockedMemberCount = dto.LockedMemberCount,
                FailedInvitationCount = dto.FailedInvitationCount,
                FailedActivationCount = dto.FailedActivationCount,
                FailedPasswordResetCount = dto.FailedPasswordResetCount,
                FailedAdminTestCount = dto.FailedAdminTestCount,
                SelectedBusinessPendingInvitationCount = dto.SelectedBusinessPendingInvitationCount,
                SelectedBusinessOpenInvitationCount = dto.SelectedBusinessOpenInvitationCount,
                SelectedBusinessPendingActivationCount = dto.SelectedBusinessPendingActivationCount,
                SelectedBusinessLockedMemberCount = dto.SelectedBusinessLockedMemberCount,
                SelectedBusinessFailedInvitationCount = dto.SelectedBusinessFailedInvitationCount,
                SelectedBusinessFailedActivationCount = dto.SelectedBusinessFailedActivationCount,
                SelectedBusinessFailedPasswordResetCount = dto.SelectedBusinessFailedPasswordResetCount,
                SelectedBusinessFailedAdminTestCount = dto.SelectedBusinessFailedAdminTestCount
            };
        }

        private static BusinessCommunicationOpsSummaryVm MapCommunicationOpsSummary(
            Darwin.Application.Businesses.DTOs.BusinessCommunicationOpsSummaryDto dto,
            Darwin.Application.Settings.DTOs.SiteSettingDto siteSettings)
        {
            return new BusinessCommunicationOpsSummaryVm
            {
                EmailTransportConfigured = siteSettings.SmtpEnabled &&
                                           !string.IsNullOrWhiteSpace(siteSettings.SmtpHost) &&
                                           siteSettings.SmtpPort.HasValue &&
                                           !string.IsNullOrWhiteSpace(siteSettings.SmtpFromAddress),
                SmsTransportConfigured = siteSettings.SmsEnabled &&
                                         !string.IsNullOrWhiteSpace(siteSettings.SmsProvider) &&
                                         !string.IsNullOrWhiteSpace(siteSettings.SmsFromPhoneE164),
                WhatsAppTransportConfigured = siteSettings.WhatsAppEnabled &&
                                              !string.IsNullOrWhiteSpace(siteSettings.WhatsAppBusinessPhoneId) &&
                                              !string.IsNullOrWhiteSpace(siteSettings.WhatsAppAccessToken),
                AdminAlertRoutingConfigured = !string.IsNullOrWhiteSpace(siteSettings.AdminAlertEmailsCsv) ||
                                              !string.IsNullOrWhiteSpace(siteSettings.AdminAlertSmsRecipientsCsv),
                TransactionalEmailBusinessesCount = dto.BusinessesWithCustomerEmailNotificationsEnabledCount,
                MarketingEmailBusinessesCount = dto.BusinessesWithMarketingEmailsEnabledCount,
                OperationalAlertBusinessesCount = dto.BusinessesWithOperationalAlertEmailsEnabledCount,
                MissingSupportEmailCount = dto.BusinessesMissingSupportEmailCount,
                MissingSenderIdentityCount = dto.BusinessesMissingSenderIdentityCount,
                BusinessesRequiringEmailSetupCount = dto.BusinessesRequiringEmailSetupCount,
                FailedInvitationCount = dto.FailedInvitationCount,
                FailedActivationCount = dto.FailedActivationCount,
                FailedPasswordResetCount = dto.FailedPasswordResetCount,
                FailedAdminTestCount = dto.FailedAdminTestCount
            };
        }

        private static IReadOnlyList<DashboardKpiVm> BuildKpis(AdminDashboardVm vm)
        {
            var businessAttention = BusinessAttentionCount(vm.BusinessSupport);
            var billingAttention = (vm.PaymentCount ?? 0) + vm.OrderPaymentIssueCount;
            var communicationAttention = CommunicationAttentionCount(vm);
            var mobileAttention = MobileAttentionCount(vm);

            var kpis = new List<DashboardKpiVm>
            {
                CreateKpi("DashboardKpiBusinessesAttention", businessAttention, "Businesses", "SupportQueue", "BusinessSupportOpenQueueAction", businessAttention > 0 ? "Warning" : "Ready", businessAttention > 0 ? "text-bg-warning" : "text-bg-success"),
                CreateKpi("DashboardKpiOrdersAttention", vm.OrderFulfillmentAttentionCount, "Orders", "Index", "DashboardOpenOrdersAction", vm.OrderFulfillmentAttentionCount > 0 ? "Warning" : "Ready", vm.OrderFulfillmentAttentionCount > 0 ? "text-bg-warning" : "text-bg-success"),
                CreateKpi("DashboardKpiBillingAttention", billingAttention, "Billing", "Payments", "DashboardOpenPaymentsAction", billingAttention > 0 ? "Warning" : "Ready", billingAttention > 0 ? "text-bg-warning" : "text-bg-success"),
                CreateKpi("DashboardKpiCommunicationAttention", communicationAttention, "BusinessCommunications", "Index", "OpenWorkspaceAction", communicationAttention > 0 ? "Warning" : "Ready", communicationAttention > 0 ? "text-bg-warning" : "text-bg-success"),
                CreateKpi("DashboardKpiMobileAttention", mobileAttention, "MobileOperations", "Index", "OpenMobileOps", mobileAttention > 0 ? "Warning" : "Ready", mobileAttention > 0 ? "text-bg-warning" : "text-bg-success")
            };

            if (vm.SelectedBusinessId.HasValue)
            {
                var loyaltyAttention = vm.PendingRedemptionCount ?? 0;
                kpis.Add(CreateKpi("DashboardKpiLoyaltyAttention", loyaltyAttention, "Loyalty", "Redemptions", "Redemptions", loyaltyAttention > 0 ? "Warning" : "Ready", loyaltyAttention > 0 ? "text-bg-warning" : "text-bg-success", new { businessId = vm.SelectedBusinessId }));
            }

            return kpis;
        }

        private static DashboardKpiVm CreateKpi(
            string labelKey,
            int count,
            string controller,
            string action,
            string actionLabelKey,
            string badgeKey,
            string badgeCssClass,
            object? routeValues = null)
        {
            return new DashboardKpiVm
            {
                LabelKey = labelKey,
                Count = count,
                Controller = controller,
                Action = action,
                ActionLabelKey = actionLabelKey,
                BadgeKey = badgeKey,
                BadgeCssClass = badgeCssClass,
                RouteValues = routeValues
            };
        }

        private static IReadOnlyList<DashboardAttentionItemVm> BuildAttentionItems(AdminDashboardVm vm)
        {
            var items = new List<DashboardAttentionItemVm>
            {
                CreateAttention("critical", "DashboardAttentionBusinessOnboarding", BusinessAttentionCount(vm.BusinessSupport), "BusinessesTitle", "Businesses", "SupportQueue", "BusinessSupportOpenQueueAction"),
                CreateAttention("critical", "DashboardAttentionPaymentIssues", vm.OrderPaymentIssueCount, "Orders", "Orders", "Index", "DashboardOpenOrdersAction", new { filter = Darwin.Application.Orders.DTOs.OrderQueueFilter.PaymentIssues }),
                CreateAttention("warning", "DashboardAttentionFulfillment", vm.OrderFulfillmentAttentionCount, "Orders", "Orders", "Index", "DashboardOpenOrdersAction", new { filter = Darwin.Application.Orders.DTOs.OrderQueueFilter.FulfillmentAttention }),
                CreateAttention("warning", "DashboardAttentionBilling", vm.PaymentCount ?? 0, "Billing", "Billing", "Payments", "DashboardOpenPaymentsAction", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null),
                CreateAttention("warning", "DashboardAttentionCommunication", CommunicationAttentionCount(vm), "Communications", "BusinessCommunications", "Index", "OpenWorkspaceAction"),
                CreateAttention("warning", "DashboardAttentionMobile", MobileAttentionCount(vm), "MobileOperations", "MobileOperations", "Index", "OpenMobileOps"),
                CreateAttention("info", "DashboardAttentionInventory", vm.PurchaseOrderCount ?? 0, "Inventory", "Inventory", "PurchaseOrders", "DashboardOpenPurchaseOrdersAction", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null),
                CreateAttention("info", "DashboardAttentionLoyalty", vm.PendingRedemptionCount ?? 0, "Loyalty", "Loyalty", "Redemptions", "Redemptions", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null)
            };

            return items
                .Where(item => item.Count > 0)
                .OrderBy(item => item.Severity == "critical" ? 0 : item.Severity == "warning" ? 1 : 2)
                .ThenByDescending(item => item.Count)
                .Take(8)
                .ToList();
        }

        private static DashboardAttentionItemVm CreateAttention(
            string severity,
            string titleKey,
            int count,
            string moduleKey,
            string controller,
            string action,
            string actionLabelKey,
            object? routeValues = null)
        {
            return new DashboardAttentionItemVm
            {
                Severity = severity,
                TitleKey = titleKey,
                Count = count,
                ModuleKey = moduleKey,
                Controller = controller,
                Action = action,
                ActionLabelKey = actionLabelKey,
                RouteValues = routeValues
            };
        }

        private static DashboardBusinessContextVm? BuildBusinessContext(AdminDashboardVm vm)
        {
            if (!vm.SelectedBusinessId.HasValue)
            {
                return null;
            }

            return new DashboardBusinessContextVm
            {
                BusinessName = vm.SelectedBusinessLabel,
                Rows = new[]
                {
                    CreateModule("GoLiveReadiness", [Metric("NeedsAttention", BusinessAttentionCount(vm.BusinessSupport).ToString())], "MerchantReadinessTitle", "Businesses", "MerchantReadiness"),
                    CreateModule("Billing", [Metric("DashboardPaymentsLabel", (vm.PaymentCount ?? 0).ToString()), Metric("DashboardKpiBillingAttention", ((vm.PaymentCount ?? 0) + vm.OrderPaymentIssueCount).ToString())], "DashboardOpenPaymentsAction", "Billing", "Payments", new { businessId = vm.SelectedBusinessId }),
                    CreateModule("Communications", [Metric("DashboardKpiCommunicationAttention", CommunicationAttentionCount(vm).ToString())], "OpenWorkspaceAction", "BusinessCommunications", "Index"),
                    CreateModule("Inventory", [Metric("DashboardWarehousesLabel", (vm.WarehouseCount ?? 0).ToString()), Metric("DashboardPurchaseOrdersLabel", (vm.PurchaseOrderCount ?? 0).ToString())], "DashboardOpenInventoryAction", "Inventory", "Warehouses", new { businessId = vm.SelectedBusinessId }),
                    CreateModule("Loyalty", [Metric("Accounts", (vm.LoyaltyAccountCount ?? 0).ToString()), Metric("PendingRedemptions", (vm.PendingRedemptionCount ?? 0).ToString())], "OpenLoyalty", "Loyalty", "Accounts", new { businessId = vm.SelectedBusinessId })
                }
            };
        }

        private static IReadOnlyList<DashboardModuleSummaryVm> BuildModuleSummaries(AdminDashboardVm vm)
        {
            return new[]
            {
                CreateModule("DashboardCrmTitle", [Metric("DashboardCustomersLabel", vm.Crm.CustomerCount.ToString()), Metric("DashboardLeadsLabel", vm.Crm.LeadCount.ToString()), Metric("DashboardOpenOpportunitiesLabel", vm.Crm.OpenOpportunityCount.ToString())], "DashboardOpenCrmAction", "Crm", "Index"),
                CreateModule("Orders", [Metric("DashboardOrdersLabel", vm.OrderCount.ToString()), Metric("OpenQueue", vm.OpenOrderCount.ToString()), Metric("FulfillmentAttention", vm.OrderFulfillmentAttentionCount.ToString())], "DashboardOpenOrdersAction", "Orders", "Index"),
                CreateModule("Inventory", [Metric("DashboardWarehousesLabel", (vm.WarehouseCount ?? 0).ToString()), Metric("DashboardSuppliersLabel", (vm.SupplierCount ?? 0).ToString()), Metric("DashboardPurchaseOrdersLabel", (vm.PurchaseOrderCount ?? 0).ToString())], "DashboardOpenInventoryAction", "Inventory", "Warehouses", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null),
                CreateModule("Billing", [Metric("DashboardPaymentsLabel", (vm.PaymentCount ?? 0).ToString()), Metric("DashboardAttentionPaymentIssues", vm.OrderPaymentIssueCount.ToString())], "DashboardOpenPaymentsAction", "Billing", "Payments", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null),
                CreateModule("Loyalty", [Metric("Accounts", (vm.LoyaltyAccountCount ?? 0).ToString()), Metric("PendingRedemptions", (vm.PendingRedemptionCount ?? 0).ToString()), Metric("RecentScanSessions", (vm.ScanSessionCount ?? 0).ToString())], "OpenLoyalty", "Loyalty", "Accounts", vm.SelectedBusinessId.HasValue ? new { businessId = vm.SelectedBusinessId } : null),
                CreateModule("MobileOperations", [Metric("ActiveDevices", vm.MobileActiveDeviceCount.ToString()), Metric("MobileStaleDevices", vm.MobileStaleDeviceCount.ToString()), Metric("MobileMissingPushToken", vm.MobileMissingPushTokenCount.ToString())], "OpenMobileOps", "MobileOperations", "Index"),
                CreateModule("CommunicationOpsTitle", [Metric("BusinessesRequiringEmailSetup", vm.CommunicationOps.BusinessesRequiringEmailSetupCount.ToString()), Metric("MissingSupportEmail", vm.CommunicationOps.MissingSupportEmailCount.ToString()), Metric("FailedEmails", CommunicationFailureCount(vm.CommunicationOps).ToString())], "OpenWorkspaceAction", "BusinessCommunications", "Index")
            };
        }

        private static DashboardModuleSummaryVm CreateModule(
            string titleKey,
            IReadOnlyList<DashboardMetricVm> metrics,
            string primaryActionLabelKey,
            string primaryController,
            string primaryAction,
            object? primaryRouteValues = null)
        {
            return new DashboardModuleSummaryVm
            {
                TitleKey = titleKey,
                Metrics = metrics,
                PrimaryActionLabelKey = primaryActionLabelKey,
                PrimaryController = primaryController,
                PrimaryAction = primaryAction,
                PrimaryRouteValues = primaryRouteValues
            };
        }

        private static DashboardMetricVm Metric(string labelKey, string value)
        {
            return new DashboardMetricVm
            {
                LabelKey = labelKey,
                Value = value
            };
        }

        private static int BusinessAttentionCount(BusinessSupportSummaryVm summary)
        {
            return summary.AttentionBusinessCount
                + summary.PendingApprovalBusinessCount
                + summary.SuspendedBusinessCount
                + summary.MissingOwnerBusinessCount
                + summary.PendingInvitationCount
                + summary.PendingActivationMemberCount
                + summary.LockedMemberCount;
        }

        private static int CommunicationAttentionCount(AdminDashboardVm vm)
        {
            var missingRuntimeDependencyCount = (vm.CommunicationOps.EmailTransportConfigured ? 0 : 1)
                + (vm.CommunicationOps.AdminAlertRoutingConfigured ? 0 : 1)
                + (vm.CommunicationOps.TransactionalEmailBusinessesCount > 0 ? 0 : 1);

            return missingRuntimeDependencyCount
                + vm.CommunicationOps.BusinessesRequiringEmailSetupCount
                + vm.CommunicationOps.MissingSupportEmailCount
                + vm.CommunicationOps.MissingSenderIdentityCount
                + CommunicationFailureCount(vm.CommunicationOps);
        }

        private static int CommunicationFailureCount(BusinessCommunicationOpsSummaryVm summary)
        {
            return summary.FailedInvitationCount
                + summary.FailedActivationCount
                + summary.FailedPasswordResetCount
                + summary.FailedAdminTestCount;
        }

        private static int MobileAttentionCount(AdminDashboardVm vm)
        {
            return vm.MobileStaleDeviceCount + vm.MobileMissingPushTokenCount;
        }
    }
}
