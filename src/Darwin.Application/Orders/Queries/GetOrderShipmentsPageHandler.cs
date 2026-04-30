using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns paged shipments of an order for admin listing screens.
    /// </summary>
    public sealed class GetOrderShipmentsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        public GetOrderShipmentsPageHandler(IAppDbContext db, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        }

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
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            if (attentionDelayHours < 1) attentionDelayHours = 24;
            if (trackingGraceHours < 1) trackingGraceHours = 12;

            var nowUtc = _clock.UtcNow;
            var handoffThresholdUtc = nowUtc.AddHours(-attentionDelayHours);
            var trackingThresholdUtc = nowUtc.AddHours(-trackingGraceHours);

            var baseQuery = _db.Set<Shipment>().AsNoTracking().Where(s => s.OrderId == orderId && !s.IsDeleted);
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

            var orderContext = await _db.Set<Order>()
                .AsNoTracking()
                .Where(o => o.Id == orderId && !o.IsDeleted)
                .Select(o => new
                {
                    o.OrderNumber,
                    DefaultRefundPaymentId = _db.Set<Payment>()
                        .Where(p => p.OrderId == o.Id &&
                            !p.IsDeleted &&
                            (p.Status == PaymentStatus.Captured ||
                             p.Status == PaymentStatus.Completed ||
                             p.Status == PaymentStatus.Refunded))
                        .OrderByDescending(p => p.PaidAtUtc ?? DateTime.MinValue)
                        .Select(p => (Guid?)p.Id)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
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

            foreach (var item in items)
            {
                item.OrderNumber = orderContext?.OrderNumber ?? string.Empty;
                item.DefaultRefundPaymentId = orderContext?.DefaultRefundPaymentId;
            }

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
}
