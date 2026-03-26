using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries
{
    public sealed class GetPaymentsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<PaymentListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId!.Value,
                    OrderId = x.OrderId,
                    InvoiceId = x.InvoiceId,
                    CustomerId = x.CustomerId,
                    UserId = x.UserId,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    Provider = x.Provider,
                    ProviderTransactionRef = x.ProviderTransactionRef,
                    PaidAtUtc = x.PaidAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await EnrichPaymentsAsync(items, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichPaymentsAsync(List<PaymentListItemDto> items, CancellationToken ct)
        {
            if (items.Count == 0)
            {
                return;
            }

            var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var invoiceIds = items.Where(x => x.InvoiceId.HasValue).Select(x => x.InvoiceId!.Value).Distinct().ToList();
            var paymentUserIds = items.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();

            var orderMap = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            var invoiceMap = invoiceIds.Count == 0
                ? new Dictionary<Guid, Invoice>()
                : await _db.Set<Invoice>()
                    .AsNoTracking()
                    .Where(x => invoiceIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);
            var paymentIds = items.Select(x => x.Id).ToList();
            var refundTotals = paymentIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);

            foreach (var customerId in invoiceMap.Values.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value))
            {
                if (!customerIds.Contains(customerId))
                {
                    customerIds.Add(customerId);
                }
            }

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => customerIds.Contains(x.Id))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var identityUserIds = paymentUserIds.ToHashSet();
            foreach (var linkedUserId in customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value))
            {
                identityUserIds.Add(linkedUserId);
            }

            var userMap = identityUserIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>()
                    .AsNoTracking()
                    .Where(x => identityUserIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                if (item.OrderId.HasValue && orderMap.TryGetValue(item.OrderId.Value, out var orderNumber))
                {
                    item.OrderNumber = orderNumber;
                }

                if (item.InvoiceId.HasValue && invoiceMap.TryGetValue(item.InvoiceId.Value, out var invoice))
                {
                    item.InvoiceStatus = invoice.Status;
                    item.InvoiceDueAtUtc = invoice.DueDateUtc;
                    item.InvoiceTotalGrossMinor = invoice.TotalGrossMinor;

                    if (!item.CustomerId.HasValue && invoice.CustomerId.HasValue)
                    {
                        item.CustomerId = invoice.CustomerId;
                    }
                }

                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, userMap);
                    item.CustomerEmail = BillingPaymentDisplayFormatter.ResolveCustomerEmail(customer, userMap);
                }

                if (item.UserId.HasValue && userMap.TryGetValue(item.UserId.Value, out var user))
                {
                    item.UserDisplayName = BillingPaymentDisplayFormatter.BuildUserDisplayName(user);
                    item.UserEmail = user.Email;
                }

                item.RefundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                    item.AmountMinor,
                    refundTotals.TryGetValue(item.Id, out var refundedAmountMinor) ? refundedAmountMinor : 0L);
                item.NetCapturedAmountMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(item.AmountMinor, item.RefundedAmountMinor);
            }
        }
    }

    public sealed class GetPaymentForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<PaymentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return HandleInternalAsync(id, ct);
        }

        private async Task<PaymentEditDto?> HandleInternalAsync(Guid id, CancellationToken ct)
        {
            var dto = await _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new PaymentEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId!.Value,
                    OrderId = x.OrderId,
                    InvoiceId = x.InvoiceId,
                    CustomerId = x.CustomerId,
                    UserId = x.UserId,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    Provider = x.Provider,
                    ProviderTransactionRef = x.ProviderTransactionRef,
                    PaidAtUtc = x.PaidAtUtc
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (dto is null)
            {
                return null;
            }

            if (dto.OrderId.HasValue)
            {
                dto.OrderNumber = await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => x.Id == dto.OrderId.Value)
                    .Select(x => x.OrderNumber)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }

            if (dto.InvoiceId.HasValue)
            {
                var invoice = await _db.Set<Invoice>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.InvoiceId.Value, ct)
                    .ConfigureAwait(false);

                if (invoice is not null)
                {
                    dto.InvoiceStatus = invoice.Status;
                    dto.InvoiceDueAtUtc = invoice.DueDateUtc;
                    dto.InvoiceTotalGrossMinor = invoice.TotalGrossMinor;

                    if (!dto.CustomerId.HasValue && invoice.CustomerId.HasValue)
                    {
                        dto.CustomerId = invoice.CustomerId;
                    }
                }
            }

            dto.RefundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                dto.AmountMinor,
                await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.PaymentId == dto.Id && x.Status == RefundStatus.Completed)
                    .SumAsync(x => (long?)x.AmountMinor, ct)
                    .ConfigureAwait(false) ?? 0L);
            dto.NetCapturedAmountMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(dto.AmountMinor, dto.RefundedAmountMinor);

            User? paymentUser = null;
            if (dto.UserId.HasValue)
            {
                paymentUser = await _db.Set<User>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.UserId.Value, ct)
                    .ConfigureAwait(false);

                if (paymentUser is not null)
                {
                    dto.UserDisplayName = BillingPaymentDisplayFormatter.BuildUserDisplayName(paymentUser);
                    dto.UserEmail = paymentUser.Email;
                }
            }

            if (dto.CustomerId.HasValue)
            {
                var customer = await _db.Set<Customer>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.CustomerId.Value, ct)
                    .ConfigureAwait(false);

                if (customer is not null)
                {
                    User? linkedUser = null;
                    if (customer.UserId.HasValue)
                    {
                        linkedUser = paymentUser is not null && paymentUser.Id == customer.UserId.Value
                            ? paymentUser
                            : await _db.Set<User>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == customer.UserId.Value, ct)
                                .ConfigureAwait(false);
                    }

                    dto.CustomerDisplayName = BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, linkedUser);
                    dto.CustomerEmail = BillingPaymentDisplayFormatter.ResolveCustomerEmail(customer, linkedUser);
                }
            }

            return dto;
        }
    }

    public sealed class GetFinancialAccountsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetFinancialAccountsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<FinancialAccountListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FinancialAccountListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Type = x.Type,
                    Code = x.Code,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetFinancialAccountForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetFinancialAccountForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<FinancialAccountEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new FinancialAccountEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Type = x.Type,
                    Code = x.Code
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetExpensesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetExpensesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<ExpenseListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Set<Expense>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(x => x.ExpenseDateUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ExpenseListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    SupplierId = x.SupplierId,
                    Category = x.Category,
                    Description = x.Description,
                    AmountMinor = x.AmountMinor,
                    ExpenseDateUtc = x.ExpenseDateUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetExpenseForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetExpenseForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<ExpenseEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Expense>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ExpenseEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    SupplierId = x.SupplierId,
                    Category = x.Category,
                    Description = x.Description,
                    AmountMinor = x.AmountMinor,
                    ExpenseDateUtc = x.ExpenseDateUtc
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetJournalEntriesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetJournalEntriesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<JournalEntryListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Set<JournalEntry>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(x => x.EntryDateUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new JournalEntryListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    EntryDateUtc = x.EntryDateUtc,
                    Description = x.Description,
                    LineCount = x.Lines.Count,
                    TotalDebitMinor = x.Lines.Sum(l => l.DebitMinor),
                    TotalCreditMinor = x.Lines.Sum(l => l.CreditMinor),
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetJournalEntryForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetJournalEntryForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<JournalEntryEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entry = await _db.Set<JournalEntry>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (entry is null)
            {
                return null;
            }

            return new JournalEntryEditDto
            {
                Id = entry.Id,
                RowVersion = entry.RowVersion,
                BusinessId = entry.BusinessId,
                EntryDateUtc = entry.EntryDateUtc,
                Description = entry.Description,
                Lines = entry.Lines
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new JournalEntryLineDto
                    {
                        Id = x.Id,
                        AccountId = x.AccountId,
                        DebitMinor = x.DebitMinor,
                        CreditMinor = x.CreditMinor,
                        Memo = x.Memo
                    })
                    .ToList()
            };
        }
    }

    internal static class BillingPaymentDisplayFormatter
    {
        public static string BuildCustomerDisplayName(Customer customer, IReadOnlyDictionary<Guid, User> users)
        {
            if (customer.UserId.HasValue && users.TryGetValue(customer.UserId.Value, out var linkedUser))
            {
                return BuildUserDisplayName(linkedUser);
            }

            return BuildFallbackDisplayName(customer.FirstName, customer.LastName, customer.Email);
        }

        public static string BuildCustomerDisplayName(Customer customer, User? linkedUser)
        {
            if (linkedUser is not null)
            {
                return BuildUserDisplayName(linkedUser);
            }

            return BuildFallbackDisplayName(customer.FirstName, customer.LastName, customer.Email);
        }

        public static string? ResolveCustomerEmail(Customer customer, IReadOnlyDictionary<Guid, User> users)
        {
            if (customer.UserId.HasValue && users.TryGetValue(customer.UserId.Value, out var linkedUser))
            {
                return linkedUser.Email;
            }

            return string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email;
        }

        public static string? ResolveCustomerEmail(Customer customer, User? linkedUser)
        {
            if (linkedUser is not null)
            {
                return linkedUser.Email;
            }

            return string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email;
        }

        public static string BuildUserDisplayName(User user)
        {
            return BuildFallbackDisplayName(user.FirstName, user.LastName, user.Email);
        }

        private static string BuildFallbackDisplayName(string? firstName, string? lastName, string emailFallback)
        {
            var fullName = $"{firstName ?? string.Empty} {lastName ?? string.Empty}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? emailFallback : fullName;
        }
    }
}
