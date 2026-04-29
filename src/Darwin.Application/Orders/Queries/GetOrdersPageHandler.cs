using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns a paged list of orders for Admin.
    /// </summary>
    public sealed class GetOrdersPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetOrdersPageHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<(List<OrderListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, query: null, filter: OrderQueueFilter.All, ct).ConfigureAwait(false);
        }

        public async Task<(List<OrderListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, string? query, OrderQueueFilter filter = OrderQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            query = string.IsNullOrWhiteSpace(query) ? null : QueryLikePattern.Contains(query);

            var baseQuery = _db.Set<Order>()
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Where(o =>
                    query == null ||
                    EF.Functions.Like(o.OrderNumber, query, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(o.Currency, query, QueryLikePattern.EscapeCharacter));

            baseQuery = filter switch
            {
                OrderQueueFilter.Open => baseQuery.Where(o =>
                    o.Status != Domain.Enums.OrderStatus.Cancelled &&
                    o.Status != Domain.Enums.OrderStatus.Refunded &&
                    o.Status != Domain.Enums.OrderStatus.Completed),
                OrderQueueFilter.PaymentIssues => baseQuery.Where(o =>
                    o.Status != Domain.Enums.OrderStatus.Cancelled &&
                    o.Payments.Any(p => !p.IsDeleted && p.Status == Domain.Enums.PaymentStatus.Failed)),
                OrderQueueFilter.FulfillmentAttention => baseQuery.Where(o =>
                    o.Status == Domain.Enums.OrderStatus.Paid ||
                    o.Status == Domain.Enums.OrderStatus.PartiallyShipped),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(o => o.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new OrderListItemDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Currency = o.Currency,
                    GrandTotalGrossMinor = o.GrandTotalGrossMinor,
                    Status = o.Status,
                    PaymentCount = o.Payments.Count(p => !p.IsDeleted),
                    FailedPaymentCount = o.Payments.Count(p => !p.IsDeleted && p.Status == Domain.Enums.PaymentStatus.Failed),
                    ShipmentCount = o.Shipments.Count(s => !s.IsDeleted),
                    CreatedAtUtc = o.CreatedAtUtc,
                    RowVersion = o.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
