using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Identity;
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

            var paymentIds = items.Select(x => x.PaymentId).Distinct().ToList();
            if (paymentIds.Count > 0)
            {
                var payments = await _db.Set<Payment>()
                    .AsNoTracking()
                    .Where(x => paymentIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

                foreach (var item in items)
                {
                    if (payments.TryGetValue(item.PaymentId, out var payment))
                    {
                        item.PaymentProvider = payment.Provider;
                        item.PaymentProviderReference = payment.ProviderTransactionRef;
                        item.PaymentStatus = payment.Status;
                    }
                }
            }

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
                    CustomerId = x.CustomerId,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    Status = x.Status,
                    IssuedAtUtc = x.CreatedAtUtc,
                    DueAtUtc = x.DueDateUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var paymentIds = items.Where(x => x.PaymentId.HasValue).Select(x => x.PaymentId!.Value).Distinct().ToList();
            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();

            var payments = paymentIds.Count == 0
                ? new Dictionary<Guid, Payment>()
                : await _db.Set<Payment>()
                    .AsNoTracking()
                    .Where(x => paymentIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => customerIds.Contains(x.Id))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var customerUserIds = customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var users = customerUserIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>()
                    .AsNoTracking()
                    .Where(x => customerUserIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                if (item.PaymentId.HasValue && payments.TryGetValue(item.PaymentId.Value, out var payment))
                {
                    item.PaymentProvider = payment.Provider;
                    item.PaymentProviderReference = payment.ProviderTransactionRef;
                    item.PaymentStatus = payment.Status;
                }

                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = Darwin.Application.Billing.Queries.BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, users);
                }
            }

            return (items, total);
        }
    }
}
