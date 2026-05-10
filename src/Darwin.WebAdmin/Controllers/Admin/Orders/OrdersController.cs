using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common.Queries;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.CRM;
using Darwin.WebAdmin.ViewModels.Orders;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Orders
{
    /// <summary>
    /// Admin controller for order operations including payments, shipments, refunds, and invoices.
    /// </summary>
    public sealed class OrdersController : AdminBaseController
    {
        private readonly GetOrdersPageHandler _getOrdersPage;
        private readonly GetShipmentsPageHandler _getShipmentsPage;
        private readonly GetShipmentOpsSummaryHandler _getShipmentOpsSummary;
        private readonly GetShipmentProviderOperationsPageHandler _getShipmentProviderOperationsPage;
        private readonly GetOrderForViewHandler _getOrderForView;
        private readonly GetOrderPaymentsPageHandler _getOrderPaymentsPage;
        private readonly GetOrderShipmentsPageHandler _getOrderShipmentsPage;
        private readonly GetOrderRefundsPageHandler _getOrderRefundsPage;
        private readonly GetOrderInvoicesPageHandler _getOrderInvoicesPage;
        private readonly GetWarehouseLookupHandler _getWarehouseLookup;
        private readonly GetBusinessLookupHandler _getBusinessLookup;
        private readonly GetCustomerLookupHandler _getCustomerLookup;
        private readonly AddPaymentHandler _addPayment;
        private readonly AddShipmentHandler _addShipment;
        private readonly GenerateDhlShipmentLabelHandler _generateDhlShipmentLabel;
        private readonly ResolveShipmentCarrierExceptionHandler _resolveShipmentCarrierException;
        private readonly UpdateShipmentProviderOperationHandler _updateShipmentProviderOperation;
        private readonly AddRefundHandler _addRefund;
        private readonly CreateOrderInvoiceHandler _createOrderInvoice;
        private readonly UpdateOrderStatusHandler _updateOrderStatus;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IClock _clock;

        public OrdersController(
            GetOrdersPageHandler getOrdersPage,
            GetShipmentsPageHandler getShipmentsPage,
            GetShipmentOpsSummaryHandler getShipmentOpsSummary,
            GetShipmentProviderOperationsPageHandler getShipmentProviderOperationsPage,
            GetOrderForViewHandler getOrderForView,
            GetOrderPaymentsPageHandler getOrderPaymentsPage,
            GetOrderShipmentsPageHandler getOrderShipmentsPage,
            GetOrderRefundsPageHandler getOrderRefundsPage,
            GetOrderInvoicesPageHandler getOrderInvoicesPage,
            GetWarehouseLookupHandler getWarehouseLookup,
            GetBusinessLookupHandler getBusinessLookup,
            GetCustomerLookupHandler getCustomerLookup,
            AddPaymentHandler addPayment,
            AddShipmentHandler addShipment,
            GenerateDhlShipmentLabelHandler generateDhlShipmentLabel,
            ResolveShipmentCarrierExceptionHandler resolveShipmentCarrierException,
            UpdateShipmentProviderOperationHandler updateShipmentProviderOperation,
            AddRefundHandler addRefund,
            CreateOrderInvoiceHandler createOrderInvoice,
            UpdateOrderStatusHandler updateOrderStatus,
            ISiteSettingCache siteSettingCache,
            IClock clock)
        {
            _getOrdersPage = getOrdersPage ?? throw new ArgumentNullException(nameof(getOrdersPage));
            _getShipmentsPage = getShipmentsPage ?? throw new ArgumentNullException(nameof(getShipmentsPage));
            _getShipmentOpsSummary = getShipmentOpsSummary ?? throw new ArgumentNullException(nameof(getShipmentOpsSummary));
            _getShipmentProviderOperationsPage = getShipmentProviderOperationsPage ?? throw new ArgumentNullException(nameof(getShipmentProviderOperationsPage));
            _getOrderForView = getOrderForView ?? throw new ArgumentNullException(nameof(getOrderForView));
            _getOrderPaymentsPage = getOrderPaymentsPage ?? throw new ArgumentNullException(nameof(getOrderPaymentsPage));
            _getOrderShipmentsPage = getOrderShipmentsPage ?? throw new ArgumentNullException(nameof(getOrderShipmentsPage));
            _getOrderRefundsPage = getOrderRefundsPage ?? throw new ArgumentNullException(nameof(getOrderRefundsPage));
            _getOrderInvoicesPage = getOrderInvoicesPage ?? throw new ArgumentNullException(nameof(getOrderInvoicesPage));
            _getWarehouseLookup = getWarehouseLookup ?? throw new ArgumentNullException(nameof(getWarehouseLookup));
            _getBusinessLookup = getBusinessLookup ?? throw new ArgumentNullException(nameof(getBusinessLookup));
            _getCustomerLookup = getCustomerLookup ?? throw new ArgumentNullException(nameof(getCustomerLookup));
            _addPayment = addPayment ?? throw new ArgumentNullException(nameof(addPayment));
            _addShipment = addShipment ?? throw new ArgumentNullException(nameof(addShipment));
            _generateDhlShipmentLabel = generateDhlShipmentLabel ?? throw new ArgumentNullException(nameof(generateDhlShipmentLabel));
            _resolveShipmentCarrierException = resolveShipmentCarrierException ?? throw new ArgumentNullException(nameof(resolveShipmentCarrierException));
            _updateShipmentProviderOperation = updateShipmentProviderOperation ?? throw new ArgumentNullException(nameof(updateShipmentProviderOperation));
            _addRefund = addRefund ?? throw new ArgumentNullException(nameof(addRefund));
            _createOrderInvoice = createOrderInvoice ?? throw new ArgumentNullException(nameof(createOrderInvoice));
            _updateOrderStatus = updateOrderStatus ?? throw new ArgumentNullException(nameof(updateOrderStatus));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, OrderQueueFilter filter = OrderQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getOrdersPage.HandleAsync(page, pageSize, query, filter, ct).ConfigureAwait(false);
            var vm = new OrdersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildOrderFilterItems(filter),
                Items = items.Select(o => new OrderListItemVm
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    Currency = o.Currency,
                    GrandTotalGrossMinor = o.GrandTotalGrossMinor,
                    PaymentCount = o.PaymentCount,
                    FailedPaymentCount = o.FailedPaymentCount,
                    ShipmentCount = o.ShipmentCount,
                    CreatedAtUtc = o.CreatedAtUtc,
                    RowVersion = o.RowVersion
                }).ToList()
            };

            return RenderOrdersWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ShipmentsQueue(int page = 1, int pageSize = 20, string? query = null, ShipmentQueueFilter filter = ShipmentQueueFilter.All, CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var (items, total) = await _getShipmentsPage.HandleAsync(
                page,
                pageSize,
                query,
                filter,
                settings.ShipmentAttentionDelayHours,
                settings.ShipmentTrackingGraceHours,
                ct).ConfigureAwait(false);
            var vm = new ShipmentsQueueVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                Dhl = BuildDhlOperationsVm(settings),
                Summary = await BuildShipmentOpsSummaryVmAsync(settings.ShipmentAttentionDelayHours, settings.ShipmentTrackingGraceHours, ct).ConfigureAwait(false),
                Playbooks = BuildShipmentPlaybooks(),
                FilterItems = BuildShipmentFilterItems(filter),
                PageSizeItems = BuildPageSizeItems(pageSize),
                Items = items.Select(x => new ShipmentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    Carrier = x.Carrier,
                    Service = x.Service,
                    ProviderShipmentReference = x.ProviderShipmentReference,
                    TrackingNumber = x.TrackingNumber,
                    TrackingUrl = x.TrackingUrl,
                    LabelUrl = x.LabelUrl,
                    TotalWeight = x.TotalWeight,
                    Status = x.Status,
                    ShippedAtUtc = x.ShippedAtUtc,
                    DeliveredAtUtc = x.DeliveredAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsDhl = x.IsDhl,
                    NeedsCarrierReview = x.NeedsCarrierReview,
                    NeedsReturnFollowUp = x.NeedsReturnFollowUp,
                    AwaitingHandoff = x.AwaitingHandoff,
                    TrackingOverdue = x.TrackingOverdue,
                    OpenAgeHours = x.OpenAgeHours,
                    InTransitAgeHours = x.InTransitAgeHours,
                    LastCarrierEventAtUtc = x.LastCarrierEventAtUtc,
                    LastCarrierEventKey = x.LastCarrierEventKey,
                    RecentCarrierEvents = x.RecentCarrierEvents.Select(e => new ShipmentCarrierEventVm
                    {
                        CarrierEventKey = e.CarrierEventKey,
                        ProviderStatus = e.ProviderStatus,
                        ExceptionCode = e.ExceptionCode,
                        ExceptionMessage = e.ExceptionMessage,
                        TrackingNumber = e.TrackingNumber,
                        LabelUrl = e.LabelUrl,
                        Service = e.Service,
                        OccurredAtUtc = e.OccurredAtUtc
                    }).ToList(),
                    TrackingState = x.TrackingState,
                    ExceptionNote = x.ExceptionNote,
                    AttentionDelayHours = x.AttentionDelayHours,
                    TrackingGraceHours = x.TrackingGraceHours,
                    DefaultRefundPaymentId = x.DefaultRefundPaymentId,
                    HasRefundablePayment = x.HasRefundablePayment,
                    ProviderOperationQueued = x.ProviderOperationQueued,
                    ProviderOperationFailed = x.ProviderOperationFailed,
                    ProviderOperationType = x.ProviderOperationType,
                    ProviderOperationFailureReason = x.ProviderOperationFailureReason,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderShipmentsQueueWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ReturnsQueue(int page = 1, int pageSize = 20, string? query = null, ReturnQueueFilter filter = ReturnQueueFilter.All, CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var shipmentFilter = filter switch
            {
                ReturnQueueFilter.FollowUp => ShipmentQueueFilter.ReturnFollowUp,
                ReturnQueueFilter.CarrierReview => ShipmentQueueFilter.CarrierReview,
                _ => ShipmentQueueFilter.Returned
            };

            var (items, total) = await _getShipmentsPage.HandleAsync(
                page,
                pageSize,
                query,
                shipmentFilter,
                settings.ShipmentAttentionDelayHours,
                settings.ShipmentTrackingGraceHours,
                ct).ConfigureAwait(false);

            var vm = new ReturnsQueueVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                Summary = await BuildShipmentOpsSummaryVmAsync(settings.ShipmentAttentionDelayHours, settings.ShipmentTrackingGraceHours, ct).ConfigureAwait(false),
                Playbooks = BuildReturnPlaybooks(),
                FilterItems = BuildReturnFilterItems(filter),
                PageSizeItems = BuildPageSizeItems(pageSize),
                Items = items.Select(x => new ShipmentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    Carrier = x.Carrier,
                    Service = x.Service,
                    ProviderShipmentReference = x.ProviderShipmentReference,
                    TrackingNumber = x.TrackingNumber,
                    TrackingUrl = x.TrackingUrl,
                    LabelUrl = x.LabelUrl,
                    TotalWeight = x.TotalWeight,
                    Status = x.Status,
                    ShippedAtUtc = x.ShippedAtUtc,
                    DeliveredAtUtc = x.DeliveredAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsDhl = x.IsDhl,
                    NeedsCarrierReview = x.NeedsCarrierReview,
                    NeedsReturnFollowUp = x.NeedsReturnFollowUp,
                    AwaitingHandoff = x.AwaitingHandoff,
                    TrackingOverdue = x.TrackingOverdue,
                    OpenAgeHours = x.OpenAgeHours,
                    InTransitAgeHours = x.InTransitAgeHours,
                    LastCarrierEventAtUtc = x.LastCarrierEventAtUtc,
                    LastCarrierEventKey = x.LastCarrierEventKey,
                    RecentCarrierEvents = x.RecentCarrierEvents.Select(e => new ShipmentCarrierEventVm
                    {
                        CarrierEventKey = e.CarrierEventKey,
                        ProviderStatus = e.ProviderStatus,
                        ExceptionCode = e.ExceptionCode,
                        ExceptionMessage = e.ExceptionMessage,
                        TrackingNumber = e.TrackingNumber,
                        LabelUrl = e.LabelUrl,
                        Service = e.Service,
                        OccurredAtUtc = e.OccurredAtUtc
                    }).ToList(),
                    TrackingState = x.TrackingState,
                    ExceptionNote = x.ExceptionNote,
                    AttentionDelayHours = x.AttentionDelayHours,
                    TrackingGraceHours = x.TrackingGraceHours,
                    DefaultRefundPaymentId = x.DefaultRefundPaymentId,
                    HasRefundablePayment = x.HasRefundablePayment,
                    ProviderOperationQueued = x.ProviderOperationQueued,
                    ProviderOperationFailed = x.ProviderOperationFailed,
                    ProviderOperationType = x.ProviderOperationType,
                    ProviderOperationFailureReason = x.ProviderOperationFailureReason,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderReturnsQueueWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ShipmentProviderOperations(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? provider = null,
            string? operationType = null,
            string? status = null,
            bool stalePendingOnly = false,
            bool failedOnly = false,
            CancellationToken ct = default)
        {
            var filter = new ShipmentProviderOperationFilterDto
            {
                Query = query ?? string.Empty,
                Provider = provider ?? string.Empty,
                OperationType = operationType ?? string.Empty,
                Status = status ?? string.Empty,
                StalePendingOnly = stalePendingOnly,
                FailedOnly = failedOnly
            };

            var (items, total, summary, providers, operationTypes) = await _getShipmentProviderOperationsPage
                .HandleAsync(page, pageSize, filter, ct)
                .ConfigureAwait(false);

            var vm = new ShipmentProviderOperationsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = filter.Query,
                Provider = filter.Provider,
                OperationType = filter.OperationType,
                Status = filter.Status,
                StalePendingOnly = stalePendingOnly,
                FailedOnly = failedOnly,
                Summary = new ShipmentProviderOperationSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    PendingCount = summary.PendingCount,
                    FailedCount = summary.FailedCount,
                    ProcessedCount = summary.ProcessedCount,
                    StalePendingCount = summary.StalePendingCount,
                    CancelledCount = summary.CancelledCount
                },
                PageSizeItems = BuildPageSizeItems(pageSize),
                ProviderItems = BuildShipmentProviderItems(providers, provider),
                OperationTypeItems = BuildShipmentOperationTypeItems(operationTypes, operationType),
                StatusItems = BuildShipmentProviderOperationStatusItems(status),
                Items = items.Select(x => new ShipmentProviderOperationListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    ShipmentId = x.ShipmentId,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    Provider = x.Provider,
                    OperationType = x.OperationType,
                    Status = x.Status,
                    AttemptCount = x.AttemptCount,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    ProcessedAtUtc = x.ProcessedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    AgeMinutes = x.AgeMinutes,
                    IsStalePending = x.IsStalePending,
                    FailureReason = x.FailureReason,
                    TrackingNumber = x.TrackingNumber,
                    LabelUrl = x.LabelUrl
                }).ToList()
            };

            return RenderShipmentProviderOperationsWorkspace(vm);
        }

        private async Task<DhlOperationsVm> BuildDhlOperationsVmAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return BuildDhlOperationsVm(settings);
        }

        private static DhlOperationsVm BuildDhlOperationsVm(Darwin.Application.Settings.DTOs.SiteSettingDto settings)
        {
            return new DhlOperationsVm
            {
                Enabled = settings.DhlEnabled,
                ApiBaseUrlConfigured = !string.IsNullOrWhiteSpace(settings.DhlApiBaseUrl),
                ApiCredentialsConfigured = !string.IsNullOrWhiteSpace(settings.DhlApiKey) && !string.IsNullOrWhiteSpace(settings.DhlApiSecret),
                AccountNumberConfigured = !string.IsNullOrWhiteSpace(settings.DhlAccountNumber),
                EnvironmentLabel = string.IsNullOrWhiteSpace(settings.DhlEnvironment) ? "NotSet" : settings.DhlEnvironment,
                ShipmentAttentionDelayHours = settings.ShipmentAttentionDelayHours,
                ShipmentTrackingGraceHours = settings.ShipmentTrackingGraceHours,
                ShipperIdentityConfigured =
                    !string.IsNullOrWhiteSpace(settings.DhlShipperName) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperEmail) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperPhoneE164) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperStreet) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperPostalCode) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperCity) &&
                    !string.IsNullOrWhiteSpace(settings.DhlShipperCountry)
            };
        }

        private TaxPolicySnapshotVm MapTaxPolicy(Darwin.Application.Settings.DTOs.SiteSettingDto dto)
        {
            var issuerConfigured = !string.IsNullOrWhiteSpace(dto.InvoiceIssuerLegalName);
            var issuerTaxIdConfigured = !string.IsNullOrWhiteSpace(dto.InvoiceIssuerTaxId);
            var issuerAddressConfigured =
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerAddressLine1) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerPostalCode) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerCity) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerCountry);
            var archiveReady = issuerConfigured && issuerTaxIdConfigured && issuerAddressConfigured;
            var eInvoiceBaselineReady = archiveReady && dto.VatEnabled;
            var structuredExportBaselineReady = archiveReady;

            return new TaxPolicySnapshotVm
            {
                VatEnabled = dto.VatEnabled,
                DefaultVatRatePercent = dto.DefaultVatRatePercent,
                PricesIncludeVat = dto.PricesIncludeVat,
                AllowReverseCharge = dto.AllowReverseCharge,
                IssuerConfigured = issuerConfigured,
                InvoiceIssuerLegalName = dto.InvoiceIssuerLegalName ?? string.Empty,
                InvoiceIssuerCountry = dto.InvoiceIssuerCountry ?? string.Empty,
                InvoiceIssuerTaxIdConfigured = issuerTaxIdConfigured,
                ArchiveReadinessComplete = archiveReady,
                ArchiveReadinessLabel = archiveReady ? T("TaxPolicyArchiveReady") : T("TaxPolicyArchiveIncomplete"),
                EInvoiceBaselineReady = eInvoiceBaselineReady,
                EInvoiceBaselineLabel = eInvoiceBaselineReady ? T("TaxPolicyBaselineReady") : T("TaxPolicyBaselineIncomplete"),
                StructuredExportBaselineReady = structuredExportBaselineReady,
                StructuredExportBaselineLabel = structuredExportBaselineReady ? T("TaxPolicyStructuredExportReady") : T("TaxPolicyStructuredExportIncomplete"),
                ComplianceScopeNote = T("TaxPolicyComplianceScopeNote")
            };
        }

        private async Task<ShipmentOpsSummaryVm> BuildShipmentOpsSummaryVmAsync(int attentionDelayHours, int trackingGraceHours, CancellationToken ct)
        {
            var summary = await _getShipmentOpsSummary.HandleAsync(attentionDelayHours, trackingGraceHours, ct).ConfigureAwait(false);
            return new ShipmentOpsSummaryVm
            {
                PendingCount = summary.PendingCount,
                ShippedCount = summary.ShippedCount,
                MissingTrackingCount = summary.MissingTrackingCount,
                ReturnedCount = summary.ReturnedCount,
                DhlCount = summary.DhlCount,
                MissingServiceCount = summary.MissingServiceCount,
                AwaitingHandoffCount = summary.AwaitingHandoffCount,
                TrackingOverdueCount = summary.TrackingOverdueCount,
                CarrierReviewCount = summary.CarrierReviewCount,
                ReturnFollowUpCount = summary.ReturnFollowUpCount
            };
        }

        private List<ShipmentPlaybookVm> BuildShipmentPlaybooks()
        {
            return new List<ShipmentPlaybookVm>
            {
                new()
                {
                    Title = T("ShipmentPlaybookPendingTitle"),
                    ScopeNote = T("ShipmentPlaybookPendingScope"),
                    OperatorAction = T("ShipmentPlaybookPendingAction"),
                    SettingsDependency = T("ShipmentPlaybookPendingDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookAwaitingHandoffTitle"),
                    ScopeNote = T("ShipmentPlaybookAwaitingHandoffScope"),
                    OperatorAction = T("ShipmentPlaybookAwaitingHandoffAction"),
                    SettingsDependency = T("ShipmentPlaybookAwaitingHandoffDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookTrackingOverdueTitle"),
                    ScopeNote = T("ShipmentPlaybookTrackingOverdueScope"),
                    OperatorAction = T("ShipmentPlaybookTrackingOverdueAction"),
                    SettingsDependency = T("ShipmentPlaybookTrackingOverdueDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookMissingTrackingTitle"),
                    ScopeNote = T("ShipmentPlaybookMissingTrackingScope"),
                    OperatorAction = T("ShipmentPlaybookMissingTrackingAction"),
                    SettingsDependency = T("ShipmentPlaybookMissingTrackingDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookDhlDataTitle"),
                    ScopeNote = T("ShipmentPlaybookDhlDataScope"),
                    OperatorAction = T("ShipmentPlaybookDhlDataAction"),
                    SettingsDependency = T("ShipmentPlaybookDhlDataDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookCarrierReviewTitle"),
                    ScopeNote = T("ShipmentPlaybookCarrierReviewScope"),
                    OperatorAction = T("ShipmentPlaybookCarrierReviewAction"),
                    SettingsDependency = T("ShipmentPlaybookCarrierReviewDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookReturnedTitle"),
                    ScopeNote = T("ShipmentPlaybookReturnedScope"),
                    OperatorAction = T("ShipmentPlaybookReturnedAction"),
                    SettingsDependency = T("ShipmentPlaybookReturnedDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookReturnFollowUpTitle"),
                    ScopeNote = T("ShipmentPlaybookReturnFollowUpScope"),
                    OperatorAction = T("ShipmentPlaybookReturnFollowUpAction"),
                    SettingsDependency = T("ShipmentPlaybookReturnFollowUpDependency")
                }
            };
        }

        private List<ShipmentPlaybookVm> BuildReturnPlaybooks()
        {
            return new List<ShipmentPlaybookVm>
            {
                new()
                {
                    Title = T("ShipmentPlaybookReturnedTitle"),
                    ScopeNote = T("ShipmentPlaybookReturnedScope"),
                    OperatorAction = T("ShipmentPlaybookReturnedAction"),
                    SettingsDependency = T("ShipmentPlaybookReturnedDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookReturnFollowUpTitle"),
                    ScopeNote = T("ShipmentPlaybookReturnFollowUpScope"),
                    OperatorAction = T("ShipmentPlaybookReturnFollowUpAction"),
                    SettingsDependency = T("ShipmentPlaybookReturnFollowUpDependency")
                },
                new()
                {
                    Title = T("ShipmentPlaybookCarrierReviewTitle"),
                    ScopeNote = T("ShipmentPlaybookCarrierReviewScope"),
                    OperatorAction = T("ShipmentPlaybookCarrierReviewAction"),
                    SettingsDependency = T("ShipmentPlaybookCarrierReviewDependency")
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getOrderForView.HandleAsync(id, ct).ConfigureAwait(false);
            var warehouses = await _getWarehouseLookup.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var assignedWarehouseIds = dto.Lines
                .Select(x => x.WarehouseId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            Guid? selectedWarehouseId = assignedWarehouseIds.Count == 1 ? assignedWarehouseIds[0] : null;
            var warehouseMap = warehouses.ToDictionary(x => x.Id, x => x.Name);

            var vm = new OrderDetailVm
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                UserId = dto.UserId,
                Status = dto.Status,
                Currency = dto.Currency,
                PricesIncludeTax = dto.PricesIncludeTax,
                SubtotalNetMinor = dto.SubtotalNetMinor,
                TaxTotalMinor = dto.TaxTotalMinor,
                DiscountTotalMinor = dto.DiscountTotalMinor,
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
                ShippingMethodId = dto.ShippingMethodId,
                ShippingMethodName = dto.ShippingMethodName,
                ShippingCarrier = dto.ShippingCarrier,
                ShippingService = dto.ShippingService,
                ShippingTotalMinor = dto.ShippingTotalMinor,
                TaxPolicy = MapTaxPolicy(settings),
                ReturnSupport = BuildReturnSupportBaseline(dto),
                RowVersion = dto.RowVersion,
                SelectedWarehouseId = selectedWarehouseId,
                WarehouseOptions = warehouses
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.IsDefault
                            ? $"{x.Name} (Default{(string.IsNullOrWhiteSpace(x.Location) ? string.Empty : $", {x.Location}")})"
                            : string.IsNullOrWhiteSpace(x.Location)
                                ? x.Name
                                : $"{x.Name} ({x.Location})",
                        Selected = selectedWarehouseId == x.Id
                    })
                    .ToList(),
                Lines = dto.Lines.Select(x => new OrderLineVm
                {
                    VariantId = x.VariantId,
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.WarehouseId.HasValue && warehouseMap.TryGetValue(x.WarehouseId.Value, out var warehouseName)
                        ? warehouseName
                        : string.Empty,
                    Name = x.Name,
                    Sku = x.Sku,
                    Quantity = x.Quantity,
                    UnitPriceGrossMinor = x.UnitPriceGrossMinor,
                    LineGrossMinor = x.LineGrossMinor
                }).ToList()
            };

            return RenderOrderDetailsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Payments(Guid orderId, int page = 1, int pageSize = 10, PaymentQueueFilter filter = PaymentQueueFilter.All, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(T("OrderNotFound"));
            }

            var (items, total) = await _getOrderPaymentsPage.HandleAsync(orderId, page, pageSize, filter, ct).ConfigureAwait(false);
            var vm = new OrderPaymentsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Filter = filter,
                FilterItems = BuildPaymentFilterItems(filter),
                Items = items.Select(x => new PaymentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    Provider = x.Provider,
                    InvoiceId = x.InvoiceId,
                    InvoiceStatus = x.InvoiceStatus,
                    ProviderReference = x.ProviderReference,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    FailureReason = x.FailureReason,
                    CreatedAtUtc = x.CreatedAtUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    RefundedAmountMinor = x.RefundedAmountMinor,
                    NetCapturedAmountMinor = x.NetCapturedAmountMinor,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_PaymentsGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Shipments(Guid orderId, int page = 1, int pageSize = 10, ShipmentQueueFilter filter = ShipmentQueueFilter.All, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(T("OrderNotFound"));
            }

            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var order = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            var (items, total) = await _getOrderShipmentsPage.HandleAsync(
                orderId,
                page,
                pageSize,
                filter,
                settings.ShipmentAttentionDelayHours,
                settings.ShipmentTrackingGraceHours,
                ct).ConfigureAwait(false);
            var vm = new OrderShipmentsPageVm
            {
                OrderId = orderId,
                DefaultRefundPaymentId = ResolveDefaultRefundPaymentId(order),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Filter = filter,
                FilterItems = BuildShipmentFilterItems(filter),
                Items = items.Select(x => new ShipmentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    Carrier = x.Carrier,
                    Service = x.Service,
                    ProviderShipmentReference = x.ProviderShipmentReference,
                    TrackingNumber = x.TrackingNumber,
                    TrackingUrl = x.TrackingUrl,
                    LabelUrl = x.LabelUrl,
                    TotalWeight = x.TotalWeight,
                    Status = x.Status,
                    ShippedAtUtc = x.ShippedAtUtc,
                    DeliveredAtUtc = x.DeliveredAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsDhl = x.IsDhl,
                    NeedsCarrierReview = x.NeedsCarrierReview,
                    NeedsReturnFollowUp = x.NeedsReturnFollowUp,
                    AwaitingHandoff = x.AwaitingHandoff,
                    TrackingOverdue = x.TrackingOverdue,
                    OpenAgeHours = x.OpenAgeHours,
                    InTransitAgeHours = x.InTransitAgeHours,
                    LastCarrierEventAtUtc = x.LastCarrierEventAtUtc,
                    LastCarrierEventKey = x.LastCarrierEventKey,
                    RecentCarrierEvents = x.RecentCarrierEvents.Select(e => new ShipmentCarrierEventVm
                    {
                        CarrierEventKey = e.CarrierEventKey,
                        ProviderStatus = e.ProviderStatus,
                        TrackingNumber = e.TrackingNumber,
                        LabelUrl = e.LabelUrl,
                        Service = e.Service,
                        OccurredAtUtc = e.OccurredAtUtc
                    }).ToList(),
                    TrackingState = x.TrackingState,
                    ExceptionNote = x.ExceptionNote,
                    AttentionDelayHours = x.AttentionDelayHours,
                    TrackingGraceHours = x.TrackingGraceHours,
                    DefaultRefundPaymentId = x.DefaultRefundPaymentId,
                    HasRefundablePayment = x.HasRefundablePayment,
                    ProviderOperationQueued = x.ProviderOperationQueued,
                    ProviderOperationFailed = x.ProviderOperationFailed,
                    ProviderOperationType = x.ProviderOperationType,
                    ProviderOperationFailureReason = x.ProviderOperationFailureReason,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_ShipmentsGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Refunds(Guid orderId, int page = 1, int pageSize = 10, RefundQueueFilter filter = RefundQueueFilter.All, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(T("OrderNotFound"));
            }

            var order = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            var (items, total) = await _getOrderRefundsPage.HandleAsync(orderId, page, pageSize, filter, ct).ConfigureAwait(false);
            var vm = new OrderRefundsPageVm
            {
                OrderId = orderId,
                ReturnedShipmentCount = order?.Shipments.Count(x => x.Status == ShipmentStatus.Returned) ?? 0,
                DefaultRefundPaymentId = ResolveDefaultRefundPaymentId(order),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Filter = filter,
                FilterItems = BuildRefundFilterItems(filter),
                Items = items.Select(x => new RefundListItemVm
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
                    PaymentProvider = x.PaymentProvider,
                    PaymentProviderReference = x.PaymentProviderReference,
                    PaymentStatus = x.PaymentStatus,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_RefundsGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(Guid orderId, int page = 1, int pageSize = 10, InvoiceQueueFilter filter = InvoiceQueueFilter.All, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(T("OrderNotFound"));
            }

            var (items, total) = await _getOrderInvoicesPage.HandleAsync(orderId, page, pageSize, filter, ct).ConfigureAwait(false);
            var vm = new OrderInvoicesPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Filter = filter,
                FilterItems = BuildInvoiceFilterItems(filter),
                Items = items.Select(x => new OrderInvoiceListItemVm
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
                    PaymentProvider = x.PaymentProvider,
                    PaymentProviderReference = x.PaymentProviderReference,
                    PaymentStatus = x.PaymentStatus,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CustomerTaxProfileType = x.CustomerTaxProfileType,
                    CustomerVatId = x.CustomerVatId,
                    Currency = x.Currency,
                    TotalNetMinor = x.TotalNetMinor,
                    TotalTaxMinor = x.TotalTaxMinor,
                    TotalGrossMinor = x.TotalGrossMinor,
                    RefundedAmountMinor = x.RefundedAmountMinor,
                    SettledAmountMinor = x.SettledAmountMinor,
                    BalanceMinor = x.BalanceMinor,
                    Status = x.Status,
                    IssuedAtUtc = x.IssuedAtUtc,
                    DueAtUtc = x.DueAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_InvoicesGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> AddPayment(Guid orderId, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new PaymentCreateVm
            {
                OrderId = dto.Id,
                Currency = dto.Currency
            };

            SetOrderHeader(CreateHeader(dto));
            return RenderPaymentEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(PaymentCreateVm vm, CancellationToken ct = default)
        {
            if (vm.OrderId == Guid.Empty)
            {
                SetErrorMessage("OrderPaymentAddFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderPaymentEditor(vm);
            }

            var dto = new PaymentCreateDto
            {
                OrderId = vm.OrderId,
                Provider = vm.Provider,
                ProviderReference = vm.ProviderReference,
                AmountMinor = vm.AmountMinor,
                Currency = vm.Currency,
                Status = vm.Status,
                FailureReason = vm.Status == PaymentStatus.Failed ? vm.FailureReason : null
            };

            try
            {
                await _addPayment.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("OrderPaymentAdded");
            }
            catch (Exception)
            {
                AddModelErrorMessage("OrderPaymentAddFailed");
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderPaymentEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> AddShipment(Guid orderId, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new ShipmentCreateVm
            {
                OrderId = dto.Id,
                Carrier = dto.ShippingCarrier ?? string.Empty,
                Service = dto.ShippingService ?? string.Empty,
                Lines = dto.Lines.Select(x => new ShipmentLineCreateVm
                {
                    OrderLineId = x.Id,
                    Label = $"{x.Sku} - {x.Name}",
                    Quantity = x.Quantity
                }).ToList()
            };

            SetOrderHeader(CreateHeader(dto));
            return RenderShipmentEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShipment(ShipmentCreateVm vm, CancellationToken ct = default)
        {
            if (vm.OrderId == Guid.Empty)
            {
                SetErrorMessage("OrderShipmentAddFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderShipmentEditor(vm);
            }

            var dto = new ShipmentCreateDto
            {
                OrderId = vm.OrderId,
                Carrier = vm.Carrier,
                Service = vm.Service,
                ProviderShipmentReference = vm.ProviderShipmentReference,
                TrackingNumber = vm.TrackingNumber,
                LabelUrl = vm.LabelUrl,
                LastCarrierEventKey = vm.LastCarrierEventKey,
                TotalWeight = vm.TotalWeight,
                Lines = vm.Lines
                    .Where(x => x.Quantity > 0)
                    .Select(x => new ShipmentLineCreateDto
                    {
                        OrderLineId = x.OrderLineId,
                        Quantity = x.Quantity
                    })
                    .ToList()
            };

            try
            {
                await _addShipment.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("OrderShipmentAdded");
            }
            catch (Exception)
            {
                AddModelErrorMessage("OrderShipmentAddFailed");
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderShipmentEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> AddRefund(Guid orderId, Guid? paymentId = null, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new RefundCreateVm
            {
                OrderId = dto.Id,
                Currency = dto.Currency,
                PaymentId = paymentId ?? Guid.Empty,
                PaymentOptions = dto.Payments.Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.Provider} | {x.Currency} {(x.AmountMinor / 100.0M):0.00} | {x.Status}",
                    Selected = paymentId.HasValue && paymentId.Value == x.Id
                }).ToList()
            };

            SetOrderHeader(CreateHeader(dto));
            return RenderRefundEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDhlLabel(
            Guid shipmentId,
            Guid orderId,
            string? rowVersion,
            bool returnToQueue = false,
            ShipmentQueueFilter filter = ShipmentQueueFilter.All,
            string? query = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            if (shipmentId == Guid.Empty || orderId == Guid.Empty)
            {
                SetErrorMessage("DhlLabelGenerationFailed");
                return returnToQueue
                    ? RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter })
                    : RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                var version = DecodeBase64RowVersion(rowVersion);
                if (version.Length == 0)
                {
                    SetErrorMessage("DhlLabelGenerationFailed");
                    return returnToQueue
                        ? RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter })
                        : RedirectOrHtmx(nameof(Details), new { id = orderId });
                }

                await _generateDhlShipmentLabel.HandleAsync(shipmentId, version, ct).ConfigureAwait(false);
                SetSuccessMessage("DhlLabelGenerationQueued");
            }
            catch (Exception)
            {
                SetErrorMessage("DhlLabelGenerationFailed");
            }

            if (returnToQueue)
            {
                return RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter });
            }

            return RedirectOrHtmx(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveShipmentCarrierException(
            Guid shipmentId,
            string rowVersion,
            string? resolutionNote = null,
            bool returnToReturnsQueue = false,
            ShipmentQueueFilter filter = ShipmentQueueFilter.All,
            ReturnQueueFilter returnFilter = ReturnQueueFilter.All,
            string? query = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            if (shipmentId == Guid.Empty)
            {
                SetErrorMessage("ShipmentCarrierExceptionResolveFailedMessage");
                return returnToReturnsQueue
                    ? RedirectOrHtmx(nameof(ReturnsQueue), new { page, pageSize, query, filter = returnFilter })
                    : RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter });
            }

            var version = DecodeBase64RowVersion(rowVersion);
            if (version.Length == 0)
            {
                SetErrorMessage("ConcurrencyTokenMissing");
                return returnToReturnsQueue
                    ? RedirectOrHtmx(nameof(ReturnsQueue), new { page, pageSize, query, filter = returnFilter })
                    : RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter });
            }

            var result = await _resolveShipmentCarrierException
                .HandleAsync(new ResolveShipmentCarrierExceptionDto
                {
                    ShipmentId = shipmentId,
                    RowVersion = version,
                    ResolutionNote = string.IsNullOrWhiteSpace(resolutionNote)
                        ? T("ShipmentCarrierExceptionResolvedByOperator")
                        : resolutionNote
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("ShipmentCarrierExceptionResolvedMessage");
            }
            else
            {
                SetErrorMessage("ShipmentCarrierExceptionResolveFailedMessage");
            }

            if (returnToReturnsQueue)
            {
                return RedirectOrHtmx(nameof(ReturnsQueue), new { page, pageSize, query, filter = returnFilter });
            }

            return RedirectOrHtmx(nameof(ShipmentsQueue), new { page, pageSize, query, filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShipmentProviderOperation(
            Guid id,
            string rowVersion,
            string action,
            string? failureReason = null,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? provider = null,
            string? operationType = null,
            string? status = null,
            bool stalePendingOnly = false,
            bool failedOnly = false,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("ShipmentProviderOperationUpdateFailedMessage");
                return RedirectOrHtmx(nameof(ShipmentProviderOperations), new
                {
                    page,
                    pageSize,
                    query,
                    provider,
                    operationType,
                    status,
                    stalePendingOnly,
                    failedOnly
                });
            }

            var version = DecodeBase64RowVersion(rowVersion);
            if (version.Length == 0)
            {
                SetErrorMessage("ConcurrencyTokenMissing");
                return RedirectOrHtmx(nameof(ShipmentProviderOperations), new
                {
                    page,
                    pageSize,
                    query,
                    provider,
                    operationType,
                    status,
                    stalePendingOnly,
                    failedOnly
                });
            }

            var result = await _updateShipmentProviderOperation
                .HandleAsync(new UpdateShipmentProviderOperationDto
                {
                    Id = id,
                    RowVersion = version,
                    Action = action,
                    FailureReason = failureReason
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage(action switch
                {
                    "MarkProcessed" => "ShipmentProviderOperationMarkedProcessedMessage",
                    "MarkFailed" => "ShipmentProviderOperationMarkedFailedMessage",
                    "Requeue" => "ShipmentProviderOperationRequeuedMessage",
                    "Cancel" => "ShipmentProviderOperationCancelledMessage",
                    _ => "ShipmentProviderOperationUpdatedMessage"
                });
            }
            else
            {
                SetErrorMessage("ShipmentProviderOperationUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(ShipmentProviderOperations), new
            {
                page,
                pageSize,
                query,
                provider,
                operationType,
                status,
                stalePendingOnly,
                failedOnly
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRefund(RefundCreateVm vm, CancellationToken ct = default)
        {
            if (vm.OrderId == Guid.Empty || vm.PaymentId == Guid.Empty)
            {
                SetErrorMessage("OrderRefundAddFailed");
                return vm.OrderId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmxDetails(vm.OrderId);
            }

            if (!ModelState.IsValid)
            {
                await PopulateRefundOptionsAsync(vm, ct).ConfigureAwait(false);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderRefundEditor(vm);
            }

            try
            {
                await _addRefund.HandleAsync(new RefundCreateDto
                {
                    OrderId = vm.OrderId,
                    PaymentId = vm.PaymentId,
                    AmountMinor = vm.AmountMinor,
                    Currency = vm.Currency,
                    Reason = vm.Reason
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("OrderRefundAdded");
            }
            catch (Exception)
            {
                AddModelErrorMessage("OrderRefundAddFailed");
                await PopulateRefundOptionsAsync(vm, ct).ConfigureAwait(false);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderRefundEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvoice(Guid orderId, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var businessOptions = await _getBusinessLookup.HandleAsync(ct).ConfigureAwait(false);
            var customerOptions = await _getCustomerLookup.HandleAsync(ct).ConfigureAwait(false);
            var nowUtc = _clock.UtcNow;

            var vm = new OrderInvoiceCreateVm
            {
                OrderId = dto.Id,
                DueAtUtc = nowUtc.AddDays(14)
            };

            await PopulateOrderInvoiceOptionsAsync(vm, businessOptions, customerOptions, dto).ConfigureAwait(false);
            SetOrderHeader(CreateHeader(dto));
            return RenderInvoiceCreateEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(OrderInvoiceCreateVm vm, CancellationToken ct = default)
        {
            if (vm.OrderId == Guid.Empty || vm.BusinessId == Guid.Empty || vm.CustomerId == Guid.Empty)
            {
                SetErrorMessage("OrderInvoiceCreateFailed");
                return vm.OrderId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmxDetails(vm.OrderId);
            }

            if (!ModelState.IsValid)
            {
                await PopulateOrderInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderInvoiceCreateEditor(vm);
            }

            try
            {
                await _createOrderInvoice.HandleAsync(new OrderInvoiceCreateDto
                {
                    OrderId = vm.OrderId,
                    BusinessId = vm.BusinessId,
                    CustomerId = vm.CustomerId,
                    PaymentId = vm.PaymentId,
                    DueAtUtc = vm.DueAtUtc
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("OrderInvoiceCreated");
            }
            catch (Exception)
            {
                AddModelErrorMessage("OrderInvoiceCreateFailed");
                await PopulateOrderInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderInvoiceCreateEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(OrderStatusChangeVm vm, CancellationToken ct = default)
        {
            if (vm.OrderId == Guid.Empty)
            {
                SetErrorMessage("OrderNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (vm.RowVersion is null || vm.RowVersion.Length == 0)
            {
                SetErrorMessage("ConcurrencyTokenMissing");
                return RedirectOrHtmxDetails(vm.OrderId);
            }

            try
            {
                await _updateOrderStatus.HandleAsync(new UpdateOrderStatusDto
                {
                    OrderId = vm.OrderId,
                    RowVersion = vm.RowVersion,
                    NewStatus = vm.NewStatus,
                    WarehouseId = vm.WarehouseId
                }, ct).ConfigureAwait(false);

                TempData["Success"] = string.Format(T("OrderStatusUpdatedFormat"), vm.NewStatus);
            }
            catch (ValidationException ex)
            {
                SetValidationErrorMessage(ex, "OrderStatusUpdateFailed");
            }
            catch (Exception)
            {
                SetErrorMessage("OrderStatusUpdateFailed");
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        private IEnumerable<SelectListItem> BuildOrderFilterItems(OrderQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllOrders"), OrderQueueFilter.All.ToString(), selectedFilter == OrderQueueFilter.All);
            yield return new SelectListItem(T("Open"), OrderQueueFilter.Open.ToString(), selectedFilter == OrderQueueFilter.Open);
            yield return new SelectListItem(T("PaymentIssues"), OrderQueueFilter.PaymentIssues.ToString(), selectedFilter == OrderQueueFilter.PaymentIssues);
            yield return new SelectListItem(T("FulfillmentAttention"), OrderQueueFilter.FulfillmentAttention.ToString(), selectedFilter == OrderQueueFilter.FulfillmentAttention);
        }

        private IEnumerable<SelectListItem> BuildPaymentFilterItems(PaymentQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllPayments"), PaymentQueueFilter.All.ToString(), selectedFilter == PaymentQueueFilter.All);
            yield return new SelectListItem(T("Failed"), PaymentQueueFilter.Failed.ToString(), selectedFilter == PaymentQueueFilter.Failed);
            yield return new SelectListItem(T("Refunded"), PaymentQueueFilter.Refunded.ToString(), selectedFilter == PaymentQueueFilter.Refunded);
        }

        private IEnumerable<SelectListItem> BuildShipmentFilterItems(ShipmentQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllShipments"), ShipmentQueueFilter.All.ToString(), selectedFilter == ShipmentQueueFilter.All);
            yield return new SelectListItem(T("PendingPacked"), ShipmentQueueFilter.Pending.ToString(), selectedFilter == ShipmentQueueFilter.Pending);
            yield return new SelectListItem(T("ShippedDelivered"), ShipmentQueueFilter.Shipped.ToString(), selectedFilter == ShipmentQueueFilter.Shipped);
            yield return new SelectListItem(T("MissingTracking"), ShipmentQueueFilter.MissingTracking.ToString(), selectedFilter == ShipmentQueueFilter.MissingTracking);
            yield return new SelectListItem(T("Returned"), ShipmentQueueFilter.Returned.ToString(), selectedFilter == ShipmentQueueFilter.Returned);
            yield return new SelectListItem(T("DhlMethods"), ShipmentQueueFilter.Dhl.ToString(), selectedFilter == ShipmentQueueFilter.Dhl);
            yield return new SelectListItem(T("MissingService"), ShipmentQueueFilter.MissingService.ToString(), selectedFilter == ShipmentQueueFilter.MissingService);
            yield return new SelectListItem(T("AwaitingHandoff"), ShipmentQueueFilter.AwaitingHandoff.ToString(), selectedFilter == ShipmentQueueFilter.AwaitingHandoff);
            yield return new SelectListItem(T("TrackingOverdue"), ShipmentQueueFilter.TrackingOverdue.ToString(), selectedFilter == ShipmentQueueFilter.TrackingOverdue);
            yield return new SelectListItem(T("CarrierReview"), ShipmentQueueFilter.CarrierReview.ToString(), selectedFilter == ShipmentQueueFilter.CarrierReview);
            yield return new SelectListItem(T("ReturnFollowUp"), ShipmentQueueFilter.ReturnFollowUp.ToString(), selectedFilter == ShipmentQueueFilter.ReturnFollowUp);
        }

        private IEnumerable<SelectListItem> BuildRefundFilterItems(RefundQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllRefunds"), RefundQueueFilter.All.ToString(), selectedFilter == RefundQueueFilter.All);
            yield return new SelectListItem(T("Pending"), RefundQueueFilter.Pending.ToString(), selectedFilter == RefundQueueFilter.Pending);
            yield return new SelectListItem(T("Completed"), RefundQueueFilter.Completed.ToString(), selectedFilter == RefundQueueFilter.Completed);
        }

        private IEnumerable<SelectListItem> BuildReturnFilterItems(ReturnQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllReturnCases"), ReturnQueueFilter.All.ToString(), selectedFilter == ReturnQueueFilter.All);
            yield return new SelectListItem(T("ReturnFollowUp"), ReturnQueueFilter.FollowUp.ToString(), selectedFilter == ReturnQueueFilter.FollowUp);
            yield return new SelectListItem(T("CarrierReview"), ReturnQueueFilter.CarrierReview.ToString(), selectedFilter == ReturnQueueFilter.CarrierReview);
        }

        private IEnumerable<SelectListItem> BuildShipmentProviderItems(IEnumerable<string> providers, string? selectedProvider)
        {
            yield return new SelectListItem(T("CommunicationProviderAll"), string.Empty, string.IsNullOrWhiteSpace(selectedProvider));
            foreach (var provider in providers
                         .Concat(string.IsNullOrWhiteSpace(selectedProvider) ? Array.Empty<string>() : new[] { selectedProvider! })
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                yield return new SelectListItem(provider, provider, string.Equals(selectedProvider, provider, StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<SelectListItem> BuildShipmentOperationTypeItems(IEnumerable<string> operationTypes, string? selectedOperationType)
        {
            yield return new SelectListItem(T("ShipmentProviderOperationTypeAll"), string.Empty, string.IsNullOrWhiteSpace(selectedOperationType));
            foreach (var operationType in operationTypes
                         .Concat(string.IsNullOrWhiteSpace(selectedOperationType) ? Array.Empty<string>() : new[] { selectedOperationType! })
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                yield return new SelectListItem(operationType, operationType, string.Equals(selectedOperationType, operationType, StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<SelectListItem> BuildShipmentProviderOperationStatusItems(string? selectedStatus)
        {
            yield return new SelectListItem(T("CommunicationAuditStatusAll"), string.Empty, string.IsNullOrWhiteSpace(selectedStatus));
            yield return new SelectListItem(T("Pending"), "Pending", string.Equals(selectedStatus, "Pending", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("Failed"), "Failed", string.Equals(selectedStatus, "Failed", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("Processed"), "Processed", string.Equals(selectedStatus, "Processed", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildInvoiceFilterItems(InvoiceQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllInvoices"), InvoiceQueueFilter.All.ToString(), selectedFilter == InvoiceQueueFilter.All);
            yield return new SelectListItem(T("Outstanding"), InvoiceQueueFilter.Outstanding.ToString(), selectedFilter == InvoiceQueueFilter.Outstanding);
            yield return new SelectListItem(T("Paid"), InvoiceQueueFilter.Paid.ToString(), selectedFilter == InvoiceQueueFilter.Paid);
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }

        private static ReturnSupportBaselineVm BuildReturnSupportBaseline(OrderDetailDto dto)
        {
            return new ReturnSupportBaselineVm
            {
                ReturnedShipmentCount = dto.Shipments.Count(x => x.Status == ShipmentStatus.Returned),
                ReturnedWithoutTrackingCount = dto.Shipments.Count(x => x.Status == ShipmentStatus.Returned && string.IsNullOrWhiteSpace(x.TrackingNumber)),
                CarrierReviewShipmentCount = dto.Shipments.Count(x =>
                    x.Status == ShipmentStatus.Returned ||
                    ((x.Status == ShipmentStatus.Shipped || x.Status == ShipmentStatus.Delivered) && string.IsNullOrWhiteSpace(x.TrackingNumber)) ||
                    string.IsNullOrWhiteSpace(x.Service)),
                HasRefundablePayment = ResolveDefaultRefundPaymentId(dto).HasValue,
                DefaultRefundPaymentId = ResolveDefaultRefundPaymentId(dto)
            };
        }

        private static Guid? ResolveDefaultRefundPaymentId(OrderDetailDto? dto)
        {
            return dto?.Payments
                .Where(x =>
                    x.Status == PaymentStatus.Captured ||
                    x.Status == PaymentStatus.Completed ||
                    x.Status == PaymentStatus.Refunded)
                .OrderByDescending(x => x.CapturedAtUtc ?? DateTime.MinValue)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();
        }

        private static OrderHeaderVm CreateHeader(OrderDetailDto dto)
        {
            return new OrderHeaderVm
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                Currency = dto.Currency,
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor
            };
        }

        private IActionResult RenderPaymentEditor(PaymentCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/_PaymentCreateShell.cshtml", vm);
            }

            return View("AddPayment", vm);
        }

        private IActionResult RenderShipmentEditor(ShipmentCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/_ShipmentCreateShell.cshtml", vm);
            }

            return View("AddShipment", vm);
        }

        private IActionResult RenderRefundEditor(RefundCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/_RefundCreateShell.cshtml", vm);
            }

            return View("AddRefund", vm);
        }

        private IActionResult RenderInvoiceCreateEditor(OrderInvoiceCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/_InvoiceCreateShell.cshtml", vm);
            }

            return View("CreateInvoice", vm);
        }

        private IActionResult RenderShipmentsQueueWorkspace(ShipmentsQueueVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/ShipmentsQueue.cshtml", vm);
            }

            return View("ShipmentsQueue", vm);
        }

        private IActionResult RenderReturnsQueueWorkspace(ReturnsQueueVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/ReturnsQueue.cshtml", vm);
            }

            return View("ReturnsQueue", vm);
        }

        private IActionResult RenderShipmentProviderOperationsWorkspace(ShipmentProviderOperationsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/ShipmentProviderOperations.cshtml", vm);
            }

            return View("ShipmentProviderOperations", vm);
        }

        private IActionResult RenderOrdersWorkspace(OrdersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderOrderDetailsWorkspace(OrderDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Orders/Details.cshtml", vm);
            }

            return View("Details", vm);
        }

        private IActionResult RedirectOrHtmxDetails(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(nameof(Details), new { id = orderId }) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        private IActionResult RedirectOrHtmx(string actionName, object routeValues)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, routeValues);
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private void SetOrderHeader(OrderHeaderVm? header)
        {
            ViewData["OrderHeader"] = header;
        }

        private async Task<OrderHeaderVm?> GetOrderHeaderAsync(Guid orderId, CancellationToken ct)
        {
            if (orderId == Guid.Empty)
            {
                return null;
            }

            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            return dto is null ? null : CreateHeader(dto);
        }

        private async Task PopulateRefundOptionsAsync(RefundCreateVm vm, CancellationToken ct)
        {
            if (vm.OrderId == Guid.Empty)
            {
                vm.PaymentOptions = new List<SelectListItem>();
                return;
            }

            var dto = await _getOrderForView.HandleAsync(vm.OrderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                vm.PaymentOptions = new List<SelectListItem>();
                return;
            }

            vm.Currency = string.IsNullOrWhiteSpace(vm.Currency) ? dto.Currency : vm.Currency;
            vm.PaymentOptions = dto.Payments.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Provider} | {x.Currency} {(x.AmountMinor / 100.0M):0.00} | {x.Status}",
                Selected = vm.PaymentId == x.Id
            }).ToList();
        }

        private async Task PopulateOrderInvoiceOptionsAsync(OrderInvoiceCreateVm vm, CancellationToken ct)
        {
            var businessOptions = await _getBusinessLookup.HandleAsync(ct).ConfigureAwait(false);
            var customerOptions = await _getCustomerLookup.HandleAsync(ct).ConfigureAwait(false);
            var orderDto = vm.OrderId == Guid.Empty
                ? null
                : await _getOrderForView.HandleAsync(vm.OrderId, ct).ConfigureAwait(false);
            await PopulateOrderInvoiceOptionsAsync(vm, businessOptions, customerOptions, orderDto).ConfigureAwait(false);
        }

        private Task PopulateOrderInvoiceOptionsAsync(
            OrderInvoiceCreateVm vm,
            IReadOnlyCollection<LookupItemDto> businessOptions,
            IReadOnlyCollection<LookupItemDto> customerOptions,
            OrderDetailDto? orderDto)
        {
            vm.BusinessOptions = businessOptions.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.SecondaryLabel is null ? x.Label : $"{x.Label} ({x.SecondaryLabel})",
                Selected = vm.BusinessId == x.Id
            }).Prepend(new SelectListItem
            {
                Value = string.Empty,
                Text = "No business scope",
                Selected = !vm.BusinessId.HasValue
            }).ToList();

            vm.CustomerOptions = customerOptions.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.SecondaryLabel is null ? x.Label : $"{x.Label} ({x.SecondaryLabel})",
                Selected = vm.CustomerId == x.Id
            }).Prepend(new SelectListItem
            {
                Value = string.Empty,
                Text = "Auto-resolve from user",
                Selected = !vm.CustomerId.HasValue
            }).ToList();

            vm.PaymentOptions = (orderDto?.Payments ?? new List<PaymentDetailDto>()).Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Provider} | {x.Currency} {(x.AmountMinor / 100.0M):0.00} | {x.Status}",
                Selected = vm.PaymentId == x.Id
            }).Prepend(new SelectListItem
            {
                Value = string.Empty,
                Text = "No linked payment",
                Selected = !vm.PaymentId.HasValue
            }).ToList();

            return Task.CompletedTask;
        }
    }
}
