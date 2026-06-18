using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Builds read-only receivables projections from posted journal entries and account-role mappings.
/// </summary>
public sealed class ReceivablesProjectionService
{
    private readonly IAppDbContext _db;
    private readonly FinanceAccountMappingService _accountMappingService;

    public ReceivablesProjectionService(IAppDbContext db, FinanceAccountMappingService accountMappingService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _accountMappingService = accountMappingService ?? throw new ArgumentNullException(nameof(accountMappingService));
    }

    public async Task<Result<ReceivablesSummaryDto>> GetSummaryAsync(
        ReceivablesProjectionQuery query,
        CancellationToken ct = default)
    {
        if (query.BusinessId == Guid.Empty)
        {
            return Result<ReceivablesSummaryDto>.Fail("Business id is required.");
        }

        if (query.AsOfUtc.HasValue && query.FromUtc.HasValue && query.FromUtc.Value > query.AsOfUtc.Value)
        {
            return Result<ReceivablesSummaryDto>.Fail("Receivables projection date range is invalid.");
        }

        var accountResolution = await _accountMappingService.ResolveRequiredAccountsAsync(
            query.BusinessId,
            [FinancePostingAccountRole.Receivables],
            ct).ConfigureAwait(false);
        if (!accountResolution.Succeeded)
        {
            return Result<ReceivablesSummaryDto>.Fail(accountResolution.Error!);
        }

        var receivablesAccountId = accountResolution.Value![FinancePostingAccountRole.Receivables];
        var entries = await BuildReceivableLinesQuery(query, receivablesAccountId)
            .Include(entry => entry.Lines)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var rows = entries
            .SelectMany(entry => entry.Lines
                .Where(line => !line.IsDeleted && line.AccountId == receivablesAccountId)
                .Select(line => new ReceivableLineRow(
                    entry.Id,
                    entry.EntryDateUtc,
                    entry.PostingKind,
                    entry.PostingStatus,
                    entry.PostingKey,
                    entry.SourceEntityType,
                    entry.SourceEntityId,
                    entry.SourceDocumentNumber,
                    line.DebitMinor,
                    line.CreditMinor)))
            .ToArray();

        var totalDebit = rows.Sum(x => x.DebitMinor);
        var totalCredit = rows.Sum(x => x.CreditMinor);
        var items = rows
            .GroupBy(x => new { x.SourceEntityType, x.SourceEntityId, x.SourceDocumentNumber })
            .Select(group =>
            {
                var debit = group.Sum(x => x.DebitMinor);
                var credit = group.Sum(x => x.CreditMinor);
                var lastEntry = group.OrderByDescending(x => x.EntryDateUtc).First();
                return new ReceivableSourceBalanceDto(
                    group.Key.SourceEntityType,
                    group.Key.SourceEntityId,
                    group.Key.SourceDocumentNumber,
                    debit,
                    credit,
                    debit - credit,
                    lastEntry.EntryDateUtc,
                    lastEntry.PostingKind,
                    lastEntry.PostingStatus,
                    lastEntry.PostingKey);
            })
            .OrderByDescending(x => x.LastEntryDateUtc)
            .ThenBy(x => x.SourceEntityType)
            .ThenBy(x => x.SourceDocumentNumber)
            .ToArray();

        var summary = new ReceivablesSummaryDto(
            query.BusinessId,
            receivablesAccountId,
            query.AsOfUtc,
            totalDebit,
            totalCredit,
            totalDebit - totalCredit,
            items);
        return Result<ReceivablesSummaryDto>.Ok(summary);
    }

    private IQueryable<JournalEntry> BuildReceivableLinesQuery(
        ReceivablesProjectionQuery query,
        Guid receivablesAccountId)
    {
        var entries = _db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(entry =>
                entry.BusinessId == query.BusinessId &&
                !entry.IsDeleted &&
                (entry.PostingStatus == JournalEntryPostingStatus.Posted ||
                 entry.PostingStatus == JournalEntryPostingStatus.Reversed) &&
                entry.Lines.Any(line => !line.IsDeleted && line.AccountId == receivablesAccountId));

        if (query.FromUtc.HasValue)
        {
            entries = entries.Where(x => x.EntryDateUtc >= query.FromUtc.Value);
        }

        if (query.AsOfUtc.HasValue)
        {
            entries = entries.Where(x => x.EntryDateUtc <= query.AsOfUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceEntityType))
        {
            var sourceEntityType = query.SourceEntityType.Trim();
            entries = entries.Where(x => x.SourceEntityType == sourceEntityType);
        }

        if (query.SourceEntityId.HasValue && query.SourceEntityId.Value != Guid.Empty)
        {
            entries = entries.Where(x => x.SourceEntityId == query.SourceEntityId.Value);
        }

        return entries;
    }

    private sealed record ReceivableLineRow(
        Guid JournalEntryId,
        DateTime EntryDateUtc,
        JournalEntryPostingKind PostingKind,
        JournalEntryPostingStatus PostingStatus,
        string? PostingKey,
        string? SourceEntityType,
        Guid? SourceEntityId,
        string? SourceDocumentNumber,
        long DebitMinor,
        long CreditMinor);
}

public sealed record ReceivablesProjectionQuery(
    Guid BusinessId,
    DateTime? FromUtc = null,
    DateTime? AsOfUtc = null,
    string? SourceEntityType = null,
    Guid? SourceEntityId = null);

public sealed record ReceivablesSummaryDto(
    Guid BusinessId,
    Guid ReceivablesAccountId,
    DateTime? AsOfUtc,
    long TotalDebitMinor,
    long TotalCreditMinor,
    long OpenBalanceMinor,
    IReadOnlyList<ReceivableSourceBalanceDto> Sources);

public sealed record ReceivableSourceBalanceDto(
    string? SourceEntityType,
    Guid? SourceEntityId,
    string? SourceDocumentNumber,
    long DebitMinor,
    long CreditMinor,
    long OpenBalanceMinor,
    DateTime LastEntryDateUtc,
    JournalEntryPostingKind LastPostingKind,
    JournalEntryPostingStatus LastPostingStatus,
    string? LastPostingKey);
