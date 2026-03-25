using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns paged refunds belonging to a specific order.
    /// </summary>
    public sealed class GetOrderRefundsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetOrderRefundsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<RefundListItemDto> Items, int Total)> HandleAsync(Guid orderId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.OrderId == orderId);

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RefundListItemDto
                {
                    Id = x.Id,
                    OrderId = x.OrderId ?? Guid.Empty,
                    PaymentId = x.PaymentId,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    /// <summary>
    /// Returns paged order invoice snapshots.
    /// </summary>
    public sealed class GetOrderInvoicesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetOrderInvoicesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<OrderInvoiceListItemDto> Items, int Total)> HandleAsync(Guid orderId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Invoice>()
                .AsNoTracking()
                .Where(x => x.OrderId == orderId);

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new OrderInvoiceListItemDto
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    PaymentId = x.PaymentId,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    Status = x.Status,
                    IssuedAtUtc = x.CreatedAtUtc,
                    DueAtUtc = x.DueDateUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
