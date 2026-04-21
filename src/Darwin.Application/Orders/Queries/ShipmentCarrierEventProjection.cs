using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries;

internal static class ShipmentCarrierEventProjection
{
    public static async Task PopulateRecentEventsAsync(
        IAppDbContext db,
        IEnumerable<ShipmentListItemDto> items,
        int maxEventsPerShipment,
        CancellationToken ct = default)
    {
        var shipmentIds = items.Select(x => x.Id).Distinct().ToList();
        if (shipmentIds.Count == 0 || maxEventsPerShipment < 1)
        {
            return;
        }

        var events = await db.Set<ShipmentCarrierEvent>()
            .AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.ShipmentId,
                Item = new ShipmentCarrierEventListItemDto
                {
                    CarrierEventKey = x.CarrierEventKey,
                    ProviderStatus = x.ProviderStatus,
                    ExceptionCode = x.ExceptionCode,
                    ExceptionMessage = x.ExceptionMessage,
                    TrackingNumber = x.TrackingNumber,
                    LabelUrl = x.LabelUrl,
                    Service = x.Service,
                    OccurredAtUtc = x.OccurredAtUtc
                }
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lookup = events
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.Item).Take(maxEventsPerShipment).ToList());

        foreach (var item in items)
        {
            item.RecentCarrierEvents = lookup.TryGetValue(item.Id, out var history)
                ? history
                : new List<ShipmentCarrierEventListItemDto>();
        }
    }
}
