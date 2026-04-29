using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.StateMachine;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Changes an order status and orchestrates stock movements around key transitions:
    /// - Paid       => reserve stock per order lines (idempotent per order)
    /// - Cancelled  => release reserved stock (idempotent per order)
    /// - Shipped    => allocate (finalize) stock per order lines (idempotent per order)
    /// 
    /// This command centralizes imperative consistency while a future event-driven pipeline
    /// can later be replaced by an event-driven pipeline if the platform adopts an outbox/saga boundary.
    /// </summary>
    public sealed class UpdateOrderStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UpdateOrderStatusDto> _validator;
        private readonly OrderStatePolicy _policy = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        // Inventory orchestration dependencies
        private readonly ReserveInventoryHandler _reserveInventory;
        private readonly ReleaseInventoryReservationHandler _releaseReservation;
        private readonly AllocateInventoryForOrderHandler _allocateForOrder;

        public UpdateOrderStatusHandler(
            IAppDbContext db,
            IValidator<UpdateOrderStatusDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            ReserveInventoryHandler reserveInventory,
            ReleaseInventoryReservationHandler releaseReservation,
            AllocateInventoryForOrderHandler allocateForOrder)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _reserveInventory = reserveInventory ?? throw new ArgumentNullException(nameof(reserveInventory));
            _releaseReservation = releaseReservation ?? throw new ArgumentNullException(nameof(releaseReservation));
            _allocateForOrder = allocateForOrder ?? throw new ArgumentNullException(nameof(allocateForOrder));
        }

        /// <summary>
        /// Applies a legal state transition and performs coupled inventory actions.
        /// </summary>
        public async Task HandleAsync(UpdateOrderStatusDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new ValidationException(val.Errors);

            // Load order with lines for inventory orchestration.
            var order = await _db.Set<Order>()
                .Include(o => o.Lines)
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                    .ThenInclude(s => s.Lines)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && !o.IsDeleted, ct);

            if (order is null)
                throw new ValidationException(_localizer["OrderNotFound"]);

            // Concurrency guard (RowVersion lives in BaseEntity).
            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
                throw new ValidationException(_localizer["RowVersionRequired"]);

            var currentRowVersion = order.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                throw new ValidationException(_localizer["ConcurrencyConflictOrderModified"]);

            // Policy check: allowed transitions.
            if (!_policy.IsAllowed(order.Status, dto.NewStatus))
                throw new ValidationException(_localizer["OrderTransitionNotAllowed", order.Status, dto.NewStatus]);

            await ValidateTargetStatusEvidenceAsync(order, dto.NewStatus, ct).ConfigureAwait(false);

            if (dto.WarehouseId.HasValue)
            {
                foreach (var line in order.Lines.Where(l => !l.IsDeleted && !l.WarehouseId.HasValue))
                {
                    line.WarehouseId = dto.WarehouseId.Value;
                }
            }

            // Execute inventory side-effects depending on the target status.
            switch (dto.NewStatus)
            {
                case OrderStatus.Paid:
                    {
                        // Idempotency: skip if a reservation transaction already exists for this order.
                        bool alreadyReserved = await _db.Set<InventoryTransaction>().AsNoTracking()
                            .AnyAsync(t => t.ReferenceId == order.Id && t.Reason == "OrderPaid-Reserve", ct);

                        if (!alreadyReserved)
                        {
                            foreach (var line in order.Lines.Where(l => !l.IsDeleted))
                            {
                                var reserveDto = new InventoryReserveDto
                                {
                                    WarehouseId = line.WarehouseId ?? dto.WarehouseId,
                                    VariantId = line.VariantId,
                                    Quantity = line.Quantity,
                                    Reason = "OrderPaid-Reserve",
                                    ReferenceId = order.Id
                                };
                                await _reserveInventory.HandleAsync(reserveDto, ct);
                            }
                        }
                        break;
                    }

                case OrderStatus.Cancelled:
                    {
                        // Idempotency: skip if a release was already recorded (helps with retries).
                        bool alreadyReleased = await _db.Set<InventoryTransaction>().AsNoTracking()
                            .AnyAsync(t => t.ReferenceId == order.Id && t.Reason == "OrderCancelled-Release", ct);

                        if (!alreadyReleased)
                        {
                            foreach (var line in order.Lines.Where(l => !l.IsDeleted))
                            {
                                var releaseDto = new InventoryReleaseReservationDto
                                {
                                    WarehouseId = line.WarehouseId ?? dto.WarehouseId,
                                    VariantId = line.VariantId,
                                    Quantity = line.Quantity,
                                    Reason = "OrderCancelled-Release",
                                    ReferenceId = order.Id
                                };
                                await _releaseReservation.HandleAsync(releaseDto, ct);
                            }
                        }
                        break;
                    }

                case OrderStatus.Shipped:
                    {
                        // Allocation is aggregated at order-level; handler is expected to be idempotent by ReferenceId.
                        var allocDto = new InventoryAllocateForOrderDto
                        {
                            OrderId = order.Id,
                            Lines = order.Lines
                                .Where(l => !l.IsDeleted)
                                .Select(l => new InventoryAllocateForOrderLineDto
                                {
                                    WarehouseId = l.WarehouseId ?? dto.WarehouseId,
                                    VariantId = l.VariantId,
                                    Quantity = l.Quantity
                                })
                                .ToList()
                        };
                        await _allocateForOrder.HandleAsync(allocDto, ct);
                        break;
                    }
            }

            order.Status = dto.NewStatus;
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictOrderModified"]);
            }
        }

        private async Task ValidateTargetStatusEvidenceAsync(Order order, OrderStatus targetStatus, CancellationToken ct)
        {
            switch (targetStatus)
            {
                case OrderStatus.Paid:
                    if (GetCollectedPaymentTotal(order) < order.GrandTotalGrossMinor)
                    {
                        throw new ValidationException(_localizer["OrderPaidStatusRequiresCapturedPayment"]);
                    }
                    break;

                case OrderStatus.PartiallyShipped:
                    if (!HasPartialFulfillmentEvidence(order))
                    {
                        throw new ValidationException(_localizer["OrderPartiallyShippedStatusRequiresPartialShipment"]);
                    }
                    break;

                case OrderStatus.Shipped:
                    if (!HasFullFulfillmentEvidence(order, includeDelivered: true))
                    {
                        throw new ValidationException(_localizer["OrderShippedStatusRequiresFullShipment"]);
                    }
                    break;

                case OrderStatus.Delivered:
                    if (!HasFullDeliveryEvidence(order))
                    {
                        throw new ValidationException(_localizer["OrderDeliveredStatusRequiresFullDelivery"]);
                    }
                    break;

                case OrderStatus.PartiallyRefunded:
                case OrderStatus.Refunded:
                    await ValidateRefundEvidenceAsync(order, targetStatus, ct).ConfigureAwait(false);
                    break;

                case OrderStatus.Completed:
                    if (!HasFullDeliveryEvidence(order))
                    {
                        throw new ValidationException(_localizer["OrderCompletedStatusRequiresFullDelivery"]);
                    }
                    if (await HasOpenRefundsAsync(order.Id, ct).ConfigureAwait(false))
                    {
                        throw new ValidationException(_localizer["OrderCompletedStatusRequiresNoOpenRefunds"]);
                    }
                    break;
            }
        }

        private static long GetCollectedPaymentTotal(Order order)
        {
            return order.Payments
                .Where(x => !x.IsDeleted && x.Currency == order.Currency && x.Status is PaymentStatus.Captured or PaymentStatus.Completed)
                .Sum(x => x.AmountMinor);
        }

        private static bool HasPartialFulfillmentEvidence(Order order)
        {
            var orderedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.Quantity);
            var shippedQuantity = GetFulfilledQuantity(order, includeDelivered: true);
            return orderedQuantity > 0 && shippedQuantity > 0 && shippedQuantity < orderedQuantity;
        }

        private static bool HasFullFulfillmentEvidence(Order order, bool includeDelivered)
        {
            var shippedByLine = GetFulfilledQuantitiesByLine(order, includeDelivered);
            return order.Lines
                .Where(x => !x.IsDeleted)
                .All(line => shippedByLine.TryGetValue(line.Id, out var quantity) && quantity >= line.Quantity);
        }

        private static bool HasFullDeliveryEvidence(Order order)
        {
            var deliveredByLine = order.Shipments
                .Where(x => !x.IsDeleted && x.Status == ShipmentStatus.Delivered)
                .SelectMany(x => x.Lines.Where(l => !l.IsDeleted))
                .GroupBy(x => x.OrderLineId)
                .ToDictionary(x => x.Key, x => x.Sum(l => l.Quantity));

            return order.Lines
                .Where(x => !x.IsDeleted)
                .All(line => deliveredByLine.TryGetValue(line.Id, out var quantity) && quantity >= line.Quantity);
        }

        private static int GetFulfilledQuantity(Order order, bool includeDelivered)
        {
            return GetFulfilledQuantitiesByLine(order, includeDelivered).Values.Sum();
        }

        private static Dictionary<Guid, int> GetFulfilledQuantitiesByLine(Order order, bool includeDelivered)
        {
            var validStatuses = includeDelivered
                ? new[] { ShipmentStatus.Shipped, ShipmentStatus.Delivered, ShipmentStatus.Returned }
                : new[] { ShipmentStatus.Shipped, ShipmentStatus.Returned };

            return order.Shipments
                .Where(x => !x.IsDeleted && validStatuses.Contains(x.Status))
                .SelectMany(x => x.Lines.Where(l => !l.IsDeleted))
                .GroupBy(x => x.OrderLineId)
                .ToDictionary(x => x.Key, x => x.Sum(l => l.Quantity));
        }

        private async Task ValidateRefundEvidenceAsync(Order order, OrderStatus targetStatus, CancellationToken ct)
        {
            var completedRefundTotal = await _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.OrderId == order.Id && x.Status == RefundStatus.Completed && !x.IsDeleted)
                .SumAsync(x => (long?)x.AmountMinor, ct)
                .ConfigureAwait(false) ?? 0L;

            if (targetStatus == OrderStatus.PartiallyRefunded &&
                (completedRefundTotal <= 0 || completedRefundTotal >= order.GrandTotalGrossMinor))
            {
                throw new ValidationException(_localizer["OrderPartiallyRefundedStatusRequiresPartialRefund"]);
            }

            if (targetStatus == OrderStatus.Refunded && completedRefundTotal < order.GrandTotalGrossMinor)
            {
                throw new ValidationException(_localizer["OrderRefundedStatusRequiresFullRefund"]);
            }
        }

        private async Task<bool> HasOpenRefundsAsync(Guid orderId, CancellationToken ct)
        {
            return await _db.Set<Refund>()
                .AsNoTracking()
                .AnyAsync(x => x.OrderId == orderId && x.Status == RefundStatus.Pending && !x.IsDeleted, ct)
                .ConfigureAwait(false);
        }
    }
}
