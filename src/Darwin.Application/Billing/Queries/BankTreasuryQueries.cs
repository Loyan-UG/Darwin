using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetBankAccountsPageHandler
{
    private readonly IAppDbContext _db;

    public GetBankAccountsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankAccountsPageDto> HandleAsync(Guid? businessId = null, string? query = null, BankAccountStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new BankAccountsPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            BusinessOptions = context.BusinessOptions,
            Query = normalizedQuery,
            Status = status,
            Page = page,
            PageSize = pageSize
        };
        if (!context.BusinessId.HasValue) return dto;

        var accounts = _db.Set<BankAccount>().AsNoTracking().Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.ActiveCount = await accounts.CountAsync(x => x.Status == BankAccountStatus.Active, ct).ConfigureAwait(false);
        dto.ArchivedCount = await accounts.CountAsync(x => x.Status == BankAccountStatus.Archived, ct).ConfigureAwait(false);

        if (status.HasValue) accounts = accounts.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            accounts = accounts.Where(x =>
                x.Code.Contains(normalizedQuery) ||
                x.DisplayName.Contains(normalizedQuery) ||
                (x.BankName != null && x.BankName.Contains(normalizedQuery)) ||
                (x.MaskedAccountIdentifier != null && x.MaskedAccountIdentifier.Contains(normalizedQuery)));
        }

        dto.Total = await accounts.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await accounts
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BankAccountListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                FinancialAccountId = x.FinancialAccountId,
                FinancialAccountLabel = x.FinancialAccountId.HasValue
                    ? _db.Set<FinancialAccount>().Where(a => a.Id == x.FinancialAccountId.Value).Select(a => (a.Code ?? string.Empty) + " " + a.Name).FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Code = x.Code,
                DisplayName = x.DisplayName,
                BankName = x.BankName ?? string.Empty,
                Currency = x.Currency,
                MaskedAccountIdentifier = x.MaskedAccountIdentifier ?? string.Empty,
                Status = x.Status,
                IsDefault = x.IsDefault,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetBankAccountForEditHandler
{
    private readonly IAppDbContext _db;

    public GetBankAccountForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankAccountEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        return await _db.Set<BankAccount>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new BankAccountEditDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                FinancialAccountId = x.FinancialAccountId,
                Code = x.Code,
                DisplayName = x.DisplayName,
                BankName = x.BankName,
                Currency = x.Currency,
                MaskedAccountIdentifier = x.MaskedAccountIdentifier,
                Status = x.Status,
                IsDefault = x.IsDefault,
                MetadataJson = x.MetadataJson
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

public sealed class GetBankStatementsPageHandler
{
    private readonly IAppDbContext _db;

    public GetBankStatementsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankStatementsPageDto> HandleAsync(Guid? businessId = null, Guid? bankAccountId = null, string? query = null, BankStatementImportStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new BankStatementsPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            BusinessOptions = context.BusinessOptions,
            BankAccountId = bankAccountId is { } id && id != Guid.Empty ? id : null,
            Query = normalizedQuery,
            Status = status,
            Page = page,
            PageSize = pageSize
        };
        if (!context.BusinessId.HasValue) return dto;

        dto.BankAccountOptions = await _db.Set<BankAccount>()
            .AsNoTracking()
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted && x.Status == BankAccountStatus.Active)
            .OrderBy(x => x.DisplayName)
            .Select(x => new BankAccountOptionDto { Id = x.Id, Label = x.Code + " - " + x.DisplayName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var imports = _db.Set<BankStatementImport>().AsNoTracking().Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.ImportedCount = await imports.CountAsync(x => x.Status == BankStatementImportStatus.Imported, ct).ConfigureAwait(false);
        dto.CancelledCount = await imports.CountAsync(x => x.Status == BankStatementImportStatus.Cancelled, ct).ConfigureAwait(false);

        if (dto.BankAccountId.HasValue) imports = imports.Where(x => x.BankAccountId == dto.BankAccountId.Value);
        if (status.HasValue) imports = imports.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery)) imports = imports.Where(x => x.StatementReference.Contains(normalizedQuery));

        dto.Total = await imports.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await imports
            .OrderByDescending(x => x.ImportedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BankStatementImportListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                BankAccountId = x.BankAccountId,
                BankAccountLabel = _db.Set<BankAccount>().Where(a => a.Id == x.BankAccountId).Select(a => a.Code + " - " + a.DisplayName).FirstOrDefault() ?? string.Empty,
                StatementReference = x.StatementReference,
                PeriodStartUtc = x.PeriodStartUtc,
                PeriodEndUtc = x.PeriodEndUtc,
                ImportedAtUtc = x.ImportedAtUtc,
                Status = x.Status,
                LineCount = x.LineCount,
                DebitTotalMinor = x.DebitTotalMinor,
                CreditTotalMinor = x.CreditTotalMinor,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetBankStatementImportDetailHandler
{
    private readonly IAppDbContext _db;

    public GetBankStatementImportDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankStatementImportDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var import = await _db.Set<BankStatementImport>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (import is null) return null;
        var accountLabel = await _db.Set<BankAccount>()
            .AsNoTracking()
            .Where(x => x.Id == import.BankAccountId)
            .Select(x => x.Code + " - " + x.DisplayName)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;
        return new BankStatementImportDetailDto
        {
            Id = import.Id,
            RowVersion = import.RowVersion,
            BusinessId = import.BusinessId,
            BankAccountId = import.BankAccountId,
            BankAccountLabel = accountLabel,
            StatementReference = import.StatementReference,
            PeriodStartUtc = import.PeriodStartUtc,
            PeriodEndUtc = import.PeriodEndUtc,
            ImportedAtUtc = import.ImportedAtUtc,
            Status = import.Status,
            LineCount = import.LineCount,
            DebitTotalMinor = import.DebitTotalMinor,
            CreditTotalMinor = import.CreditTotalMinor,
            MetadataJson = import.MetadataJson,
            Lines = import.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.TransactionDateUtc).ThenBy(x => x.Id).Select(x => new BankStatementLineDto
            {
                Id = x.Id,
                TransactionDateUtc = x.TransactionDateUtc,
                ValueDateUtc = x.ValueDateUtc,
                Direction = x.Direction,
                AmountMinor = x.AmountMinor,
                Currency = x.Currency,
                CounterpartyName = x.CounterpartyName,
                CounterpartyReference = x.CounterpartyReference,
                RemittanceInformation = x.RemittanceInformation,
                NormalizedIdentityKey = x.NormalizedIdentityKey,
                ReviewStatus = x.ReviewStatus,
                MetadataJson = x.MetadataJson
            }).ToList()
        };
    }
}
