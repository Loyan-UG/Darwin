using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
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
    private const int MaxPageSize = 200;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public GetShipmentsPageHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

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
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        if (attentionDelayHours < 1) attentionDelayHours = 24;
        if (trackingGraceHours < 1) trackingGraceHours = 12;

        var nowUtc = _clock.UtcNow;
        var handoffThresholdUtc = nowUtc.AddHours(-attentionDelayHours);
        var trackingThresholdUtc = nowUtc.AddHours(-trackingGraceHours);

        var shipments = _db.Set<Shipment>().AsNoTracking().Where(s => !s.IsDeleted);

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
            ShipmentQueueFilter.ReturnFollowUp => shipments.Where(s =>
                s.Status == Domain.Enums.ShipmentStatus.Returned),
            _ => shipments
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.Contains(query);
            shipments = shipments.Where(s =>
                EF.Functions.Like(s.Carrier, term, QueryLikePattern.EscapeCharacter) ||
                (s.Service != null && EF.Functions.Like(s.Service, term, QueryLikePattern.EscapeCharacter)) ||
                (s.TrackingNumber != null && EF.Functions.Like(s.TrackingNumber, term, QueryLikePattern.EscapeCharacter)) ||
                _db.Set<Order>().Any(o => o.Id == s.OrderId && !o.IsDeleted && EF.Functions.Like(o.OrderNumber, term, QueryLikePattern.EscapeCharacter)));
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
                OrderNumber = string.Empty,
                Carrier = s.Carrier,
                Service = s.Service,
                ProviderShipmentReference = s.ProviderShipmentReference,
                TrackingNumber = s.TrackingNumber,
                TrackingUrl = ShipmentTrackingPresentation.ResolveTrackingUrl(s.Carrier, s.TrackingNumber),
                LabelUrl = s.LabelUrl,
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
                LastCarrierEventKey = s.LastCarrierEventKey,
                AttentionDelayHours = attentionDelayHours,
                TrackingGraceHours = trackingGraceHours,
                DefaultRefundPaymentId = null,
                NeedsCarrierReview =
                    s.Carrier == "DHL" &&
                    ((s.Service == null || s.Service == string.Empty) ||
                     s.Status == Domain.Enums.ShipmentStatus.Returned ||
                     ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                      (s.TrackingNumber == null || s.TrackingNumber == string.Empty))),
                RowVersion = s.RowVersion
            })
            .ToListAsync(ct);

        await ShipmentListItemEnrichment.PopulateOrderPaymentStateAsync(_db, items, ct).ConfigureAwait(false);
        await ShipmentListItemEnrichment.PopulateProviderOperationStateAsync(_db, items, ct).ConfigureAwait(false);
        await ShipmentCarrierEventProjection.PopulateRecentEventsAsync(_db, items, 3, ct).ConfigureAwait(false);

        foreach (var item in items)
        {
            item.LastCarrierEventAtUtc = item.RecentCarrierEvents.FirstOrDefault()?.OccurredAtUtc ?? item.LastCarrierEventAtUtc;
        }

        ShipmentTrackingPresentation.Enrich(items, nowUtc);

        return (items, total);
    }
}

public sealed class GetShipmentOpsSummaryHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public GetShipmentOpsSummaryHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ShipmentOpsSummaryDto> HandleAsync(
        int attentionDelayHours = 24,
        int trackingGraceHours = 12,
        CancellationToken ct = default)
    {
        if (attentionDelayHours < 1) attentionDelayHours = 24;
        if (trackingGraceHours < 1) trackingGraceHours = 12;

        var nowUtc = _clock.UtcNow;
        var handoffThresholdUtc = nowUtc.AddHours(-attentionDelayHours);
        var trackingThresholdUtc = nowUtc.AddHours(-trackingGraceHours);
        var shipments = _db.Set<Shipment>().AsNoTracking().Where(s => !s.IsDeleted);

        return await shipments
            .GroupBy(_ => 1)
            .Select(g => new ShipmentOpsSummaryDto
            {
                PendingCount = g.Count(s => s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed),
                ShippedCount = g.Count(s => s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered),
                MissingTrackingCount = g.Count(s =>
                    (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                    (s.TrackingNumber == null || s.TrackingNumber == string.Empty)),
                ReturnedCount = g.Count(s => s.Status == Domain.Enums.ShipmentStatus.Returned),
                DhlCount = g.Count(s => s.Carrier == "DHL"),
                MissingServiceCount = g.Count(s => s.Service == null || s.Service == string.Empty),
                AwaitingHandoffCount = g.Count(s =>
                    (s.Status == Domain.Enums.ShipmentStatus.Pending || s.Status == Domain.Enums.ShipmentStatus.Packed) &&
                    s.CreatedAtUtc <= handoffThresholdUtc),
                TrackingOverdueCount = g.Count(s =>
                    s.Carrier == "DHL" &&
                    (s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                    (s.TrackingNumber == null || s.TrackingNumber == string.Empty) &&
                    ((s.ShippedAtUtc ?? s.CreatedAtUtc) <= trackingThresholdUtc)),
                CarrierReviewCount = g.Count(s =>
                    s.Carrier == "DHL" &&
                    ((s.Service == null || s.Service == string.Empty) ||
                     s.Status == Domain.Enums.ShipmentStatus.Returned ||
                     ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                      (s.TrackingNumber == null || s.TrackingNumber == string.Empty)))),
                ReturnFollowUpCount = g.Count(s => s.Status == Domain.Enums.ShipmentStatus.Returned)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? new ShipmentOpsSummaryDto();
    }
}
