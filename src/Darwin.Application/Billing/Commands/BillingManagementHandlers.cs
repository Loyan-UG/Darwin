using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Commands
{
    public sealed class CreatePaymentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PaymentCreateDto> _validator;

        public CreatePaymentHandler(IAppDbContext db, IValidator<PaymentCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(PaymentCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var payment = new Payment
            {
                BusinessId = dto.BusinessId,
                OrderId = dto.OrderId,
                InvoiceId = dto.InvoiceId,
                CustomerId = dto.CustomerId,
                UserId = dto.UserId,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency.Trim().ToUpperInvariant(),
                Status = dto.Status,
                Provider = dto.Provider.Trim(),
                ProviderTransactionRef = NormalizeOptional(dto.ProviderTransactionRef),
                PaidAtUtc = dto.PaidAtUtc
            };

            _db.Set<Payment>().Add(payment);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return payment.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdatePaymentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PaymentEditDto> _validator;

        public UpdatePaymentHandler(IAppDbContext db, IValidator<PaymentEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(PaymentEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (payment is null)
            {
                throw new InvalidOperationException("Payment not found.");
            }

            if (!payment.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            payment.BusinessId = dto.BusinessId;
            payment.OrderId = dto.OrderId;
            payment.InvoiceId = dto.InvoiceId;
            payment.CustomerId = dto.CustomerId;
            payment.UserId = dto.UserId;
            payment.AmountMinor = dto.AmountMinor;
            payment.Currency = dto.Currency.Trim().ToUpperInvariant();
            payment.Status = dto.Status;
            payment.Provider = dto.Provider.Trim();
            payment.ProviderTransactionRef = NormalizeOptional(dto.ProviderTransactionRef);
            payment.PaidAtUtc = dto.PaidAtUtc;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class CreateFinancialAccountHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<FinancialAccountCreateDto> _validator;

        public CreateFinancialAccountHandler(IAppDbContext db, IValidator<FinancialAccountCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(FinancialAccountCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var account = new FinancialAccount
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Code = NormalizeOptional(dto.Code)
            };

            _db.Set<FinancialAccount>().Add(account);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return account.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdateFinancialAccountHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<FinancialAccountEditDto> _validator;

        public UpdateFinancialAccountHandler(IAppDbContext db, IValidator<FinancialAccountEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(FinancialAccountEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var account = await _db.Set<FinancialAccount>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                throw new InvalidOperationException("Financial account not found.");
            }

            if (!account.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            account.BusinessId = dto.BusinessId;
            account.Name = dto.Name.Trim();
            account.Type = dto.Type;
            account.Code = NormalizeOptional(dto.Code);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class CreateExpenseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ExpenseCreateDto> _validator;

        public CreateExpenseHandler(IAppDbContext db, IValidator<ExpenseCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(ExpenseCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var expense = new Expense
            {
                BusinessId = dto.BusinessId,
                SupplierId = dto.SupplierId,
                Category = dto.Category.Trim(),
                Description = dto.Description.Trim(),
                AmountMinor = dto.AmountMinor,
                ExpenseDateUtc = dto.ExpenseDateUtc
            };

            _db.Set<Expense>().Add(expense);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return expense.Id;
        }
    }

    public sealed class UpdateExpenseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ExpenseEditDto> _validator;

        public UpdateExpenseHandler(IAppDbContext db, IValidator<ExpenseEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(ExpenseEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var expense = await _db.Set<Expense>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (expense is null)
            {
                throw new InvalidOperationException("Expense not found.");
            }

            if (!expense.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            expense.BusinessId = dto.BusinessId;
            expense.SupplierId = dto.SupplierId;
            expense.Category = dto.Category.Trim();
            expense.Description = dto.Description.Trim();
            expense.AmountMinor = dto.AmountMinor;
            expense.ExpenseDateUtc = dto.ExpenseDateUtc;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public sealed class CreateJournalEntryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<JournalEntryCreateDto> _validator;

        public CreateJournalEntryHandler(IAppDbContext db, IValidator<JournalEntryCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(JournalEntryCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entry = new JournalEntry
            {
                BusinessId = dto.BusinessId,
                EntryDateUtc = dto.EntryDateUtc,
                Description = dto.Description.Trim(),
                Lines = dto.Lines.Select(x => new JournalEntryLine
                {
                    AccountId = x.AccountId,
                    DebitMinor = x.DebitMinor,
                    CreditMinor = x.CreditMinor,
                    Memo = NormalizeOptional(x.Memo)
                }).ToList()
            };

            _db.Set<JournalEntry>().Add(entry);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return entry.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdateJournalEntryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<JournalEntryEditDto> _validator;

        public UpdateJournalEntryHandler(IAppDbContext db, IValidator<JournalEntryEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(JournalEntryEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entry = await _db.Set<JournalEntry>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (entry is null)
            {
                throw new InvalidOperationException("Journal entry not found.");
            }

            if (!entry.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            entry.BusinessId = dto.BusinessId;
            entry.EntryDateUtc = dto.EntryDateUtc;
            entry.Description = dto.Description.Trim();

            _db.Set<JournalEntryLine>().RemoveRange(entry.Lines);
            entry.Lines = dto.Lines.Select(x => new JournalEntryLine
            {
                AccountId = x.AccountId,
                DebitMinor = x.DebitMinor,
                CreditMinor = x.CreditMinor,
                Memo = NormalizeOptional(x.Memo)
            }).ToList();

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
