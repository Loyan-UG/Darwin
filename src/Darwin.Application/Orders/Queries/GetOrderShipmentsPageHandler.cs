using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns paged shipments of an order for admin listing screens.
    /// </summary>
    public sealed class GetOrderShipmentsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetOrderShipmentsPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Executes a paged query over shipments of a given order.
        /// </summary>
        public async Task<(List<ShipmentListItemDto> Items, int Total)> HandleAsync(
            Guid orderId,
            int page,
            int pageSize,
            ShipmentQueueFilter filter = ShipmentQueueFilter.All,
            int attentionDelayHours = 24,
            int trackingGraceHours = 12,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (attentionDelayHours < 1) attentionDelayHours = 24;
            if (trackingGraceHours < 1) trackingGraceHours = 12;

            var nowUtc = DateTime.UtcNow;
            var handoffThresholdUtc = nowUtc.AddHours(-attentionDelayHours);
            var trackingThresholdUtc = nowUtc.AddHours(-trackingGraceHours);

            var baseQuery = _db.Set<Shipment>().AsNoTracking().Where(s => s.OrderId == orderId);
            baseQuery = filter switch
            {
                ShipmentQueueFilter.Pending => baseQuery.Where(s => s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed),
                ShipmentQueueFilter.Shipped => baseQuery.Where(s => s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered),
                ShipmentQueueFilter.MissingTracking => baseQuery.Where(s =>
                    (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                    (s.TrackingNumber == null || s.TrackingNumber == string.Empty)),
                ShipmentQueueFilter.Returned => baseQuery.Where(s => s.Status == Domain.Enums.ShipmentStatus.Returned),
                ShipmentQueueFilter.Dhl => baseQuery.Where(s => s.Carrier == "DHL"),
                ShipmentQueueFilter.MissingService => baseQuery.Where(s => s.Service == null || s.Service == string.Empty),
                ShipmentQueueFilter.AwaitingHandoff => baseQuery.Where(s =>
                    (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                    s.CreatedAtUtc <= handoffThresholdUtc),
                ShipmentQueueFilter.TrackingOverdue => baseQuery.Where(s =>
                    s.Carrier == "DHL" &&
                    (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                    (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                    ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc)),
                _ => baseQuery
            };
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
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
                    IsDhl = s.Carrier == "DHL",
                    AwaitingHandoff =
                        (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                        s.CreatedAtUtc <= handoffThresholdUtc,
                    TrackingOverdue =
                        s.Carrier == "DHL" &&
                        (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                        (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                        ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc),
                    AttentionDelayHours = attentionDelayHours,
                    TrackingGraceHours = trackingGraceHours,
                    NeedsCarrierReview =
                        s.Carrier == "DHL" &&
                        ((s.Service == null || s.Service == string.Empty) ||
                         ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                          (s.TrackingNumber == null || s.TrackingNumber == string.Empty))),
                    RowVersion = s.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
