using Darwin.Application.Billing.Queries;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.CMS.Queries;
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
        private readonly GetProductsPageHandler _getProductsPage;
        private readonly GetPagesPageHandler _getPagesPage;
        private readonly GetOrdersPageHandler _getOrdersPage;
        private readonly GetUsersPageHandler _getUsersPage;
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
            GetProductsPageHandler getProductsPage,
            GetPagesPageHandler getPagesPage,
            GetOrdersPageHandler getOrdersPage,
            GetUsersPageHandler getUsersPage,
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
            _getProductsPage = getProductsPage ?? throw new ArgumentNullException(nameof(getProductsPage));
            _getPagesPage = getPagesPage ?? throw new ArgumentNullException(nameof(getPagesPage));
            _getOrdersPage = getOrdersPage ?? throw new ArgumentNullException(nameof(getOrdersPage));
            _getUsersPage = getUsersPage ?? throw new ArgumentNullException(nameof(getUsersPage));
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
            var businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId: businessId, ct).ConfigureAwait(false);
            var selectedBusinessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId, ct).ConfigureAwait(false);

            var crmSummary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var products = await _getProductsPage.HandleAsync(page: 1, pageSize: 1, culture: "de-DE", ct).ConfigureAwait(false);
            var pages = await _getPagesPage.HandleAsync(page: 1, pageSize: 1, culture: "de-DE", ct: ct).ConfigureAwait(false);
            var orders = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, ct: ct).ConfigureAwait(false);
            var users = await _getUsersPage.HandleAsync(page: 1, pageSize: 1, emailFilter: null, filter: Darwin.Application.Identity.DTOs.UserQueueFilter.All, ct: ct).ConfigureAwait(false);
            var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);
            var communicationOps = await _getBusinessCommunicationOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            var mobileDeviceOps = await _getMobileDeviceOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);

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

            var vm = new AdminDashboardVm
            {
                BusinessCount = businessOptions.Count,
                BusinessOptions = businessOptions,
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                ProductCount = products.Total,
                PageCount = pages.Total,
                OrderCount = orders.Total,
                UserCount = users.Total,
                Crm = MapCrmSummary(crmSummary, siteSettings.DefaultCurrency),
                BusinessSupport = MapBusinessSupportSummary(businessSupport),
                CommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings),
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
            var businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId: businessId, ct).ConfigureAwait(false);
            var selectedBusinessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId, ct).ConfigureAwait(false);

            var communicationOps = await _getBusinessCommunicationOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);

            return new AdminDashboardVm
            {
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                CommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings)
            };
        }

        private async Task<AdminDashboardVm> BuildBusinessSupportCardVmAsync(Guid? businessId, CancellationToken ct)
        {
            var businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId: businessId, ct).ConfigureAwait(false);
            var selectedBusinessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId, ct).ConfigureAwait(false);

            var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);

            return new AdminDashboardVm
            {
                SelectedBusinessId = selectedBusinessId,
                SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,
                BusinessSupport = MapBusinessSupportSummary(businessSupport)
            };
        }

        private static CrmSummaryVm MapCrmSummary(CrmSummaryDto dto, string currency)
        {
            return new CrmSummaryVm
            {
                CustomerCount = dto.CustomerCount,
                LeadCount = dto.LeadCount,
                QualifiedLeadCount = dto.QualifiedLeadCount,
                OpenOpportunityCount = dto.OpenOpportunityCount,
                Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency,
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
                OpenInvitationCount = dto.OpenInvitationCount,
                PendingActivationMemberCount = dto.PendingActivationMemberCount,
                LockedMemberCount = dto.LockedMemberCount,
                SelectedBusinessOpenInvitationCount = dto.SelectedBusinessOpenInvitationCount,
                SelectedBusinessPendingActivationCount = dto.SelectedBusinessPendingActivationCount,
                SelectedBusinessLockedMemberCount = dto.SelectedBusinessLockedMemberCount
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
                BusinessesRequiringEmailSetupCount = dto.BusinessesRequiringEmailSetupCount
            };
        }
    }
}
