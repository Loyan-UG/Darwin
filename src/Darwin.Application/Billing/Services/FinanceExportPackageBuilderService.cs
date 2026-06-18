using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Builds deterministic internal finance export packages from posted journal entries.
/// </summary>
public sealed class FinanceExportPackageBuilderService
{
    public const string PackageContentType = "application/vnd.darwin.finance-export+json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly FinanceExportBatchService _batchService;

    public FinanceExportPackageBuilderService(
        IAppDbContext db,
        IClock clock,
        FinanceExportBatchService batchService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
    }

    public async Task<Result<FinanceExportPackageBuildResult>> BuildAsync(
        FinanceExportPackageBuildCommand command,
        CancellationToken ct = default)
    {
        var batch = await LoadBuildableBatchAsync(command.FinanceExportBatchId, ct).ConfigureAwait(false);
        if (!batch.Succeeded)
        {
            return Result<FinanceExportPackageBuildResult>.Fail(batch.Error!);
        }

        var attempt = await _batchService.StartAttemptAsync(batch.Value!.Id, "{\"purpose\":\"package-builder\"}", ct).ConfigureAwait(false);
        if (!attempt.Succeeded)
        {
            return Result<FinanceExportPackageBuildResult>.Fail(attempt.Error!);
        }

        try
        {
            var packageContent = await BuildPackageContentAsync(batch.Value, ct).ConfigureAwait(false);
            var completed = await _batchService.CompleteAttemptAsync(
                    attempt.Value!.AttemptId,
                    packageContent.PackageHashSha256,
                    PackageContentType,
                    packageContent.FileName,
                    ct)
                .ConfigureAwait(false);
            if (!completed.Succeeded)
            {
                return Result<FinanceExportPackageBuildResult>.Fail(completed.Error!);
            }

            return Result<FinanceExportPackageBuildResult>.Ok(new FinanceExportPackageBuildResult(
                batch.Value.Id,
                attempt.Value.AttemptId,
                packageContent.PackageJson,
                packageContent.PackageHashSha256,
                packageContent.EntryCount,
                packageContent.LineCount));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var error = ex.Message;
            await _batchService.FailAttemptAsync(attempt.Value!.AttemptId, "Finance export package validation failed.", ct).ConfigureAwait(false);
            return Result<FinanceExportPackageBuildResult>.Fail(error);
        }
    }

    public async Task<Result<FinanceExportPackageContent>> BuildPackageContentAsync(
        Guid financeExportBatchId,
        CancellationToken ct = default)
    {
        var batch = await LoadBuildableBatchAsync(financeExportBatchId, ct).ConfigureAwait(false);
        return !batch.Succeeded
            ? Result<FinanceExportPackageContent>.Fail(batch.Error!)
            : await BuildPackageContentSafeAsync(batch.Value!, ct).ConfigureAwait(false);
    }

    internal async Task<FinanceExportPackageContent> BuildPackageContentAsync(FinanceExportBatch batch, CancellationToken ct)
    {
        var package = await BuildPackageAsync(batch, ct).ConfigureAwait(false);
        var packageJson = JsonSerializer.Serialize(package, JsonOptions);
        return new FinanceExportPackageContent(
            packageJson,
            ComputeSha256(packageJson),
            PackageContentType,
            $"finance-export-{batch.Id:N}.json",
            Encoding.UTF8.GetByteCount(packageJson),
            package.Header.EntryCount,
            package.Header.LineCount);
    }

