using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Commands;

public sealed class CreateBankReconciliationMatchHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateBankReconciliationMatchHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(BankReconciliationCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var account = await BankReconciliationSupport.ValidateHeaderAsync(_db, dto, ct).ConfigureAwait(false);
        var match = new BankReconciliationMatch
        {
            BusinessId = dto.BusinessId,
            BankAccountId = dto.BankAccountId,
            MatchNumber = BankTreasurySupport.Optional(dto.MatchNumber, 128),
            MatchDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.MatchDateUtc == default ? _clock.UtcNow : dto.MatchDateUtc),
            Currency = BankTreasurySupport.NormalizeCurrency(dto.Currency),
            ReviewNotes = BankTreasurySupport.Optional(dto.ReviewNotes, 4000),
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson)
        };
        if (!string.Equals(match.Currency, account.Currency, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("BankReconciliationCurrencyMismatch");
        match.Lines = await BankReconciliationSupport.MapLinesAsync(_db, match, dto.Lines, ct).ConfigureAwait(false);
        await BankReconciliationSupport.RecalculateAsync(_db, match, ct).ConfigureAwait(false);
        _db.Set<BankReconciliationMatch>().Add(match);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankReconciliationSupport.RecordEvidenceAsync(_events, match, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return match.Id;
    }
}

public sealed class UpdateBankReconciliationMatchHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateBankReconciliationMatchHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankReconciliationEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var match = await BankReconciliationSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (match.Status != BankReconciliationMatchStatus.Draft) throw new InvalidOperationException("BankReconciliationNotEditable");
        var account = await BankReconciliationSupport.ValidateHeaderAsync(_db, dto, ct).ConfigureAwait(false);
        match.BusinessId = dto.BusinessId;
        match.BankAccountId = dto.BankAccountId;
        match.MatchNumber = BankTreasurySupport.Optional(dto.MatchNumber, 128);
        match.MatchDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.MatchDateUtc == default ? _clock.UtcNow : dto.MatchDateUtc);
        match.Currency = BankTreasurySupport.NormalizeCurrency(dto.Currency);
        match.ReviewNotes = BankTreasurySupport.Optional(dto.ReviewNotes, 4000);
        match.MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        if (!string.Equals(match.Currency, account.Currency, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("BankReconciliationCurrencyMismatch");
        foreach (var old in match.Lines.Where(x => !x.IsDeleted))
        {
            old.IsActive = false;
            old.IsDeleted = true;
        }
        match.Lines.AddRange(await BankReconciliationSupport.MapLinesAsync(_db, match, dto.Lines, ct).ConfigureAwait(false));
        await BankReconciliationSupport.RecalculateAsync(_db, match, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankReconciliationSupport.RecordEvidenceAsync(_events, match, "updated", AuditTrailAction.Updated, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class MarkBankReconciliationMatchedHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public MarkBankReconciliationMatchedHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankReconciliationLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var match = await BankReconciliationSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (match.Status != BankReconciliationMatchStatus.Draft) throw new InvalidOperationException("BankReconciliationCannotMatch");
        await BankReconciliationSupport.RecalculateAsync(_db, match, ct).ConfigureAwait(false);
        if (match.Lines.Count(x => !x.IsDeleted && x.IsActive) == 0) throw new ArgumentException("BankReconciliationLinesRequired");
        if (match.Lines.Any(x => !x.IsDeleted && x.IsActive && !x.JournalEntryId.HasValue)) throw new ArgumentException("BankReconciliationFinanceFactRequired");
        if (match.DifferenceMinor != 0 && string.IsNullOrWhiteSpace(match.ReviewNotes)) throw new ArgumentException("BankReconciliationDifferenceRequiresReviewNotes");
        match.Status = BankReconciliationMatchStatus.Matched;
        match.MatchedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankReconciliationSupport.RecordEvidenceAsync(_events, match, "matched", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class CancelBankReconciliationMatchHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CancelBankReconciliationMatchHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankReconciliationLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var match = await BankReconciliationSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (match.Status == BankReconciliationMatchStatus.Cancelled) throw new InvalidOperationException("BankReconciliationAlreadyCancelled");
        match.Status = BankReconciliationMatchStatus.Cancelled;
        match.CancelledAtUtc = _clock.UtcNow;
        foreach (var line in match.Lines.Where(x => !x.IsDeleted))
        {
            line.IsActive = false;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankReconciliationSupport.RecordEvidenceAsync(_events, match, "cancelled", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

internal static class BankReconciliationSupport
{
    public static async Task<BankReconciliationMatch> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("BankReconciliationInvalidUpdate");
        var match = await db.Set<BankReconciliationMatch>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (match is null) throw new InvalidOperationException("BankReconciliationNotFound");
        if (!(match.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return match;
    }

    public static async Task<BankAccount> ValidateHeaderAsync(IAppDbContext db, BankReconciliationCreateDto dto, CancellationToken ct)
    {
        if (dto.BusinessId == Guid.Empty || dto.BankAccountId == Guid.Empty) throw new ArgumentException("BankReconciliationInvalidLink");
        _ = BankTreasurySupport.NormalizeCurrency(dto.Currency);
        _ = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        _ = BankTreasurySupport.Optional(dto.MatchNumber, 128);
        _ = BankTreasurySupport.Optional(dto.ReviewNotes, 4000);
        if (dto.MatchDateUtc == default) throw new ArgumentException("BankReconciliationInvalidDate");
        if (dto.Lines.Count == 0) throw new ArgumentException("BankReconciliationLinesRequired");
        var account = await db.Set<BankAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.BankAccountId && x.BusinessId == dto.BusinessId && !x.IsDeleted && x.Status == BankAccountStatus.Active, ct)
            .ConfigureAwait(false);
        return account ?? throw new InvalidOperationException("BankAccountNotFound");
    }

    public static async Task<List<BankReconciliationMatchLine>> MapLinesAsync(IAppDbContext db, BankReconciliationMatch match, IReadOnlyCollection<BankReconciliationLineDto> lines, CancellationToken ct)
    {
        var activeInput = lines
            .Where(x => x.BankStatementLineId != Guid.Empty)
            .Select((x, index) => (Line: x, SortOrder: x.SortOrder > 0 ? x.SortOrder : index + 1))
            .ToList();
        if (activeInput.Count == 0) throw new ArgumentException("BankReconciliationLinesRequired");
        if (activeInput.Select(x => x.Line.BankStatementLineId).Distinct().Count() != activeInput.Count) throw new ArgumentException("BankReconciliationDuplicateStatementLine");

        var statementIds = activeInput.Select(x => x.Line.BankStatementLineId).Distinct().ToList();
        var duplicateActive = await db.Set<BankReconciliationMatchLine>()
            .AsNoTracking()
            .AnyAsync(x => x.IsActive && !x.IsDeleted && x.BankReconciliationMatchId != match.Id && statementIds.Contains(x.BankStatementLineId), ct)
            .ConfigureAwait(false);
        if (duplicateActive) throw new InvalidOperationException("BankReconciliationStatementLineAlreadyMatched");

        var statementLines = await db.Set<BankStatementLine>()
            .AsNoTracking()
            .Where(x => statementIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        var importIds = statementLines.Values.Select(x => x.BankStatementImportId).Distinct().ToList();
        var importedIds = await db.Set<BankStatementImport>()
            .AsNoTracking()
            .Where(x => importIds.Contains(x.Id) && !x.IsDeleted && x.Status == BankStatementImportStatus.Imported)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var postingIds = activeInput.Select(x => x.Line.JournalEntryId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var postings = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => postingIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        var output = new List<BankReconciliationMatchLine>();
        foreach (var input in activeInput)
        {
            if (!statementLines.TryGetValue(input.Line.BankStatementLineId, out var statement)) throw new InvalidOperationException("BankStatementLineNotFound");
            if (statement.BusinessId != match.BusinessId || statement.BankAccountId != match.BankAccountId || !string.Equals(statement.Currency, match.Currency, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("BankReconciliationStatementLineMismatch");
            if (!importedIds.Contains(statement.BankStatementImportId)) throw new InvalidOperationException("BankStatementImportNotImported");
            if (input.Line.AmountMinor <= 0 || input.Line.AmountMinor > statement.AmountMinor) throw new ArgumentException("BankReconciliationInvalidLineAmount");
            var journalEntryId = input.Line.JournalEntryId is { } entryId && entryId != Guid.Empty ? entryId : (Guid?)null;
            JournalEntry? posting = null;
            if (journalEntryId.HasValue)
            {
                if (!postings.TryGetValue(journalEntryId.Value, out posting)) throw new InvalidOperationException("JournalEntryNotFound");
                if (posting.BusinessId != match.BusinessId || posting.PostingStatus is not (JournalEntryPostingStatus.Posted or JournalEntryPostingStatus.Reversed)) throw new InvalidOperationException("BankReconciliationPostingNotEligible");
            }

            _ = SupplierInvoiceSupport.NormalizeMetadata(input.Line.Memo);
            output.Add(new BankReconciliationMatchLine
            {
                BankReconciliationMatchId = match.Id,
                BankStatementLineId = statement.Id,
                JournalEntryId = journalEntryId,
                SourceType = journalEntryId.HasValue ? MapSourceType(posting) : input.Line.SourceType,
                SourceEntityType = journalEntryId.HasValue ? posting!.SourceEntityType : BankTreasurySupport.Optional(input.Line.SourceEntityType, 128),
                SourceEntityId = journalEntryId.HasValue ? posting!.SourceEntityId : BankTreasurySupport.NormalizeGuid(input.Line.SourceEntityId),
                Direction = statement.Direction,
                AmountMinor = input.Line.AmountMinor,
                Memo = BankTreasurySupport.Optional(input.Line.Memo, 1000),
                SortOrder = input.SortOrder,
                IsActive = true
            });
        }

        return output;
    }

    public static async Task RecalculateAsync(IAppDbContext db, BankReconciliationMatch match, CancellationToken ct)
    {
        var active = match.Lines.Where(x => !x.IsDeleted && x.IsActive).ToList();
        var statementIds = active.Select(x => x.BankStatementLineId).Distinct().ToList();
        var journalIds = active.Where(x => x.JournalEntryId.HasValue).Select(x => x.JournalEntryId!.Value).Distinct().ToList();
        match.BankTotalMinor = statementIds.Count == 0
            ? 0
            : await db.Set<BankStatementLine>()
                .AsNoTracking()
                .Where(x => statementIds.Contains(x.Id) && !x.IsDeleted)
                .SumAsync(x => x.AmountMinor, ct)
                .ConfigureAwait(false);
        match.FinanceTotalMinor = 0;
        if (journalIds.Count > 0)
        {
            var entries = await db.Set<JournalEntry>()
                .AsNoTracking()
                .Where(x => journalIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new
                {
                    Debit = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.DebitMinor),
                    Credit = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.CreditMinor)
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            match.FinanceTotalMinor = entries.Sum(x => Math.Max(x.Debit, x.Credit));
        }
        match.DifferenceMinor = match.BankTotalMinor - match.FinanceTotalMinor;
    }

    public static async Task RecordEvidenceAsync(BusinessEventService? events, BankReconciliationMatch match, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"bankReconciliationMatchId":"{{match.Id}}","businessId":"{{match.BusinessId}}","bankAccountId":"{{match.BankAccountId}}","status":"{{match.Status}}","currency":"{{match.Currency}}","bankTotalMinor":{{match.BankTotalMinor}},"financeTotalMinor":{{match.FinanceTotalMinor}},"differenceMinor":{{match.DifferenceMinor}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(match.BusinessId, "BankReconciliationMatch", match.Id, $"treasury.bank_reconciliation.{action}", $"treasury.bank_reconciliation.{action}:{match.Id}:{match.Status}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Bank reconciliation {action}", null, null, null, payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(match.BusinessId, "BankReconciliationMatch", match.Id, auditAction, now, null, eventResult.Value, $"Bank reconciliation {action}", null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }

    private static BankReconciliationSourceType MapSourceType(JournalEntry? posting)
    {
        var source = posting?.SourceEntityType ?? string.Empty;
        if (source.Equals("SupplierPayment", StringComparison.OrdinalIgnoreCase)) return BankReconciliationSourceType.SupplierPayment;
        if (source.Equals("Payment", StringComparison.OrdinalIgnoreCase) || source.Equals("CustomerPayment", StringComparison.OrdinalIgnoreCase)) return BankReconciliationSourceType.CustomerPayment;
        if (source.Equals("Refund", StringComparison.OrdinalIgnoreCase)) return BankReconciliationSourceType.Refund;
        return BankReconciliationSourceType.JournalEntry;
    }
}
