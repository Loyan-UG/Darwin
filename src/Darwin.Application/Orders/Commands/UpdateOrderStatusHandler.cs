// File: src/Darwin.Application/Orders/Commands/UpdateOrderStatusHandler.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.StateMachine;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Updates an order status using the central state policy and applies basic guards.
    /// Inventory side-effects are coordinated here to ensure consistency (see TODO notes).
    /// </summary>
    public sealed class UpdateOrderStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly OrderStatePolicy _policy = new();

        public UpdateOrderStatusHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Updates <see cref="Order.Status"/> when the transition is allowed.
        /// Performs optimistic concurrency check using <paramref name="dto.RowVersion"/>.
        /// </summary>
        /// <exception cref="DbUpdateConcurrencyException">When row version mismatch is detected.</exception>
        /// <exception cref="ValidationException">When transition is not allowed or guards fail.</exception>
        public async Task HandleAsync(UpdateOrderStatusDto dto, CancellationToken ct = default)
        {
            var order = await _db.Set<Order>()
                .Include(o => o.Lines)
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && !o.IsDeleted, ct);

            if (order is null) throw new InvalidOperationException("Order not found.");

            if (!order.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            var from = order.Status;
            var to = dto.NewStatus;

            if (!_policy.IsAllowed(from, to))
                throw new ValidationException($"Transition {from} → {to} is not allowed.");

            // Basic guards (phase 1):
            // - Cannot cancel after items fully shipped.
            if (to == OrderStatus.Cancelled &&
                (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered))
            {
                throw new ValidationException("Cannot cancel an order that has already shipped.");
            }

            // - To move to Shipped/Delivered, require at least one shipment, etc. (simplified in phase 1)
            if ((to == OrderStatus.Shipped || to == OrderStatus.Delivered) && order.Shipments.Count == 0)
                throw new ValidationException("No shipments found for the order.");

            // Apply transition
            order.Status = to;

            // Inventory side-effects (centralized here for clarity):
            // NOTE: We intentionally do not directly mutate stock counters here in phase 1.
            // Instead, we emit inventory transactions via dedicated handlers or inline creation (simple approach).
            
            // TODO: Reserve on Paid; Release on Cancelled; Finalize allocation on Shipped.
            // This will integrate with InventoryTransaction and, if applicable, variant-level counters.
            // See InventoryTransaction configuration for fields available. 
            // (VariantId, QuantityDelta, Reason, ReferenceId). 
            // Reason examples: "OrderPaid-Reserve", "OrderCancelled-Release", "OrderShipped-Finalize".
            // Infra mapping reference: InventoryTransactionConfiguration. 
            // These calls can be implemented here or delegated to small dedicated handlers.
            //
            // Example (pseudo-implementation for future step):
            // await _inventorySync.ApplyAsync(order, from, to, ct);
            // For now we keep it as a TODO to land incrementally with tested logic.

            await _db.SaveChangesAsync(ct);
        }
    }
}