    private async Task<Result<FinanceExportPackageContent>> BuildPackageContentSafeAsync(FinanceExportBatch batch, CancellationToken ct)
    {
        try
        {
            return Result<FinanceExportPackageContent>.Ok(await BuildPackageContentAsync(batch, ct).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result<FinanceExportPackageContent>.Fail(ex.Message);
        }
    }

    private async Task<Result<FinanceExportBatch>> LoadBuildableBatchAsync(Guid financeExportBatchId, CancellationToken ct)
    {
        if (financeExportBatchId == Guid.Empty)
        {
            return Result<FinanceExportBatch>.Fail("Finance export batch id is required.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == financeExportBatchId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return Result<FinanceExportBatch>.Fail("Finance export batch was not found.");
        }

        if (batch.Status is FinanceExportBatchStatus.Generated or FinanceExportBatchStatus.Delivered)
        {
            return Result<FinanceExportBatch>.Fail("Generated or delivered finance export batches cannot be regenerated.");
        }

        if (batch.Status == FinanceExportBatchStatus.Cancelled)
        {
            return Result<FinanceExportBatch>.Fail("Cancelled finance export batches cannot be generated.");
        }

        return Result<FinanceExportBatch>.Ok(batch);
    }

    private async Task<FinanceExportPackage> BuildPackageAsync(FinanceExportBatch batch, CancellationToken ct)
    {
        var allowedStatuses = batch.PostingStatusMode == FinanceExportPostingStatusMode.PostedOnly
            ? new[] { JournalEntryPostingStatus.Posted }
            : new[] { JournalEntryPostingStatus.Posted, JournalEntryPostingStatus.Reversed };

        var entries = await _db.Set<JournalEntry>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x =>
                x.BusinessId == batch.BusinessId &&
                x.EntryDateUtc >= batch.PeriodStartUtc &&
                x.EntryDateUtc < batch.PeriodEndUtc &&
                allowedStatuses.Contains(x.PostingStatus) &&
                !x.IsDeleted)
            .OrderBy(x => x.EntryDateUtc)
            .ThenBy(x => x.PostingKey)
            .ThenBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var accountIds = entries
            .SelectMany(x => x.Lines)
            .Where(x => !x.IsDeleted)
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        var accounts = await _db.Set<FinancialAccount>()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        if (accounts.Count != accountIds.Length)
        {
            throw new InvalidOperationException("All finance export line accounts must exist.");
        }

        var exportedEntries = new List<FinanceExportPackageEntry>();
        var totalDebit = 0L;
        var totalCredit = 0L;
        var lineCount = 0;
        foreach (var entry in entries)
        {
            ValidateSafeText(entry.Description, entry.PostingKey, entry.SourceEntityType, entry.SourceDocumentNumber, entry.PostingReason);
            var lines = entry.Lines
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .Select(line =>
                {
                    ValidateSafeText(line.Memo);
                    var account = accounts[line.AccountId];
                    return new FinanceExportPackageLine(
                        line.Id,
                        line.AccountId,
                        account.Code ?? string.Empty,
                        account.Name,
                        account.Type.ToString(),
                        line.DebitMinor,
                        line.CreditMinor,
                        line.Memo ?? string.Empty);
                })
                .ToList();
            totalDebit += lines.Sum(x => x.DebitMinor);
            totalCredit += lines.Sum(x => x.CreditMinor);
            lineCount += lines.Count;
            exportedEntries.Add(new FinanceExportPackageEntry(
                entry.Id,
                entry.EntryDateUtc,
                entry.PostingStatus.ToString(),
                entry.PostingKind.ToString(),
                entry.PostingKey ?? string.Empty,
                entry.SourceEntityType ?? string.Empty,
                entry.SourceEntityId,
                entry.SourceDocumentNumber ?? string.Empty,
                entry.ReversalOfJournalEntryId,
                entry.Description,
                lines));
        }

        var header = new FinanceExportPackageHeader(
            batch.Id,
            batch.ExportKey,
            batch.BusinessId,
            batch.ExternalSystemId,
            batch.PeriodStartUtc,
            batch.PeriodEndUtc,
            batch.PostingStatusMode.ToString(),
            _clock.UtcNow,
            exportedEntries.Count,
            lineCount,
            totalDebit,
            totalCredit);
        return new FinanceExportPackage(header, exportedEntries);
    }

    private static void ValidateSafeText(params string?[] values)
    {
        if (values.Any(FoundationInputNormalizer.LooksSensitive))
        {
            throw new InvalidOperationException("Sensitive secrets must not be stored in finance export packages.");
        }
    }

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record FinanceExportPackageBuildCommand(Guid FinanceExportBatchId);

public sealed record FinanceExportPackageContent(
    string PackageJson,
    string PackageHashSha256,
    string ContentType,
    string FileName,
    long SizeBytes,
    int EntryCount,
    int LineCount);

public sealed record FinanceExportPackageBuildResult(
    Guid FinanceExportBatchId,
    Guid AttemptId,
    string PackageJson,
    string PackageHashSha256,
    int EntryCount,
    int LineCount);

public sealed record FinanceExportPackage(FinanceExportPackageHeader Header, IReadOnlyList<FinanceExportPackageEntry> Entries);

public sealed record FinanceExportPackageHeader(
    Guid BatchId,
    string ExportKey,
    Guid BusinessId,
    Guid ExternalSystemId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    string PostingStatusMode,
    DateTime GeneratedAtUtc,
    int EntryCount,
    int LineCount,
    long TotalDebitMinor,
    long TotalCreditMinor);

public sealed record FinanceExportPackageEntry(
    Guid JournalEntryId,
    DateTime EntryDateUtc,
    string PostingStatus,
    string PostingKind,
    string PostingKey,
    string SourceEntityType,
    Guid? SourceEntityId,
    string SourceDocumentNumber,
    Guid? ReversalOfJournalEntryId,
    string Description,
    IReadOnlyList<FinanceExportPackageLine> Lines);

public sealed record FinanceExportPackageLine(
    Guid JournalEntryLineId,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    long DebitMinor,
    long CreditMinor,
    string Memo);
