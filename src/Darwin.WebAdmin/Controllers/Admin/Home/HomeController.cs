using Darwin.Application.Billing.Queries;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.CMS.Queries;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Orders.Queries;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Admin;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
        private readonly AdminReferenceDataService _referenceData;

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
            AdminReferenceDataService referenceData)
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
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
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

            var crmSummaryTask = _getCrmSummary.HandleAsync(ct);
            var productsTask = _getProductsPage.HandleAsync(page: 1, pageSize: 1, culture: "de-DE", ct);
            var pagesTask = _getPagesPage.HandleAsync(page: 1, pageSize: 1, culture: "de-DE", ct);
            var ordersTask = _getOrdersPage.HandleAsync(page: 1, pageSize: 1, ct);
            var usersTask = _getUsersPage.HandleAsync(page: 1, pageSize: 1, emailFilter: null, ct);

            Task<(List<Darwin.Application.Billing.DTOs.PaymentListItemDto> Items, int Total)>? paymentsTask = null;
            Task<(List<Darwin.Application.Inventory.DTOs.WarehouseListItemDto> Items, int Total)>? warehousesTask = null;
            Task<(List<Darwin.Application.Inventory.DTOs.SupplierListItemDto> Items, int Total)>? suppliersTask = null;
            Task<(List<Darwin.Application.Inventory.DTOs.PurchaseOrderListItemDto> Items, int Total)>? purchaseOrdersTask = null;

            if (selectedBusinessId.HasValue)
            {
                paymentsTask = _getPaymentsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, ct);
                warehousesTask = _getWarehousesPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, ct);
                suppliersTask = _getSuppliersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, ct);
                purchaseOrdersTask = _getPurchaseOrdersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, ct);
            }

            await Task.WhenAll(new Task[] { crmSummaryTask, productsTask, pagesTask, ordersTask, usersTask }
                .Concat(paymentsTask is null ? Array.Empty<Task>() : new[] { paymentsTask })
                .Concat(warehousesTask is null ? Array.Empty<Task>() : new[] { warehousesTask })
                .Concat(suppliersTask is null ? Array.Empty<Task>() : new[] { suppliersTask })
                .Concat(purchaseOrdersTask is null ? Array.Empty<Task>() : new[] { purchaseOrdersTask }))
                .ConfigureAwait(false);

            var crmSummary = await crmSummaryTask.ConfigureAwait(false);
            var products = await productsTask.ConfigureAwait(false);
            var pages = await pagesTask.ConfigureAwait(false);
            var orders = await ordersTask.ConfigureAwait(false);
            var users = await usersTask.ConfigureAwait(false);

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
                Crm = MapCrmSummary(crmSummary),
                PaymentCount = paymentsTask is null ? null : (await paymentsTask.ConfigureAwait(false)).Total,
                WarehouseCount = warehousesTask is null ? null : (await warehousesTask.ConfigureAwait(false)).Total,
                SupplierCount = suppliersTask is null ? null : (await suppliersTask.ConfigureAwait(false)).Total,
                PurchaseOrderCount = purchaseOrdersTask is null ? null : (await purchaseOrdersTask.ConfigureAwait(false)).Total
            };

            return View(vm);
        }

        private static CrmSummaryVm MapCrmSummary(CrmSummaryDto dto)
        {
            return new CrmSummaryVm
            {
                CustomerCount = dto.CustomerCount,
                LeadCount = dto.LeadCount,
                QualifiedLeadCount = dto.QualifiedLeadCount,
                OpenOpportunityCount = dto.OpenOpportunityCount,
                OpenPipelineMinor = dto.OpenPipelineMinor,
                SegmentCount = dto.SegmentCount,
                RecentInteractionCount = dto.RecentInteractionCount
            };
        }
    }
}
