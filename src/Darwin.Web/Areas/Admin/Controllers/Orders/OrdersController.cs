using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Web.Areas.Admin.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Orders
{
    /// <summary>
    /// Admin controller for Orders. 
    /// Uses Application handlers for reads/writes and maps into lightweight Admin VMs.
    /// </summary>
    [Area("Admin")]
    public sealed class OrdersController : AdminBaseController
    {
        // Queries
        private readonly GetOrdersPageHandler _getOrdersPage;
        private readonly GetOrderForViewHandler _getOrderForView;
        private readonly GetOrderPaymentsPageHandler _getOrderPaymentsPage;
        private readonly GetOrderShipmentsPageHandler _getOrderShipmentsPage;

        // Commands
        private readonly AddPaymentHandler _addPayment;
        private readonly UpdateOrderStatusHandler _updateOrderStatus;

        public OrdersController(
            GetOrdersPageHandler getOrdersPage,
            GetOrderForViewHandler getOrderForView,
            GetOrderPaymentsPageHandler getOrderPaymentsPage,
            GetOrderShipmentsPageHandler getOrderShipmentsPage,
            AddPaymentHandler addPayment,
            UpdateOrderStatusHandler updateOrderStatus)
        {
            _getOrdersPage = getOrdersPage;
            _getOrderForView = getOrderForView;
            _getOrderPaymentsPage = getOrderPaymentsPage;
            _getOrderShipmentsPage = getOrderShipmentsPage;
            _addPayment = addPayment;
            _updateOrderStatus = updateOrderStatus;
        }

        /// <summary>
        /// Paged list of orders (admin). Basic starting point for Orders area.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getOrdersPage.HandleAsync(page, pageSize, ct); // Application returns list + total (no query in v1)
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

        /// <summary>
        /// Shows order details for admin: summary + tabs for Payments and Shipments.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new OrderDetailVm
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                Status = dto.Status,
                Currency = dto.Currency,
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
                RowVersion = dto.RowVersion,
                Lines = dto.Lines.Select(l => new OrderLineVm
                {
                    VariantId = l.VariantId,
                    Name = l.Name,
                    Sku = l.Sku,
                    Quantity = l.Quantity,
                    UnitPriceGrossMinor = l.UnitPriceGrossMinor,
                    LineGrossMinor = l.LineGrossMinor
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Renders the payments grid partial for the given order (used by Details tabs + after mutations).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payments(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderPaymentsPage.HandleAsync(orderId, page, pageSize, ct);
            var vm = new OrderPaymentsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(p => new PaymentListItemVm
                {
                    Id = p.Id,
                    Provider = p.Provider,
                    ProviderReference = p.ProviderReference,
                    AmountMinor = p.AmountMinor,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAtUtc = p.CreatedAtUtc,
                    RowVersion = p.RowVersion
                }).ToList()
            };
            return PartialView("~/Areas/Admin/Views/Orders/_PaymentsGrid.cshtml", vm);
        }

        /// <summary>
        /// Renders the shipments grid partial for the given order (used by Details tabs).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Shipments(Guid orderId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _getOrderShipmentsPage.HandleAsync(orderId, page, pageSize, ct);
            var vm = new OrderShipmentsPageVm
            {
                OrderId = orderId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(s => new ShipmentListItemVm
                {
                    Id = s.Id,
                    Carrier = s.Carrier,
                    TrackingNumber = s.TrackingNumber,
                    Status = s.Status,
                    CreatedAtUtc = s.CreatedAtUtc,
                    RowVersion = s.RowVersion
                }).ToList()
            };
            return PartialView("~/Areas/Admin/Views/Orders/_ShipmentsGrid.cshtml", vm);
        }

        /// <summary>
        /// GET: Create payment page. Shows order header and a simple form.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddPayment(Guid orderId, CancellationToken ct = default)
        {
            var dto = await _getOrderForView.HandleAsync(orderId, ct);
            if (dto is null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PaymentCreateVm
            {
                OrderId = dto.Id,
                Currency = dto.Currency // currency must match order (validator enforces this in Application)
            };
            ViewBag.OrderHeader = new OrderHeaderVm
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                Currency = dto.Currency,
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor
            };
            return View(vm);
        }

        /// <summary>
        /// POST: Creates a payment (Application validates + may auto-advance status on Captured).
        /// After success, redirect to Details and let payments tab reload via partial.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(PaymentCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid payment data.";
                return RedirectToAction(nameof(AddPayment), new { orderId = vm.OrderId });
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
                await _addPayment.HandleAsync(dto, ct); // Application handler persists + optional status advance
                TempData["Success"] = "Payment added.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = vm.OrderId });
        }

        /// <summary>
        /// Changes order status with concurrency token.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(OrderStatusChangeVm vm, CancellationToken ct = default)
        {
            if (vm.RowVersion is null || vm.RowVersion.Length == 0)
            {
                TempData["Error"] = "Missing concurrency token.";
                return RedirectToAction(nameof(Details), new { id = vm.OrderId });
            }

            var dto = new UpdateOrderStatusDto
            {
                OrderId = vm.OrderId,
                RowVersion = vm.RowVersion,
                NewStatus = vm.NewStatus
            };

            try
            {
                await _updateOrderStatus.HandleAsync(dto, ct);
                TempData["Success"] = $"Order status updated to {vm.NewStatus}.";
            }
            catch (Exception ex)
            {
                // Application throws ValidationException on policy/amount/currency/concurrency errors; we show message as-is.
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = vm.OrderId });
        }
    }
}
