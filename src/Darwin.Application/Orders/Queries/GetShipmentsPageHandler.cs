using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
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

    public GetShipmentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

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

        var nowUtc = DateTime.UtcNow;
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
            var term = query.Trim().ToLowerInvariant();
            shipments = shipments.Where(s =>
                s.Carrier.ToLower().Contains(term) ||
                (s.Service != null && s.Service.ToLower().Contains(term)) ||
                (s.TrackingNumber != null && s.TrackingNumber.ToLower().Contains(term)) ||
                _db.Set<Order>().Any(o => o.Id == s.OrderId && !o.IsDeleted && o.OrderNumber.ToLower().Contains(term)));
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
                    .Where(o => o.Id == s.OrderId && !o.IsDeleted)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefault() ?? string.Empty,
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
                DefaultRefundPaymentId = _db.Set<Payment>()
                    .Where(p => p.OrderId == s.OrderId &&
                        !p.IsDeleted &&
                        (p.Status == PaymentStatus.Captured ||
                         p.Status == PaymentStatus.Completed ||
                         p.Status == PaymentStatus.Refunded))
                    .OrderByDescending(p => p.PaidAtUtc ?? DateTime.MinValue)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefault(),
                NeedsCarrierReview =
                    s.Carrier == "DHL" &&
                    ((s.Service == null || s.Service == string.Empty) ||
                     s.Status == Domain.Enums.ShipmentStatus.Returned ||
                     ((s.Status == Domain.Enums.ShipmentStatus.Shipped || s.Status == Domain.Enums.ShipmentStatus.Delivered) &&
                      (s.TrackingNumber == null || s.TrackingNumber == string.Empty))),
                ProviderOperationQueued =
                    _db.Set<ShipmentProviderOperation>().Any(op =>
                        op.ShipmentId == s.Id &&
                        !op.IsDeleted &&
                        op.Provider == "DHL" &&
                        op.Status == "Pending"),
                ProviderOperationFailed =
                    _db.Set<ShipmentProviderOperation>().Any(op =>
                        op.ShipmentId == s.Id &&
                        !op.IsDeleted &&
                        op.Provider == "DHL" &&
                        op.Status == "Failed"),
                ProviderOperationType = _db.Set<ShipmentProviderOperation>()
                    .Where(op => op.ShipmentId == s.Id &&
                                 !op.IsDeleted &&
                                 op.Provider == "DHL" &&
                                 (op.Status == "Pending" || op.Status == "Failed"))
                    .OrderByDescending(op => op.LastAttemptAtUtc ?? op.CreatedAtUtc)
                    .Select(op => op.OperationType)
                    .FirstOrDefault(),
                ProviderOperationFailureReason = _db.Set<ShipmentProviderOperation>()
                    .Where(op => op.ShipmentId == s.Id &&
                                 !op.IsDeleted &&
                                 op.Provider == "DHL" &&
                                 op.Status == "Failed")
                    .OrderByDescending(op => op.LastAttemptAtUtc ?? op.CreatedAtUtc)
                    .Select(op => op.FailureReason)
                    .FirstOrDefault(),
                RowVersion = s.RowVersion
            })
            .ToListAsync(ct);

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

    public GetShipmentOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

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
        var shipments = _db.Set<Shipment>().AsNoTracking().Where(s => !s.IsDeleted);

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
                ct).ConfigureAwait(false),
            ReturnFollowUpCount = await shipments.CountAsync(
                s => s.Status == Domain.Enums.ShipmentStatus.Returned,
                ct).ConfigureAwait(false)
        };
    }
}
