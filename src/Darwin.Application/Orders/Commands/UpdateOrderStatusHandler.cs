using System;
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
    /// may take over cross-aggregate side-effects (see TODO below).
    /// </summary>
    public sealed class UpdateOrderStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly UpdateOrderStatusValidator _validator = new();
        private readonly OrderStatePolicy _policy = new();

        // Inventory orchestration dependencies
        private readonly ReserveInventoryHandler _reserveInventory;
        private readonly ReleaseInventoryReservationHandler _releaseReservation;
        private readonly AllocateInventoryForOrderHandler _allocateForOrder;

        public UpdateOrderStatusHandler(
            IAppDbContext db,
            ReserveInventoryHandler reserveInventory,
            ReleaseInventoryReservationHandler releaseReservation,
            AllocateInventoryForOrderHandler allocateForOrder)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
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
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);

            if (order is null)
                throw new ValidationException("Order not found.");

            // Concurrency guard (RowVersion lives in BaseEntity).
            if (!order.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                throw new ValidationException("Concurrency conflict. The order was modified by another process.");

            // Policy check: allowed transitions.
            if (!_policy.IsAllowed(order.Status, dto.NewStatus))
                throw new ValidationException($"Transition {order.Status} → {dto.NewStatus} is not allowed.");

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
                                    VariantId = l.VariantId,
                                    Quantity = l.Quantity
                                })
                                .ToList()
                        };
                        await _allocateForOrder.HandleAsync(allocDto, ct);
                        break;
                    }
                    // TODO : Other statuses (Delivered/Refunded/Partially...) may have separate policies later.
            }

            order.Status = dto.NewStatus;
            await _db.SaveChangesAsync(ct);

            // TODO (Option B — Event-driven alternative):
            // In the future, raise domain/application events instead of imperative calls:
            //  - OnOrderPaid(orderId)       => InventoryService reserves per line (idempotent by (orderId, reason))
            //  - OnOrderCancelled(orderId)  => InventoryService releases reservations
            //  - OnOrderShipped(orderId)    => InventoryService allocates final stock
            //
            // Pros:
            //  * Decouples Orders from Inventory orchestration.
            //  * Retries/outbox patterns improve reliability across service boundaries.
            // Cons:
            //  * More moving parts (event bus/outbox), higher operational complexity.
            //  * Harder to maintain strong consistency without sagas/compensation.
        }
    }
}
