using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Owns idempotent finance export batch identity and safe retry evidence.
/// </summary>
public sealed class FinanceExportBatchService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public FinanceExportBatchService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<FinanceExportBatchResult>> GetOrCreateBatchAsync(
        FinanceExportBatchCommand command,
        CancellationToken ct = default)
    {
        var metadataJson = FoundationInputNormalizer.Json(command.MetadataJson);
        var validation = ValidateBatchCommand(command, metadataJson);
        if (!validation.Succeeded)
        {
            return Result<FinanceExportBatchResult>.Fail(validation.Error!);
        }

        var externalSystem = await _db.Set<ExternalSystem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ExternalSystemId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (externalSystem is null)
        {
            return Result<FinanceExportBatchResult>.Fail("External accounting system was not found.");
        }

        if (!externalSystem.IsActive || externalSystem.Kind != ExternalSystemKind.Accounting)
        {
            return Result<FinanceExportBatchResult>.Fail("External system must be an active accounting system.");
        }

        var exportKey = BuildExportKey(command);
        var existing = await _db.Set<FinanceExportBatch>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.ExternalSystemId == command.ExternalSystemId &&
                x.ExportKey == exportKey &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<FinanceExportBatchResult>.Ok(new FinanceExportBatchResult(existing.Id, existing.ExportKey, Created: false));
        }

        var batch = new FinanceExportBatch
        {
            BusinessId = command.BusinessId,
            ExternalSystemId = command.ExternalSystemId,
            ExportKey = exportKey,
            PeriodStartUtc = command.PeriodStartUtc,
            PeriodEndUtc = command.PeriodEndUtc,
            PostingStatusMode = command.PostingStatusMode,
            Status = FinanceExportBatchStatus.Draft,
            MetadataJson = metadataJson
        };
        _db.Set<FinanceExportBatch>().Add(batch);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<FinanceExportBatchResult>.Ok(new FinanceExportBatchResult(batch.Id, batch.ExportKey, Created: true));
    }

    public async Task<Result<FinanceExportAttemptResult>> StartAttemptAsync(
        Guid batchId,
        string? metadataJson = null,
        CancellationToken ct = default)
    {
        var normalizedMetadata = FoundationInputNormalizer.Json(metadataJson);
        if (batchId == Guid.Empty)
        {
            return Result<FinanceExportAttemptResult>.Fail("Finance export batch id is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(normalizedMetadata))
        {
            return Result<FinanceExportAttemptResult>.Fail("Sensitive secrets must not be stored in finance export attempt metadata.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return Result<FinanceExportAttemptResult>.Fail("Finance export batch was not found.");
        }

        var nextAttemptNumber = batch.Attempts.Where(x => !x.IsDeleted).Select(x => x.AttemptNumber).DefaultIfEmpty().Max() + 1;
        var attempt = new FinanceExportAttempt
        {
            FinanceExportBatchId = batch.Id,
            AttemptNumber = nextAttemptNumber,
            Status = FinanceExportAttemptStatus.Started,
            StartedAtUtc = _clock.UtcNow,
            MetadataJson = normalizedMetadata
        };
        batch.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<FinanceExportAttemptResult>.Ok(new FinanceExportAttemptResult(attempt.Id, attempt.AttemptNumber));
    }

    public async Task<Result> CompleteAttemptAsync(
        Guid attemptId,
        string packageHashSha256,
        string? packageContentType = null,
        string? packageFileName = null,
        CancellationToken ct = default)
    {
        var packageHash = FoundationInputNormalizer.Required(packageHashSha256);
        var contentType = FoundationInputNormalizer.Optional(packageContentType);
        var fileName = FoundationInputNormalizer.Optional(packageFileName);
        if (attemptId == Guid.Empty)
        {
            return Result.Fail("Finance export attempt id is required.");
        }

        if (packageHash is null)
        {
            return Result.Fail("Package hash is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(packageHash) ||
            FoundationInputNormalizer.LooksSensitive(contentType) ||
            FoundationInputNormalizer.LooksSensitive(fileName))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance export package metadata.");
        }

        var attempt = await _db.Set<FinanceExportAttempt>()
            .FirstOrDefaultAsync(x => x.Id == attemptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (attempt is null)
        {
            return Result.Fail("Finance export attempt was not found.");
        }

        if (attempt.Status != FinanceExportAttemptStatus.Started)
        {
            return Result.Fail("Only started finance export attempts can be completed.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .FirstAsync(x => x.Id == attempt.FinanceExportBatchId, ct)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        attempt.Status = FinanceExportAttemptStatus.Succeeded;
        attempt.CompletedAtUtc = now;
        attempt.PackageHashSha256 = packageHash;
        batch.Status = FinanceExportBatchStatus.Generated;
        batch.GeneratedAtUtc ??= now;
        batch.PackageHashSha256 = packageHash;
        batch.PackageContentType = contentType;
        batch.PackageFileName = fileName;
        batch.ErrorSummary = null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> FailAttemptAsync(
        Guid attemptId,
        string errorSummary,
        CancellationToken ct = default,
        bool markBatchFailed = true)
    {
        var error = FoundationInputNormalizer.Required(errorSummary);
        if (attemptId == Guid.Empty)
        {
            return Result.Fail("Finance export attempt id is required.");
        }

        if (error is null)
        {
            return Result.Fail("Error summary is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(error))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance export errors.");
        }

        var attempt = await _db.Set<FinanceExportAttempt>()
            .FirstOrDefaultAsync(x => x.Id == attemptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (attempt is null)
        {
            return Result.Fail("Finance export attempt was not found.");
        }

        if (attempt.Status != FinanceExportAttemptStatus.Started)
        {
            return Result.Fail("Only started finance export attempts can fail.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .FirstAsync(x => x.Id == attempt.FinanceExportBatchId, ct)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        attempt.Status = FinanceExportAttemptStatus.Failed;
        attempt.FailedAtUtc = now;
        attempt.ErrorSummary = error;
        if (markBatchFailed)
        {
            batch.Status = FinanceExportBatchStatus.Failed;
            batch.FailedAtUtc = now;
            batch.ErrorSummary = error;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> MarkDeliveredAsync(
        Guid attemptId,
        string packageHashSha256,
        string? packageContentType = null,
        string? packageFileName = null,
        string? deliveryMetadataJson = null,
        CancellationToken ct = default)
    {
        var packageHash = FoundationInputNormalizer.Required(packageHashSha256);
        var contentType = FoundationInputNormalizer.Optional(packageContentType);
        var fileName = FoundationInputNormalizer.Optional(packageFileName);
        var metadata = FoundationInputNormalizer.Json(deliveryMetadataJson);
        if (attemptId == Guid.Empty)
        {
            return Result.Fail("Finance export attempt id is required.");
        }

        if (packageHash is null)
        {
            return Result.Fail("Package hash is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(packageHash) ||
            FoundationInputNormalizer.LooksSensitive(contentType) ||
            FoundationInputNormalizer.LooksSensitive(fileName) ||
            FoundationInputNormalizer.LooksSensitive(metadata))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance export delivery metadata.");
        }

        var attempt = await _db.Set<FinanceExportAttempt>()
            .FirstOrDefaultAsync(x => x.Id == attemptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (attempt is null)
        {
            return Result.Fail("Finance export attempt was not found.");
        }

        if (attempt.Status != FinanceExportAttemptStatus.Started)
        {
            return Result.Fail("Only started finance export attempts can be delivered.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .FirstAsync(x => x.Id == attempt.FinanceExportBatchId, ct)
            .ConfigureAwait(false);
        if (batch.Status != FinanceExportBatchStatus.Generated)
        {
            return Result.Fail("Only generated finance export batches can be delivered.");
        }

        var now = _clock.UtcNow;
        attempt.Status = FinanceExportAttemptStatus.Succeeded;
        attempt.CompletedAtUtc = now;
        attempt.PackageHashSha256 = packageHash;
        attempt.MetadataJson = metadata;
        batch.Status = FinanceExportBatchStatus.Delivered;
        batch.DeliveredAtUtc ??= now;
        batch.PackageHashSha256 = packageHash;
        batch.PackageContentType = contentType;
        batch.PackageFileName = fileName;
        batch.ErrorSummary = null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public static string BuildExportKey(FinanceExportBatchCommand command)
        => string.Join(
            ':',
            "finance-export",
            command.BusinessId.ToString("N"),
            command.ExternalSystemId.ToString("N"),
            command.PeriodStartUtc.ToUniversalTime().ToString("yyyyMMddHHmmss"),
            command.PeriodEndUtc.ToUniversalTime().ToString("yyyyMMddHHmmss"),
            command.PostingStatusMode.ToString().ToLowerInvariant());

    private static Result ValidateBatchCommand(FinanceExportBatchCommand command, string metadataJson)
    {
        if (command.BusinessId == Guid.Empty)
        {
            return Result.Fail("Business id is required.");
        }

        if (command.ExternalSystemId == Guid.Empty)
        {
            return Result.Fail("External accounting system id is required.");
        }

        if (command.PeriodStartUtc == default || command.PeriodEndUtc == default || command.PeriodStartUtc >= command.PeriodEndUtc)
        {
            return Result.Fail("Finance export period is invalid.");
        }

        if (!Enum.IsDefined(typeof(FinanceExportPostingStatusMode), command.PostingStatusMode))
        {
            return Result.Fail("Finance export posting status mode is invalid.");
        }

        if (FoundationInputNormalizer.LooksSensitive(metadataJson))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance export metadata.");
        }

        return Result.Ok();
    }
}

public sealed record FinanceExportBatchCommand(
    Guid BusinessId,
    Guid ExternalSystemId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    FinanceExportPostingStatusMode PostingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
    string? MetadataJson = null);

public sealed record FinanceExportBatchResult(Guid BatchId, string ExportKey, bool Created);

public sealed record FinanceExportAttemptResult(Guid AttemptId, int AttemptNumber);
