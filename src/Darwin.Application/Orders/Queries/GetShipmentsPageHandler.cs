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

        var shipments = _db.Set<Shipment>().AsNoTracking();

        shipments = filter switch
        {
            ShipmentQueueFilter.Pending => shipments.Where(s => s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed),
            ShipmentQueueFilter.Shipped => shipments.Where(s => s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered),
            ShipmentQueueFilter.MissingTracking => shipments.Where(s =>
                (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                (s.TrackingNumber == null || s.TrackingNumber == string.Empty)),
            ShipmentQueueFilter.Returned => shipments.Where(s => s.Status == Domain.Enums.ShipmentStatus.Returned),
            ShipmentQueueFilter.Dhl => shipments.Where(s => s.Carrier == "DHL"),
            ShipmentQueueFilter.MissingService => shipments.Where(s => s.Service == null || s.Service == string.Empty),
            ShipmentQueueFilter.AwaitingHandoff => shipments.Where(s =>
                (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                s.CreatedAtUtc <= handoffThresholdUtc),
            ShipmentQueueFilter.TrackingOverdue => shipments.Where(s =>
                s.Carrier == "DHL" &&
                (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc)),
            ShipmentQueueFilter.CarrierReview => shipments.Where(s =>
                s.Carrier == "DHL" &&
                ((s.Service == null || s.Service == string.Empty) ||
                 s.Status == Domain.Enums.ShipmentStatus.Returned ||
                 ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                  (s.TrackingNumber == null || s.TrackingNumber == string.Empty)))),
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
                IsDhl = s.Carrier == "DHL",
                AwaitingHandoff =
                    (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                    s.CreatedAtUtc <= handoffThresholdUtc,
                TrackingOverdue =
                    s.Carrier == "DHL" &&
                    (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                    (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                    ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc),
                LastCarrierEventAtUtc = s.DeliveredAtUtc ?? s.ShippedAtUtc ?? s.CreatedAtUtc,
                AttentionDelayHours = attentionDelayHours,
                TrackingGraceHours = trackingGraceHours,
                NeedsCarrierReview =
                    s.Carrier == "DHL" &&
                    ((s.Service == null || s.Service == string.Empty) ||
                     s.Status == Domain.Enums.ShipmentStatus.Returned ||
                     ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                      (s.TrackingNumber == null || s.TrackingNumber == string.Empty))),
                RowVersion = s.RowVersion
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.OpenAgeHours = Math.Max(0, (int)Math.Floor((nowUtc - item.CreatedAtUtc).TotalHours));
            if (item.ShippedAtUtc.HasValue && !item.DeliveredAtUtc.HasValue)
            {
                item.InTransitAgeHours = Math.Max(0, (int)Math.Floor((nowUtc - item.ShippedAtUtc.Value).TotalHours));
            }

            item.TrackingState = ResolveTrackingState(item);
            item.ExceptionNote = ResolveExceptionNote(item);
        }

        return (items, total);
    }

    private static string ResolveTrackingState(ShipmentListItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.TrackingNumber))
        {
            return item.Status switch
            {
                Domain.Enums.ShipmentStatus.Delivered => "Tracking linked and delivered",
                Domain.Enums.ShipmentStatus.Shipped => "Tracking linked and in transit",
                _ => "Tracking linked before handoff completion"
            };
        }

        if (item.Status == Domain.Enums.ShipmentStatus.Returned)
        {
            return "Return recorded without active tracking handoff";
        }

        if (item.TrackingOverdue)
        {
            return "Tracking overdue beyond grace window";
        }

        if (item.Status == Domain.Enums.ShipmentStatus.Shipped || item.Status == Domain.Enums.ShipmentStatus.Delivered)
        {
            return "Carrier handoff recorded without tracking";
        }

        return "No carrier tracking linked yet";
    }

    private static string ResolveExceptionNote(ShipmentListItemDto item)
    {
        if (item.Status == Domain.Enums.ShipmentStatus.Returned)
        {
            return "Returned shipment requires carrier or support follow-up.";
        }

        if (string.IsNullOrWhiteSpace(item.Service))
        {
            return "Carrier service is missing.";
        }

        if (item.TrackingOverdue)
        {
            return $"Tracking missing beyond {item.TrackingGraceHours} h grace.";
        }

        if (item.AwaitingHandoff)
        {
            return $"Still open after {item.AttentionDelayHours} h attention threshold.";
        }

        if ((item.Status == Domain.Enums.ShipmentStatus.Shipped || item.Status == Domain.Enums.ShipmentStatus.Delivered) &&
            string.IsNullOrWhiteSpace(item.TrackingNumber))
        {
            return "Shipment is marked handed off but no tracking number is present.";
        }

        return string.Empty;
    }
}

public sealed class GetShipmentOpsSummaryHandler
{
    private readonly IAppDbContext _db;

    public GetShipmentOpsSummaryHandler(IAppDbContext db) => _db = db;

    public async Task<ShipmentOpsSummaryDto> HandleAsync(
        int attentionDelayHours = 24,
        int trackingGraceHours = 12,
        CancellationToken ct = default)
    {
        if (attentionDelayHours < 1) attentionDelayHours = 24;
        if (trackingGraceHours < 1) trackingGraceHours = 12;

        var nowUtc = DateTime.UtcNow;
        var handoffThresholdUtc = nowUtc.AddHours(-attentionDelayHours);
        var trackingThresholdUtc = nowUtc.AddHours(-trackingGraceHours);
        var shipments = _db.Set<Shipment>().AsNoTracking();

        return new ShipmentOpsSummaryDto
        {
            PendingCount = await shipments.CountAsync(
                s => s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed,
                ct).ConfigureAwait(false),
            ShippedCount = await shipments.CountAsync(
                s => s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered,
                ct).ConfigureAwait(false),
            MissingTrackingCount = await shipments.CountAsync(
                s => (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                     (s.TrackingNumber == null || s.TrackingNumber == string.Empty),
                ct).ConfigureAwait(false),
            ReturnedCount = await shipments.CountAsync(s => s.Status == Domain.Enums.ShipmentStatus.Returned, ct).ConfigureAwait(false),
            DhlCount = await shipments.CountAsync(s => s.Carrier == "DHL", ct).ConfigureAwait(false),
            MissingServiceCount = await shipments.CountAsync(s => s.Service == null || s.Service == string.Empty, ct).ConfigureAwait(false),
            AwaitingHandoffCount = await shipments.CountAsync(
                s => (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                     s.CreatedAtUtc <= handoffThresholdUtc,
                ct).ConfigureAwait(false),
            TrackingOverdueCount = await shipments.CountAsync(
                s => s.Carrier == "DHL" &&
                     (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                     (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                     ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc),
                ct).ConfigureAwait(false),
            CarrierReviewCount = await shipments.CountAsync(
                s => s.Carrier == "DHL" &&
                     ((s.Service == null || s.Service == string.Empty) ||
                      s.Status == Domain.Enums.ShipmentStatus.Returned ||
                      ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                       (s.TrackingNumber == null || s.TrackingNumber == string.Empty))),
                ct).ConfigureAwait(false)
        };
    }
}
