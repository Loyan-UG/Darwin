using Darwin.Application.Common.Queries;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Enums;
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

        public OrdersController(
            GetOrdersPageHandler getOrdersPage,
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
            UpdateOrderStatusHandler updateOrderStatus)
        {
            _getOrdersPage = getOrdersPage;
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
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getOrdersPage.HandleAsync(page, pageSize, ct).ConfigureAwait(false);
            var vm = new OrdersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(o => new OrderListItemVm
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    Currency = o.Currency,
                    GrandTotalGrossMinor = o.GrandTotalGrossMinor,
                    CreatedAtUtc = o.CreatedAtUtc,
                    RowVersion = o.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(id, ct).ConfigureAwait(false);
            var warehouses = await _getWarehouseLookup.HandleAsync(ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
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
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
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

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Payments(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderPaymentsPage.HandleAsync(orderId, page, pageSize, ct).ConfigureAwait(false);
            var vm = new OrderPaymentsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
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
        public async Task<IActionResult> Shipments(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderShipmentsPage.HandleAsync(orderId, page, pageSize, ct).ConfigureAwait(false);
            var vm = new OrderShipmentsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new ShipmentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    Carrier = x.Carrier,
                    Service = x.Service,
                    TrackingNumber = x.TrackingNumber,
                    TotalWeight = x.TotalWeight,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return PartialView("~/Views/Orders/_ShipmentsGrid.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Refunds(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderRefundsPage.HandleAsync(orderId, page, pageSize, ct).ConfigureAwait(false);
            var vm = new OrderRefundsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
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
        public async Task<IActionResult> Invoices(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderInvoicesPage.HandleAsync(orderId, page, pageSize, ct).ConfigureAwait(false);
            var vm = new OrderInvoicesPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new OrderInvoiceListItemVm
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
                    PaymentProvider = x.PaymentProvider,
                    PaymentProviderReference = x.PaymentProviderReference,
                    PaymentStatus = x.PaymentStatus,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    Currency = x.Currency,
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
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> AddRefund(Guid orderId, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(orderId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new RefundCreateVm
            {
                OrderId = dto.Id,
                Currency = dto.Currency,
                PaymentOptions = dto.Payments.Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.Provider} | {x.Currency} {(x.AmountMinor / 100.0M):0.00} | {x.Status}"
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
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Details), new { id = vm.OrderId });
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

            return RedirectToAction(nameof(Details), new { id = vm.OrderId });
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

        private IActionResult RedirectOrHtmxDetails(Guid orderId)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(nameof(Details), new { id = orderId }) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
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
