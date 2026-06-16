using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetBankReconciliationPageHandler
{
    private readonly IAppDbContext _db;

    public GetBankReconciliationPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankReconciliationPageDto> HandleAsync(Guid? businessId = null, Guid? bankAccountId = null, string? query = null, BankReconciliationMatchStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new BankReconciliationPageDto
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

        dto.BankAccountOptions = await BankReconciliationQuerySupport.GetBankAccountOptionsAsync(_db, context.BusinessId.Value, ct).ConfigureAwait(false);

        var matches = _db.Set<BankReconciliationMatch>().AsNoTracking().Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.DraftCount = await matches.CountAsync(x => x.Status == BankReconciliationMatchStatus.Draft, ct).ConfigureAwait(false);
        dto.MatchedCount = await matches.CountAsync(x => x.Status == BankReconciliationMatchStatus.Matched, ct).ConfigureAwait(false);
        dto.CancelledCount = await matches.CountAsync(x => x.Status == BankReconciliationMatchStatus.Cancelled, ct).ConfigureAwait(false);

        if (dto.BankAccountId.HasValue) matches = matches.Where(x => x.BankAccountId == dto.BankAccountId.Value);
        if (status.HasValue) matches = matches.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            matches = matches.Where(x =>
                (x.MatchNumber != null && x.MatchNumber.Contains(normalizedQuery)) ||
                (x.ReviewNotes != null && x.ReviewNotes.Contains(normalizedQuery)));
        }

        dto.Total = await matches.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await matches
            .OrderByDescending(x => x.MatchDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BankReconciliationListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                BankAccountId = x.BankAccountId,
                BankAccountLabel = _db.Set<BankAccount>().Where(a => a.Id == x.BankAccountId).Select(a => a.Code + " - " + a.DisplayName).FirstOrDefault() ?? string.Empty,
                MatchNumber = x.MatchNumber ?? string.Empty,
                Status = x.Status,
                MatchDateUtc = x.MatchDateUtc,
                Currency = x.Currency,
                BankTotalMinor = x.BankTotalMinor,
                FinanceTotalMinor = x.FinanceTotalMinor,
                DifferenceMinor = x.DifferenceMinor,
                LineCount = x.Lines.Count(l => !l.IsDeleted),
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetBankReconciliationDetailHandler
{
    private readonly IAppDbContext _db;

    public GetBankReconciliationDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankReconciliationEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var match = await _db.Set<BankReconciliationMatch>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (match is null) return null;

        var dto = BankReconciliationQuerySupport.Map(match);
        await BankReconciliationQuerySupport.FillDisplayAsync(_db, dto, ct).ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetBankReconciliationDraftHandler
{
    private readonly IAppDbContext _db;

    public GetBankReconciliationDraftHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BankReconciliationEditDto> HandleAsync(Guid? businessId = null, Guid? bankAccountId = null, CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new BankReconciliationEditDto
        {
            BusinessId = context.BusinessId ?? Guid.Empty,
            BankAccountId = bankAccountId is { } id && id != Guid.Empty ? id : Guid.Empty,
            MatchDateUtc = DateTime.UtcNow.Date,
            Currency = "EUR",
            MetadataJson = "{}"
        };
        if (dto.BusinessId != Guid.Empty)
        {
            dto.BankAccountOptions = await BankReconciliationQuerySupport.GetBankAccountOptionsAsync(_db, dto.BusinessId, ct).ConfigureAwait(false);
            if (dto.BankAccountId == Guid.Empty && dto.BankAccountOptions.Count > 0)
            {
                dto.BankAccountId = dto.BankAccountOptions[0].Id;
            }

            if (dto.BankAccountId != Guid.Empty)
            {
                var account = await _db.Set<BankAccount>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.BankAccountId && !x.IsDeleted, ct).ConfigureAwait(false);
                if (account is not null) dto.Currency = account.Currency;
            }

            await BankReconciliationQuerySupport.FillCandidatesAsync(_db, dto, ct).ConfigureAwait(false);
        }

        return dto;
    }
}

internal static class BankReconciliationQuerySupport
{
    public static BankReconciliationEditDto Map(BankReconciliationMatch match) => new()
    {
        Id = match.Id,
        RowVersion = match.RowVersion,
        BusinessId = match.BusinessId,
        BankAccountId = match.BankAccountId,
        MatchNumber = match.MatchNumber,
        Status = match.Status,
        MatchDateUtc = match.MatchDateUtc,
        Currency = match.Currency,
        BankTotalMinor = match.BankTotalMinor,
        FinanceTotalMinor = match.FinanceTotalMinor,
        DifferenceMinor = match.DifferenceMinor,
        MatchedAtUtc = match.MatchedAtUtc,
        CancelledAtUtc = match.CancelledAtUtc,
        ReviewNotes = match.ReviewNotes,
        MetadataJson = match.MetadataJson,
        Lines = match.Lines
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new BankReconciliationLineDto
            {
                Id = x.Id,
                BankStatementLineId = x.BankStatementLineId,
                JournalEntryId = x.JournalEntryId,
                SourceType = x.SourceType,
                SourceEntityType = x.SourceEntityType,
                SourceEntityId = x.SourceEntityId,
                Direction = x.Direction,
                AmountMinor = x.AmountMinor,
                Memo = x.Memo,
                SortOrder = x.SortOrder
            })
            .ToList()
    };

    public static async Task FillDisplayAsync(IAppDbContext db, BankReconciliationEditDto dto, CancellationToken ct)
    {
        dto.BankAccountOptions = await GetBankAccountOptionsAsync(db, dto.BusinessId, ct).ConfigureAwait(false);
        await FillCandidatesAsync(db, dto, ct).ConfigureAwait(false);
        var statementIds = dto.Lines.Select(x => x.BankStatementLineId).Where(x => x != Guid.Empty).Distinct().ToList();
        var postingIds = dto.Lines.Select(x => x.JournalEntryId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var statements = await db.Set<BankStatementLine>()
            .AsNoTracking()
            .Where(x => statementIds.Contains(x.Id))
            .Select(x => new { x.Id, x.TransactionDateUtc, x.Direction, x.AmountMinor, x.Currency, x.CounterpartyName, x.RemittanceInformation })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        var postings = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => postingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.EntryDateUtc, x.PostingKind, x.PostingStatus, x.PostingKey, x.SourceDocumentNumber, Debit = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.DebitMinor), Credit = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.CreditMinor) })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        foreach (var line in dto.Lines)
        {
            if (statements.TryGetValue(line.BankStatementLineId, out var statement))
            {
                line.StatementLineLabel = $"{statement.TransactionDateUtc:yyyy-MM-dd} {statement.Direction} {statement.AmountMinor} {statement.Currency} {statement.CounterpartyName ?? statement.RemittanceInformation ?? string.Empty}".Trim();
            }
            if (line.JournalEntryId.HasValue && postings.TryGetValue(line.JournalEntryId.Value, out var posting))
            {
                line.PostingLabel = $"{posting.EntryDateUtc:yyyy-MM-dd} {posting.PostingKind} {posting.PostingStatus} {posting.PostingKey} {posting.SourceDocumentNumber} {Math.Max(posting.Debit, posting.Credit)}".Trim();
            }
        }
    }

    public static async Task FillCandidatesAsync(IAppDbContext db, BankReconciliationEditDto dto, CancellationToken ct)
    {
        dto.StatementLineCandidates = await GetStatementLineCandidatesAsync(db, dto.BusinessId, dto.BankAccountId, dto.Currency, dto.Id, ct).ConfigureAwait(false);
        dto.PostingCandidates = await GetPostingCandidatesAsync(db, dto.BusinessId, ct).ConfigureAwait(false);
    }

    public static Task<List<BankAccountOptionDto>> GetBankAccountOptionsAsync(IAppDbContext db, Guid businessId, CancellationToken ct) =>
        db.Set<BankAccount>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && x.Status == BankAccountStatus.Active)
            .OrderBy(x => x.DisplayName)
            .Select(x => new BankAccountOptionDto { Id = x.Id, Label = x.Code + " - " + x.DisplayName })
            .ToListAsync(ct);

    private static async Task<List<BankReconciliationCandidateStatementLineDto>> GetStatementLineCandidatesAsync(IAppDbContext db, Guid businessId, Guid bankAccountId, string currency, Guid currentMatchId, CancellationToken ct)
    {
        if (businessId == Guid.Empty || bankAccountId == Guid.Empty) return new();
        var used = db.Set<BankReconciliationMatchLine>()
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.BankReconciliationMatchId != currentMatchId)
            .Select(x => x.BankStatementLineId);
        var rows = await db.Set<BankStatementLine>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId &&
                x.BankAccountId == bankAccountId &&
                !x.IsDeleted &&
                x.Currency == currency &&
                !used.Contains(x.Id) &&
                db.Set<BankStatementImport>().Any(i => i.Id == x.BankStatementImportId && !i.IsDeleted && i.Status == BankStatementImportStatus.Imported))
            .OrderByDescending(x => x.TransactionDateUtc)
            .ThenBy(x => x.Id)
            .Take(200)
            .Select(x => new { x.Id, x.TransactionDateUtc, x.Direction, x.AmountMinor, x.Currency, x.CounterpartyName, x.RemittanceInformation })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(x => new BankReconciliationCandidateStatementLineDto
        {
            Id = x.Id,
            TransactionDateUtc = x.TransactionDateUtc,
            Direction = x.Direction,
            AmountMinor = x.AmountMinor,
            Currency = x.Currency,
            Label = $"{x.TransactionDateUtc:yyyy-MM-dd} {x.Direction} {x.AmountMinor} {x.Currency} {x.CounterpartyName ?? x.RemittanceInformation ?? string.Empty}".Trim()
        }).ToList();
    }

    private static async Task<List<BankReconciliationCandidatePostingDto>> GetPostingCandidatesAsync(IAppDbContext db, Guid businessId, CancellationToken ct)
    {
        if (businessId == Guid.Empty) return new List<BankReconciliationCandidatePostingDto>();
        var rows = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && (x.PostingStatus == JournalEntryPostingStatus.Posted || x.PostingStatus == JournalEntryPostingStatus.Reversed))
            .OrderByDescending(x => x.EntryDateUtc)
            .ThenBy(x => x.Id)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.EntryDateUtc,
                x.PostingKind,
                x.PostingStatus,
                PostingKey = x.PostingKey ?? string.Empty,
                SourceEntityType = x.SourceEntityType ?? string.Empty,
                x.SourceEntityId,
                SourceDocumentNumber = x.SourceDocumentNumber ?? string.Empty,
                DebitMinor = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.DebitMinor),
                CreditMinor = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.CreditMinor)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(x => new BankReconciliationCandidatePostingDto
        {
            Id = x.Id,
            EntryDateUtc = x.EntryDateUtc,
            PostingKind = x.PostingKind,
            PostingStatus = x.PostingStatus,
            PostingKey = x.PostingKey,
            SourceEntityType = x.SourceEntityType,
            SourceEntityId = x.SourceEntityId,
            SourceDocumentNumber = x.SourceDocumentNumber,
            DebitMinor = x.DebitMinor,
            CreditMinor = x.CreditMinor,
            Label = $"{x.EntryDateUtc:yyyy-MM-dd} {x.PostingKind} {x.PostingKey} {x.SourceDocumentNumber}".Trim()
        }).ToList();
    }
}
