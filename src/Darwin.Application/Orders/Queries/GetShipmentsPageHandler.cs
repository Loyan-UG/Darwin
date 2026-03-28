using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Orders.Queries;

public sealed class GetShipmentsPageHandler
{
    private readonly IAppDbContext _db;

    public GetShipmentsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<ShipmentListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        ShipmentQueueFilter filter = ShipmentQueueFilter.All,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var shipments = _db.Set<Shipment>().AsNoTracking();

        shipments = filter switch
        {
            ShipmentQueueFilter.Pending => shipments.Where(s => s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed),
            ShipmentQueueFilter.Shipped => shipments.Where(s => s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered),
            _ => shipments
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            shipments = shipments.Where(s =>
                s.Carrier.Contains(term) ||
                s.Service.Contains(term) ||
                (s.TrackingNumber != null && s.TrackingNumber.Contains(term)) ||
                _db.Set<Order>().Any(o => o.Id == s.OrderId && o.OrderNumber.Contains(term)));
        }

        var total = await shipments.CountAsync(ct);

        var items = await shipments
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShipmentListItemDto
            {
                Id = s.Id,
                OrderId = s.OrderId,
                OrderNumber = _db.Set<Order>()
                    .Where(o => o.Id == s.OrderId)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefault() ?? string.Empty,
                Carrier = s.Carrier,
                Service = s.Service,
                TrackingNumber = s.TrackingNumber,
                TotalWeight = s.TotalWeight ?? 0,
                Status = s.Status,
                ShippedAtUtc = s.ShippedAtUtc,
                DeliveredAtUtc = s.DeliveredAtUtc,
                CreatedAtUtc = s.CreatedAtUtc,
                RowVersion = s.RowVersion
            })
            .ToListAsync(ct);

        return (items, total);
    }
}
