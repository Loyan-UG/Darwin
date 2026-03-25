using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
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

            return (items, total);
        }
    }

    public sealed class GetPaymentForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<PaymentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Payment>()
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
                .FirstOrDefaultAsync(ct);
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
}
