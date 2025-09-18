using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.StateMachine;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Transitions an order to a new status after checking policy and guard conditions (payments/shipments).
    /// </summary>
    public sealed class UpdateOrderStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly OrderStatePolicy _policy = new();

        public UpdateOrderStatusHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid orderId, OrderStatus target, CancellationToken ct = default)
        {
            var order = await _db.Set<Order>()
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, ct)
                ?? throw new InvalidOperationException("Order not found.");

            var current = order.Status;
            if (current == target) return;

            if (!_policy.IsAllowed(current, target))
                throw new InvalidOperationException($"Transition {current} → {target} is not allowed.");

            // Guards (phase 1 minimal):
            switch (target)
            {
                case OrderStatus.Paid:
                    var hasCapture = order.Payments.Exists(p => p.Status == PaymentStatus.Captured);
                    if (!hasCapture) throw new InvalidOperationException("Cannot mark Paid without a captured payment.");
                    break;

                case OrderStatus.Shipped:
                    var hasShipment = order.Shipments.Exists(s => s.TrackingNumber != null);
                    if (!hasShipment) throw new InvalidOperationException("Cannot mark Shipped without a shipment.");
                    break;

                case OrderStatus.Delivered:
                    var hasShipped = order.Shipments.Exists(s => s.ShippedAtUtc != null);
                    if (!hasShipped) throw new InvalidOperationException("Cannot mark Delivered before Shipped.");
                    break;

                case OrderStatus.Refunded:
                    var hasAnyPayment = order.Payments.Count > 0;
                    if (!hasAnyPayment) throw new InvalidOperationException("Cannot refund without a payment.");
                    break;
            }

            order.Status = target;
            await _db.SaveChangesAsync(ct);
        }
    }
}
