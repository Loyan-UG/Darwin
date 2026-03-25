using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetInvoicesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetInvoicesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<InvoiceListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Invoice>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Currency.Contains(q) ||
                    (x.CustomerId.HasValue && x.CustomerId.Value.ToString().Contains(q)) ||
                    (x.OrderId.HasValue && x.OrderId.Value.ToString().Contains(q)) ||
                    (x.PaymentId.HasValue && x.PaymentId.Value.ToString().Contains(q)));
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InvoiceListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    CustomerId = x.CustomerId,
                    OrderId = x.OrderId,
                    PaymentId = x.PaymentId,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    DueDateUtc = x.DueDateUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await EnrichInvoicesAsync(items, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichInvoicesAsync(List<InvoiceListItemDto> items, CancellationToken ct)
        {
            if (items.Count == 0)
            {
                return;
            }

            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();
            var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var paymentIds = items.Where(x => x.PaymentId.HasValue).Select(x => x.PaymentId!.Value).Distinct().ToList();

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>().AsNoTracking().Where(x => customerIds.Contains(x.Id)).ToListAsync(ct).ConfigureAwait(false);

            var userIds = customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var users = userIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>().AsNoTracking().Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);

            var orders = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>().AsNoTracking().Where(x => orderIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct).ConfigureAwait(false);

            var payments = paymentIds.Count == 0
                ? new Dictionary<Guid, Payment>()
                : await _db.Set<Payment>().AsNoTracking().Where(x => paymentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);
            foreach (var item in items)
            {
                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = Darwin.Application.Billing.Queries.BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, users);
                }

                if (item.OrderId.HasValue && orders.TryGetValue(item.OrderId.Value, out var orderNumber))
                {
                    item.OrderNumber = orderNumber;
                }

                if (item.PaymentId.HasValue && payments.TryGetValue(item.PaymentId.Value, out var payment))
                {
                    item.PaymentSummary = BuildPaymentSummary(payment);
                }
            }
        }

        private static string BuildPaymentSummary(Payment payment)
        {
            return $"{payment.Provider} | {payment.Currency} {(payment.AmountMinor / 100.0M):0.00} | {payment.Status}";
        }
    }

    public sealed class GetInvoiceForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetInvoiceForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<InvoiceEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                return null;
            }

            string customerDisplayName = string.Empty;
            if (invoice.CustomerId.HasValue)
            {
                var customer = await _db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.CustomerId.Value, ct).ConfigureAwait(false);
                if (customer is not null)
                {
                    User? linkedUser = null;
                    if (customer.UserId.HasValue)
                    {
                        linkedUser = await _db.Set<User>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.UserId.Value, ct).ConfigureAwait(false);
                    }

                    customerDisplayName = Darwin.Application.Billing.Queries.BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, linkedUser);
                }
            }

            var orderNumber = invoice.OrderId.HasValue
                ? await _db.Set<Order>().AsNoTracking().Where(x => x.Id == invoice.OrderId.Value).Select(x => x.OrderNumber).FirstOrDefaultAsync(ct).ConfigureAwait(false)
                : null;

            var paymentSummary = string.Empty;
            if (invoice.PaymentId.HasValue)
            {
                var payment = await _db.Set<Payment>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.PaymentId.Value, ct).ConfigureAwait(false);
                if (payment is not null)
                {
                    paymentSummary = $"{payment.Provider} | {payment.Currency} {(payment.AmountMinor / 100.0M):0.00} | {payment.Status}";
                }
            }

            return new InvoiceEditDto
            {
                Id = invoice.Id,
                RowVersion = invoice.RowVersion,
                BusinessId = invoice.BusinessId,
                CustomerId = invoice.CustomerId,
                CustomerDisplayName = customerDisplayName,
                OrderId = invoice.OrderId,
                OrderNumber = orderNumber,
                PaymentId = invoice.PaymentId,
                PaymentSummary = paymentSummary,
                Status = invoice.Status,
                Currency = invoice.Currency,
                TotalNetMinor = invoice.TotalNetMinor,
                TotalTaxMinor = invoice.TotalTaxMinor,
                TotalGrossMinor = invoice.TotalGrossMinor,
                DueDateUtc = invoice.DueDateUtc,
                PaidAtUtc = invoice.PaidAtUtc
            };
        }
    }
}
