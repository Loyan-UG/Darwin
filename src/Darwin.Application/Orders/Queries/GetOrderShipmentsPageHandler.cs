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
        public async Task<(List<ShipmentListItemDto> Items, int Total)> HandleAsync(Guid orderId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Shipment>().AsNoTracking().Where(s => s.OrderId == orderId);
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new ShipmentListItemDto
                {
                    Id = s.Id,
                    Carrier = s.Carrier,
                    Service = s.Service,
                    CreatedAtUtc = s.CreatedAtUtc,
                    RowVersion = s.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
