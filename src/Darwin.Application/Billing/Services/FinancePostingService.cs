using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Creates idempotent, balanced journal postings for future finance automation.
/// </summary>
public sealed class FinancePostingService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public FinancePostingService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<FinancePostingResult>> PostAsync(FinancePostingCommand command, CancellationToken ct = default)
    {
        var normalized = Normalize(command);
        var validation = Validate(normalized);
        if (!validation.Succeeded)
        {
            return Result<FinancePostingResult>.Fail(validation.Error!);
        }

        var existing = await _db.Set<JournalEntry>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PostingKey == normalized.PostingKey && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (!MatchesExistingPosting(existing, normalized))
            {
                return Result<FinancePostingResult>.Fail("Posting key is already used for a different source.");
            }

            return Result<FinancePostingResult>.Ok(new FinancePostingResult(existing.Id, existing.PostingKey!, Created: false));
        }

        var accountIds = normalized.Lines.Select(x => x.AccountId).Distinct().ToArray();
        var accounts = await _db.Set<FinancialAccount>()
            .Where(x => accountIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.BusinessId })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (accounts.Count != accountIds.Length)
        {
            return Result<FinancePostingResult>.Fail("All posting accounts must exist.");
        }

        if (accounts.Any(x => x.BusinessId != normalized.BusinessId))
        {
            return Result<FinancePostingResult>.Fail("Posting accounts must belong to the posting business.");
        }

        var nowUtc = _clock.UtcNow;
        var entry = new JournalEntry
        {
            BusinessId = normalized.BusinessId,
            EntryDateUtc = normalized.EntryDateUtc == default ? nowUtc : normalized.EntryDateUtc,
            Description = normalized.Description!,
            PostingStatus = JournalEntryPostingStatus.Posted,
            PostingKind = normalized.PostingKind,
            PostingKey = normalized.PostingKey,
            SourceEntityType = normalized.SourceEntityType,
            SourceEntityId = normalized.SourceEntityId,
            SourceDocumentNumber = normalized.SourceDocumentNumber,
            PostedAtUtc = nowUtc,
            PostingReason = normalized.PostingReason,
            MetadataJson = FoundationInputNormalizer.Json(normalized.MetadataJson),
            Lines = normalized.Lines
                .Select(line => new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    DebitMinor = line.DebitMinor,
                    CreditMinor = line.CreditMinor,
                    Memo = line.Memo
                })
                .ToList()
        };

        _db.Set<JournalEntry>().Add(entry);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<FinancePostingResult>.Ok(new FinancePostingResult(entry.Id, entry.PostingKey!, Created: true));
    }

    public async Task<FinancePostingResult?> GetByPostingKeyAsync(string? postingKey, CancellationToken ct = default)
    {
        var normalizedPostingKey = FoundationInputNormalizer.Required(postingKey);
        if (normalizedPostingKey is null)
        {
            return null;
        }

        return await _db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.PostingKey == normalizedPostingKey && !x.IsDeleted)
            .Select(x => new FinancePostingResult(x.Id, x.PostingKey!, Created: false))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private static NormalizedFinancePostingCommand Normalize(FinancePostingCommand command)
        => new(
            command.BusinessId,
            command.EntryDateUtc,
            command.PostingKind,
            FoundationInputNormalizer.Required(command.PostingKey),
            FoundationInputNormalizer.Required(command.SourceEntityType),
            command.SourceEntityId,
            FoundationInputNormalizer.Optional(command.SourceDocumentNumber),
            FoundationInputNormalizer.Required(command.Description),
            FoundationInputNormalizer.Optional(command.PostingReason),
            FoundationInputNormalizer.Json(command.MetadataJson),
            (command.Lines ?? Array.Empty<FinancePostingLineCommand>())
                .Select(line => new NormalizedFinancePostingLineCommand(
                    line.AccountId,
                    line.DebitMinor,
                    line.CreditMinor,
                    FoundationInputNormalizer.Optional(line.Memo)))
                .ToArray());

    private static Result Validate(NormalizedFinancePostingCommand command)
    {
        if (command.BusinessId == Guid.Empty)
        {
            return Result.Fail("Business id is required.");
        }

        if (!Enum.IsDefined(typeof(JournalEntryPostingKind), command.PostingKind) ||
            command.PostingKind == JournalEntryPostingKind.Manual)
        {
            return Result.Fail("Automated posting kind is required.");
        }

        if (command.PostingKey is null)
        {
            return Result.Fail("Posting key is required.");
        }

        if (command.SourceEntityType is null)
        {
            return Result.Fail("Source entity type is required.");
        }

        if (command.SourceEntityId == Guid.Empty)
        {
            return Result.Fail("Source entity id is required.");
        }

        if (command.Description is null)
        {
            return Result.Fail("Posting description is required.");
        }

        if (command.Lines.Count == 0)
        {
            return Result.Fail("At least two posting lines are required.");
        }

        if (command.Lines.Count < 2)
        {
            return Result.Fail("At least two posting lines are required.");
        }

        foreach (var line in command.Lines)
        {
            if (line.AccountId == Guid.Empty)
            {
                return Result.Fail("Posting line account is required.");
            }

            if (line.DebitMinor < 0 || line.CreditMinor < 0)
            {
                return Result.Fail("Posting line amounts cannot be negative.");
            }

            if ((line.DebitMinor == 0 && line.CreditMinor == 0) ||
                (line.DebitMinor > 0 && line.CreditMinor > 0))
            {
                return Result.Fail("Posting line must contain either debit or credit.");
            }
        }

        var debitTotal = command.Lines.Sum(x => x.DebitMinor);
        var creditTotal = command.Lines.Sum(x => x.CreditMinor);
        if (debitTotal <= 0 || debitTotal != creditTotal)
        {
            return Result.Fail("Posting lines must be balanced.");
        }

        if (ContainsSensitiveData(command))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance posting data.");
        }

        return Result.Ok();
    }

    private static bool ContainsSensitiveData(NormalizedFinancePostingCommand command)
        => FoundationInputNormalizer.LooksSensitive(command.Description) ||
           FoundationInputNormalizer.LooksSensitive(command.PostingReason) ||
           FoundationInputNormalizer.LooksSensitive(command.MetadataJson) ||
           command.Lines.Any(line => FoundationInputNormalizer.LooksSensitive(line.Memo));

    private static bool MatchesExistingPosting(JournalEntry existing, NormalizedFinancePostingCommand command)
        => existing.BusinessId == command.BusinessId &&
           existing.PostingKind == command.PostingKind &&
           string.Equals(existing.SourceEntityType, command.SourceEntityType, StringComparison.Ordinal) &&
           existing.SourceEntityId == command.SourceEntityId;

    private sealed record NormalizedFinancePostingCommand(
        Guid BusinessId,
        DateTime EntryDateUtc,
        JournalEntryPostingKind PostingKind,
        string? PostingKey,
        string? SourceEntityType,
        Guid SourceEntityId,
        string? SourceDocumentNumber,
        string? Description,
        string? PostingReason,
        string MetadataJson,
        IReadOnlyList<NormalizedFinancePostingLineCommand> Lines);

    private sealed record NormalizedFinancePostingLineCommand(
        Guid AccountId,
        long DebitMinor,
        long CreditMinor,
        string? Memo);
}

public sealed record FinancePostingCommand(
    Guid BusinessId,
    DateTime EntryDateUtc,
    JournalEntryPostingKind PostingKind,
    string? PostingKey,
    string? SourceEntityType,
    Guid SourceEntityId,
    string? Description,
    IReadOnlyList<FinancePostingLineCommand>? Lines,
    string? SourceDocumentNumber = null,
    string? PostingReason = null,
    string? MetadataJson = null);

public sealed record FinancePostingLineCommand(
    Guid AccountId,
    long DebitMinor,
    long CreditMinor,
    string? Memo = null);

public sealed record FinancePostingResult(
    Guid JournalEntryId,
    string PostingKey,
    bool Created);
