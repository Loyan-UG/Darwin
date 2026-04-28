using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    public sealed class GetShipmentProviderOperationsPageHandler
    {
        private static readonly TimeSpan StalePendingThreshold = TimeSpan.FromMinutes(30);
        private readonly IAppDbContext _db;

        public GetShipmentProviderOperationsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<ShipmentProviderOperationListItemDto> Items, int Total, ShipmentProviderOperationSummaryDto Summary, List<string> Providers, List<string> OperationTypes)> HandleAsync(
            int page,
            int pageSize,
            ShipmentProviderOperationFilterDto filter,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);
            filter ??= new ShipmentProviderOperationFilterDto();

            var now = DateTime.UtcNow;
            var staleBeforeUtc = now.Subtract(StalePendingThreshold);
            var baseQuery = _db.Set<ShipmentProviderOperation>().AsNoTracking();
            var activeQuery = baseQuery.Where(x => !x.IsDeleted);

            var summary = new ShipmentProviderOperationSummaryDto
            {
                TotalCount = await activeQuery.CountAsync(ct).ConfigureAwait(false),
                PendingCount = await activeQuery.CountAsync(x => x.Status == "Pending", ct).ConfigureAwait(false),
                FailedCount = await activeQuery.CountAsync(x => x.Status == "Failed", ct).ConfigureAwait(false),
                ProcessedCount = await activeQuery.CountAsync(x => x.Status == "Processed", ct).ConfigureAwait(false),
                StalePendingCount = await activeQuery.CountAsync(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc, ct).ConfigureAwait(false),
                CancelledCount = await baseQuery.CountAsync(x => x.IsDeleted, ct).ConfigureAwait(false)
            };

            var providers = await activeQuery
                .Select(x => x.Provider)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var operationTypes = await activeQuery
                .Select(x => x.OperationType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var query = activeQuery;

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.Provider.ToLower().Contains(q) ||
                    x.OperationType.ToLower().Contains(q) ||
                    (x.FailureReason != null && x.FailureReason.ToLower().Contains(q)) ||
                    _db.Set<Shipment>().Any(s => s.Id == x.ShipmentId &&
                        !s.IsDeleted &&
                        ((s.TrackingNumber != null && s.TrackingNumber.ToLower().Contains(q)) ||
                         (s.ProviderShipmentReference != null && s.ProviderShipmentReference.ToLower().Contains(q)) ||
                         _db.Set<Order>().Any(o => o.Id == s.OrderId && !o.IsDeleted && o.OrderNumber.ToLower().Contains(q)))));
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                var provider = filter.Provider.Trim();
                query = query.Where(x => x.Provider == provider);
            }

            if (!string.IsNullOrWhiteSpace(filter.OperationType))
            {
                var operationType = filter.OperationType.Trim();
                query = query.Where(x => x.OperationType == operationType);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var status = filter.Status.Trim();
                query = query.Where(x => x.Status == status);
            }

            if (filter.FailedOnly)
            {
                query = query.Where(x => x.Status == "Failed");
            }

            if (filter.StalePendingOnly)
            {
                query = query.Where(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc);
            }

            var total = await query.CountAsync(ct).ConfigureAwait(false);
            var operations = await query
                .OrderByDescending(x => x.Status == "Pending")
                .ThenByDescending(x => x.Status == "Failed")
                .ThenBy(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var shipmentIds = operations.Select(x => x.ShipmentId).Distinct().ToList();
            var shipments = shipmentIds.Count == 0
                ? new List<Shipment>()
                : await _db.Set<Shipment>()
                    .AsNoTracking()
                    .Where(x => shipmentIds.Contains(x.Id) && !x.IsDeleted)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
            var shipmentMap = shipments.ToDictionary(x => x.Id);
            var orderIds = shipments.Select(x => x.OrderId).Distinct().ToList();
            var orderNumbers = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            var items = operations.Select(x =>
            {
                shipmentMap.TryGetValue(x.ShipmentId, out var shipment);
                var orderNumber = shipment is not null && orderNumbers.TryGetValue(shipment.OrderId, out var value)
                    ? value
                    : string.Empty;

                return new ShipmentProviderOperationListItemDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    ShipmentId = x.ShipmentId,
                    OrderId = shipment?.OrderId ?? Guid.Empty,
                    OrderNumber = orderNumber,
                    Provider = x.Provider,
                    OperationType = x.OperationType,
                    Status = x.Status,
                    AttemptCount = x.AttemptCount,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    ProcessedAtUtc = x.ProcessedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    AgeMinutes = Math.Max(0, (int)(now - x.CreatedAtUtc).TotalMinutes),
                    IsStalePending = x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc,
                    FailureReason = x.FailureReason,
                    TrackingNumber = shipment?.TrackingNumber,
                    LabelUrl = shipment?.LabelUrl
                };
            }).ToList();

            return (items, total, summary, providers, operationTypes);
        }
    }
}
