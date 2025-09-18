using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Orders.StateMachine
{
    /// <summary>
    /// Allowed transitions between OrderStatus values.
    /// Guards (payments/shipments/refunds) are applied by the command handler.
    /// </summary>
    public sealed class OrderStatePolicy
    {
        private readonly Dictionary<OrderStatus, HashSet<OrderStatus>> _allowed = new()
        {
            [OrderStatus.Created] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new() { OrderStatus.PartiallyShipped, OrderStatus.Shipped, OrderStatus.Refunded, OrderStatus.PartiallyRefunded },
            [OrderStatus.PartiallyShipped] = new() { OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.PartiallyRefunded },
            [OrderStatus.Shipped] = new() { OrderStatus.Delivered, OrderStatus.PartiallyRefunded, OrderStatus.Refunded },
            [OrderStatus.Delivered] = new() { OrderStatus.PartiallyRefunded, OrderStatus.Refunded },
            [OrderStatus.PartiallyRefunded] = new() { OrderStatus.Refunded },
            [OrderStatus.Cancelled] = new(),
            [OrderStatus.Refunded] = new()
        };

        public bool IsAllowed(OrderStatus from, OrderStatus to)
            => _allowed.TryGetValue(from, out var set) && set.Contains(to);
    }
}
