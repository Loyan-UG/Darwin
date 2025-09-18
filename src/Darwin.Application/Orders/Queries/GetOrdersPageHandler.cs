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
    /// Returns a paged list of orders for Admin.
    /// </summary>
    public sealed class GetOrdersPageHandler
    {
        private readonly IAppDbContext _db;
        public GetOrdersPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<OrderListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Order>().AsNoTracking();
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
                    CreatedAtUtc = o.CreatedAtUtc
                }).ToListAsync(ct);

            return (items, total);
        }
    }
}
