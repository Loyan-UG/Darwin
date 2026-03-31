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
        private readonly AddRefundHandler _addRefund;
        private readonly CreateOrderInvoiceHandler _createOrderInvoice;
        private readonly UpdateOrderStatusHandler _updateOrderStatus;
        private readonly ISiteSettingCache _siteSettingCache;

        public OrdersController(
            GetOrdersPageHandler getOrdersPage,
            GetShipmentsPageHandler getShipmentsPage,
            GetShipmentOpsSummaryHandler getShipmentOpsSummary,
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
            AddRefundHandler addRefund,
            CreateOrderInvoiceHandler createOrderInvoice,
            UpdateOrderStatusHandler updateOrderStatus,
            ISiteSettingCache siteSettingCache)
        {
            _getOrdersPage = getOrdersPage;
            _getShipmentsPage = getShipmentsPage;
            _getShipmentOpsSummary = getShipmentOpsSummary;
            _getOrderForView = getOrderForView;
            _getOrderPaymentsPage = getOrderPaymentsPage;
            _getOrderShipmentsPage = getOrderShipmentsPage;
            _getOrderRefundsPage = getOrderRefundsPage;
            _getOrderInvoicesPage = getOrderInvoicesPage;
            _getWarehouseLookup = getWarehouseLookup;
            _getBusinessLookup = getBusinessLookup;
            _getCustomerLookup = getCustomerLookup;
            _addPayment = addPayment;
            _addShipment = addShipment;
            _addRefund = addRefund;
            _createOrderInvoice = createOrderInvoice;
            _updateOrderStatus = updateOrderStatus;
            _siteSettingCache = siteSettingCache;
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
                    TrackingNumber = x.TrackingNumber,
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
                    TrackingState = x.TrackingState,
                    ExceptionNote = x.ExceptionNote,
                    AttentionDelayHours = x.AttentionDelayHours,
                    TrackingGraceHours = x.TrackingGraceHours,
                    DefaultRefundPaymentId = x.DefaultRefundPaymentId,
                    HasRefundablePayment = x.HasRefundablePayment,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderShipmentsQueueWorkspace(vm);
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
                EnvironmentLabel = string.IsNullOrWhiteSpace(settings.DhlEnvironment) ? "Not set" : settings.DhlEnvironment,
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

        private static TaxPolicySnapshotVm MapTaxPolicy(Darwin.Application.Settings.DTOs.SiteSettingDto dto)
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

            return new TaxPolicySnapshotVm
            {
                VatEnabled = dto.VatEnabled,
                DefaultVatRatePercent = dto.DefaultVatRatePercent,
                PricesIncludeVat = dto.PricesIncludeVat,
                AllowReverseCharge = dto.AllowReverseCharge,
                IssuerConfigured = issuerConfigured,
                InvoiceIssuerLegalName = dto.InvoiceIssuerLegalName ?? string.Empty,
                InvoiceIssuerTaxIdConfigured = issuerTaxIdConfigured,
                ArchiveReadinessComplete = archiveReady,
                ArchiveReadinessLabel = archiveReady ? "Issuer archive-ready" : "Archive issuer data incomplete",
                EInvoiceBaselineReady = eInvoiceBaselineReady,
                EInvoiceBaselineLabel = eInvoiceBaselineReady ? "Baseline ready" : "Baseline incomplete",
                ComplianceScopeNote = "Phase-1 exposes issuer/VAT readiness only. Archive and e-invoice workflows still require deeper compliance implementation."
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

        private static List<ShipmentPlaybookVm> BuildShipmentPlaybooks()
        {
            return new List<ShipmentPlaybookVm>
            {
                new()
                {
                    Title = "Pending / packed shipments",
                    ScopeNote = "Use this queue to push open fulfillment items toward handoff.",
                    OperatorAction = "Open the order, confirm shipment composition, and add or correct shipment data before carrier handoff.",
                    SettingsDependency = "DHL shipper identity and account setup should be green before the queue is treated as ready for live carrier operations."
                },
                new()
                {
                    Title = "Awaiting handoff",
                    ScopeNote = "These shipments have stayed pending or packed longer than the configured attention threshold.",
                    OperatorAction = "Review warehouse and carrier prep, then either add the shipment handoff data or escalate the operational blockage.",
                    SettingsDependency = "Controlled by the Shipment Attention Delay setting in Shipping settings."
                },
                new()
                {
                    Title = "Tracking overdue",
                    ScopeNote = "These DHL rows passed the tracking grace window without a carrier reference.",
                    OperatorAction = "Confirm the carrier actually accepted the parcel and update the tracking number or escalate to carrier support.",
                    SettingsDependency = "Controlled by the Shipment Tracking Grace setting in Shipping settings."
                },
                new()
                {
                    Title = "Missing tracking",
                    ScopeNote = "These are shipped or delivered records without tracking context.",
                    OperatorAction = "Review the order shipment tab and update shipment data through the order workflow when the carrier reference becomes available.",
                    SettingsDependency = "DHL API/account readiness should be validated first if missing tracking reflects broader provider onboarding issues."
                },
                new()
                {
                    Title = "DHL rows with incomplete carrier data",
                    ScopeNote = "Use this when DHL shipments exist but service metadata or tracking discipline is weak.",
                    OperatorAction = "Open the linked order, confirm the intended DHL service, and normalize shipment data before handoff or support escalation.",
                    SettingsDependency = "DHL environment, account number, and shipper identity should be configured before these rows are treated as ready for production handoff."
                },
                new()
                {
                    Title = "Carrier review queue",
                    ScopeNote = "Use this subset for DHL rows with missing service data, missing tracking after shipment, or explicit returned status that needs carrier-facing follow-up.",
                    OperatorAction = "Start from the shipment row, confirm service and tracking timeline, then move into the order workflow only after the carrier-facing facts are clear.",
                    SettingsDependency = "DHL environment, account number, shipper identity, and shipment thresholds should be configured before treating this queue as a trustworthy carrier-exception surface."
                },
                new()
                {
                    Title = "Returned shipments",
                    ScopeNote = "Treat these as return-support follow-up, not just delivery completion.",
                    OperatorAction = "Open the order and coordinate refund, restock, or customer-support follow-up from the linked order workspace.",
                    SettingsDependency = "DHL readiness alone does not complete returns; this queue is a phase-1 operational signal until full RMA tooling lands."
                },
                new()
                {
                    Title = "Return follow-up queue",
                    ScopeNote = "Use this subset for returned shipments that still need refund-path or carrier-completion review.",
                    OperatorAction = "Open refunds from the shipment row, start a return refund when a refundable payment exists, and keep carrier review attached if tracking or service evidence is still incomplete.",
                    SettingsDependency = "DHL readiness and visible refundable payments are both needed before treating returned rows as operationally closed."
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(id, ct).ConfigureAwait(false);
            var warehouses = await _getWarehouseLookup.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
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
                    TrackingNumber = x.TrackingNumber,
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
                    TrackingState = x.TrackingState,
                    ExceptionNote = x.ExceptionNote,
                    AttentionDelayHours = x.AttentionDelayHours,
                    TrackingGraceHours = x.TrackingGraceHours,
                    DefaultRefundPaymentId = x.DefaultRefundPaymentId,
                    HasRefundablePayment = x.HasRefundablePayment,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_ShipmentsGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Refunds(Guid orderId, int page = 1, int pageSize = 10, RefundQueueFilter filter = RefundQueueFilter.All, CancellationToken ct = default)
        {
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
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
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
                TempData["Success"] = "Payment added.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderPaymentEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> AddShipment(Guid orderId, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new ShipmentCreateVm
            {
                OrderId = dto.Id,
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
                TrackingNumber = vm.TrackingNumber,
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
                TempData["Success"] = "Shipment added.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderShipmentEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> AddRefund(Guid orderId, Guid? paymentId = null, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
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
        public async Task<IActionResult> AddRefund(RefundCreateVm vm, CancellationToken ct = default)
        {
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

                TempData["Success"] = "Refund added.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateRefundOptionsAsync(vm, ct).ConfigureAwait(false);
                SetOrderHeader(await GetOrderHeaderAsync(vm.OrderId, ct).ConfigureAwait(false));
                return RenderRefundEditor(vm);
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvoice(Guid orderId, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var businessOptions = await _getBusinessLookup.HandleAsync(ct).ConfigureAwait(false);
            var customerOptions = await _getCustomerLookup.HandleAsync(ct).ConfigureAwait(false);

            var vm = new OrderInvoiceCreateVm
            {
                OrderId = dto.Id,
                DueAtUtc = DateTime.UtcNow.AddDays(14)
            };

            await PopulateOrderInvoiceOptionsAsync(vm, businessOptions, customerOptions, dto).ConfigureAwait(false);
            SetOrderHeader(CreateHeader(dto));
            return RenderInvoiceCreateEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(OrderInvoiceCreateVm vm, CancellationToken ct = default)
        {
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

                TempData["Success"] = "Invoice created.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
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
            if (vm.RowVersion is null || vm.RowVersion.Length == 0)
            {
                TempData["Error"] = "Missing concurrency token.";
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

                TempData["Success"] = $"Order status updated to {vm.NewStatus}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmxDetails(vm.OrderId);
        }

        private static IEnumerable<SelectListItem> BuildOrderFilterItems(OrderQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All orders", OrderQueueFilter.All.ToString(), selectedFilter == OrderQueueFilter.All);
            yield return new SelectListItem("Open", OrderQueueFilter.Open.ToString(), selectedFilter == OrderQueueFilter.Open);
            yield return new SelectListItem("Payment issues", OrderQueueFilter.PaymentIssues.ToString(), selectedFilter == OrderQueueFilter.PaymentIssues);
            yield return new SelectListItem("Fulfillment attention", OrderQueueFilter.FulfillmentAttention.ToString(), selectedFilter == OrderQueueFilter.FulfillmentAttention);
        }

        private static IEnumerable<SelectListItem> BuildPaymentFilterItems(PaymentQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All payments", PaymentQueueFilter.All.ToString(), selectedFilter == PaymentQueueFilter.All);
            yield return new SelectListItem("Failed", PaymentQueueFilter.Failed.ToString(), selectedFilter == PaymentQueueFilter.Failed);
            yield return new SelectListItem("Refunded", PaymentQueueFilter.Refunded.ToString(), selectedFilter == PaymentQueueFilter.Refunded);
        }

        private static IEnumerable<SelectListItem> BuildShipmentFilterItems(ShipmentQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All shipments", ShipmentQueueFilter.All.ToString(), selectedFilter == ShipmentQueueFilter.All);
            yield return new SelectListItem("Pending/Packed", ShipmentQueueFilter.Pending.ToString(), selectedFilter == ShipmentQueueFilter.Pending);
            yield return new SelectListItem("Shipped/Delivered", ShipmentQueueFilter.Shipped.ToString(), selectedFilter == ShipmentQueueFilter.Shipped);
            yield return new SelectListItem("Missing tracking", ShipmentQueueFilter.MissingTracking.ToString(), selectedFilter == ShipmentQueueFilter.MissingTracking);
            yield return new SelectListItem("Returned", ShipmentQueueFilter.Returned.ToString(), selectedFilter == ShipmentQueueFilter.Returned);
            yield return new SelectListItem("DHL", ShipmentQueueFilter.Dhl.ToString(), selectedFilter == ShipmentQueueFilter.Dhl);
            yield return new SelectListItem("Missing service", ShipmentQueueFilter.MissingService.ToString(), selectedFilter == ShipmentQueueFilter.MissingService);
            yield return new SelectListItem("Awaiting handoff", ShipmentQueueFilter.AwaitingHandoff.ToString(), selectedFilter == ShipmentQueueFilter.AwaitingHandoff);
            yield return new SelectListItem("Tracking overdue", ShipmentQueueFilter.TrackingOverdue.ToString(), selectedFilter == ShipmentQueueFilter.TrackingOverdue);
            yield return new SelectListItem("Carrier review", ShipmentQueueFilter.CarrierReview.ToString(), selectedFilter == ShipmentQueueFilter.CarrierReview);
            yield return new SelectListItem("Return follow-up", ShipmentQueueFilter.ReturnFollowUp.ToString(), selectedFilter == ShipmentQueueFilter.ReturnFollowUp);
        }

        private static IEnumerable<SelectListItem> BuildRefundFilterItems(RefundQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All refunds", RefundQueueFilter.All.ToString(), selectedFilter == RefundQueueFilter.All);
            yield return new SelectListItem("Pending", RefundQueueFilter.Pending.ToString(), selectedFilter == RefundQueueFilter.Pending);
            yield return new SelectListItem("Completed", RefundQueueFilter.Completed.ToString(), selectedFilter == RefundQueueFilter.Completed);
        }

        private static IEnumerable<SelectListItem> BuildInvoiceFilterItems(InvoiceQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All invoices", InvoiceQueueFilter.All.ToString(), selectedFilter == InvoiceQueueFilter.All);
            yield return new SelectListItem("Outstanding", InvoiceQueueFilter.Outstanding.ToString(), selectedFilter == InvoiceQueueFilter.Outstanding);
            yield return new SelectListItem("Paid", InvoiceQueueFilter.Paid.ToString(), selectedFilter == InvoiceQueueFilter.Paid);
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
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            return dto is null ? null : CreateHeader(dto);
        }

        private async Task PopulateRefundOptionsAsync(RefundCreateVm vm, CancellationToken ct)
        {
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
            var orderDto = await _getOrderForView.HandleAsync(vm.OrderId, ct).ConfigureAwait(false);
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
