using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries;

internal static class ShipmentListItemEnrichment
{
    public static async Task PopulateOrderPaymentStateAsync(
        IAppDbContext db,
        List<ShipmentListItemDto> items,
        CancellationToken ct)
    {
        var orderIds = items.Select(x => x.OrderId).Distinct().ToList();
        if (orderIds.Count == 0)
        {
            return;
        }

        var orderNumbers = await db.Set<Order>()
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id) && !o.IsDeleted)
            .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, ct)
            .ConfigureAwait(false);

        var refundPayments = await db.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.OrderId.HasValue &&
                        orderIds.Contains(p.OrderId.Value) &&
                        !p.IsDeleted &&
                        (p.Status == PaymentStatus.Captured ||
                         p.Status == PaymentStatus.Completed ||
                         p.Status == PaymentStatus.Refunded))
            .Select(p => new
            {
                OrderId = p.OrderId!.Value,
                p.Id,
                PaidAtUtc = p.PaidAtUtc ?? DateTime.MinValue
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var defaultRefundPaymentByOrder = refundPayments
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                x => x.Key,
                x => (Guid?)x.OrderByDescending(p => p.PaidAtUtc).Select(p => p.Id).First());

        foreach (var item in items)
        {
            item.OrderNumber = orderNumbers.TryGetValue(item.OrderId, out var orderNumber)
                ? orderNumber
                : string.Empty;
            item.DefaultRefundPaymentId = defaultRefundPaymentByOrder.TryGetValue(item.OrderId, out var paymentId)
                ? paymentId
                : null;
        }
    }

    public static async Task PopulateProviderOperationStateAsync(
        IAppDbContext db,
        List<ShipmentListItemDto> items,
        CancellationToken ct)
    {
        var shipmentIds = items.Select(x => x.Id).Distinct().ToList();
        if (shipmentIds.Count == 0)
        {
            return;
        }

        var operations = await db.Set<ShipmentProviderOperation>()
            .AsNoTracking()
            .Where(op => shipmentIds.Contains(op.ShipmentId) &&
                         !op.IsDeleted &&
                         op.Provider == "DHL" &&
                         (op.Status == "Pending" || op.Status == "Failed"))
            .Select(op => new
            {
                op.ShipmentId,
                op.Status,
                op.OperationType,
                op.FailureReason,
                SortAtUtc = op.LastAttemptAtUtc ?? op.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byShipment = operations
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var item in items)
        {
            if (!byShipment.TryGetValue(item.Id, out var shipmentOperations))
            {
                continue;
            }

            item.ProviderOperationQueued = shipmentOperations.Any(x => x.Status == "Pending");
            item.ProviderOperationFailed = shipmentOperations.Any(x => x.Status == "Failed");
            item.ProviderOperationType = shipmentOperations
                .OrderByDescending(x => x.SortAtUtc)
                .Select(x => x.OperationType)
                .FirstOrDefault();
            item.ProviderOperationFailureReason = shipmentOperations
                .Where(x => x.Status == "Failed")
                .OrderByDescending(x => x.SortAtUtc)
                .Select(x => x.FailureReason)
                .FirstOrDefault();
        }
    }
}
