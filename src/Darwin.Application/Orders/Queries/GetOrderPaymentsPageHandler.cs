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
    /// Returns paged payments of an order for admin listing screens.
    /// </summary>
    public sealed class GetOrderPaymentsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetOrderPaymentsPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Executes a paged query over payments of a given order.
        /// </summary>
        public async Task<(List<PaymentListItemDto> Items, int Total)> HandleAsync(Guid orderId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Payment>().AsNoTracking().Where(p => p.OrderId == orderId);
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PaymentListItemDto
                {
                    Id = p.Id,
                    Provider = p.Provider,
                    ProviderReference = p.ProviderReference,
                    AmountMinor = p.AmountMinor,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAtUtc = p.CreatedAtUtc,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
