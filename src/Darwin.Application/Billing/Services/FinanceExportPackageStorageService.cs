using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

public sealed class FinanceExportPackageStorageService
{
    public const string EntityType = "FinanceExportBatch";
    public const string ProfileName = "FinanceExports";
    public const string ContainerName = "finance-exports";

    private readonly IAppDbContext _db;
    private readonly IObjectStorageService _storage;
    private readonly DocumentRecordService _documents;
    private readonly FinanceExportBatchService _batchService;
    private readonly FinanceExportPackageBuilderService _packageBuilder;

    public FinanceExportPackageStorageService(
        IAppDbContext db,
        IObjectStorageService storage,
        DocumentRecordService documents,
        FinanceExportBatchService batchService,
        FinanceExportPackageBuilderService packageBuilder)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _packageBuilder = packageBuilder ?? throw new ArgumentNullException(nameof(packageBuilder));
    }

    public bool IsStorageReady()
    {
        try
        {
            return _storage.GetCapabilities(new ObjectStorageContainerSelection(ContainerName, ProfileName: ProfileName)).Provider != ObjectStorageProviderKind.Database;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<Result<FinanceExportStoredPackageResult>> GenerateAndStoreAsync(Guid financeExportBatchId, CancellationToken ct = default)
    {
        if (financeExportBatchId == Guid.Empty)
        {
            return Result<FinanceExportStoredPackageResult>.Fail("Finance export batch id is required.");
        }

        if (!IsStorageReady())
        {
            return Result<FinanceExportStoredPackageResult>.Fail("Finance export package storage is not configured.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == financeExportBatchId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return Result<FinanceExportStoredPackageResult>.Fail("Finance export batch was not found.");
        }

        if (batch.Status is FinanceExportBatchStatus.Generated or FinanceExportBatchStatus.Delivered)
        {
            return Result<FinanceExportStoredPackageResult>.Fail("Generated or delivered finance export batches cannot be regenerated.");
        }

        if (batch.Status == FinanceExportBatchStatus.Cancelled)
        {
            return Result<FinanceExportStoredPackageResult>.Fail("Cancelled finance export batches cannot be generated.");
        }

        var attempt = await _batchService.StartAttemptAsync(batch.Id, "{\"purpose\":\"webadmin-package-storage\"}", ct).ConfigureAwait(false);
        if (!attempt.Succeeded)
        {
            return Result<FinanceExportStoredPackageResult>.Fail(attempt.Error!);
        }

        try
        {
            var package = await _packageBuilder.BuildPackageContentAsync(batch.Id, ct).ConfigureAwait(false);
            if (!package.Succeeded)
            {
                await _batchService.FailAttemptAsync(attempt.Value!.AttemptId, "Finance export package generation failed.", ct).ConfigureAwait(false);
                return Result<FinanceExportStoredPackageResult>.Fail(package.Error!);
            }

            var packageValue = package.Value!;
            var stored = await StorePackageAsync(batch, packageValue, ct).ConfigureAwait(false);
            if (!stored.Succeeded)
            {
                await _batchService.FailAttemptAsync(attempt.Value!.AttemptId, "Finance export package storage failed.", ct).ConfigureAwait(false);
                return Result<FinanceExportStoredPackageResult>.Fail(stored.Error!);
            }

            var storedValue = stored.Value!;
            var documentId = await RegisterPackageDocumentAsync(batch, packageValue, storedValue, ct).ConfigureAwait(false);
            if (!documentId.Succeeded)
            {
                await _batchService.FailAttemptAsync(attempt.Value!.AttemptId, "Finance export package document registration failed.", ct).ConfigureAwait(false);
                return Result<FinanceExportStoredPackageResult>.Fail(documentId.Error!);
            }

            var completed = await _batchService.CompleteAttemptAsync(
                    attempt.Value!.AttemptId,
                    packageValue.PackageHashSha256,
                    packageValue.ContentType,
                    packageValue.FileName,
                    ct)
                .ConfigureAwait(false);
            if (!completed.Succeeded)
            {
                return Result<FinanceExportStoredPackageResult>.Fail(completed.Error!);
            }

            return Result<FinanceExportStoredPackageResult>.Ok(new FinanceExportStoredPackageResult(
                batch.Id,
                attempt.Value.AttemptId,
                documentId.Value,
                storedValue.StorageContainer,
                storedValue.StorageKey,
                packageValue.PackageHashSha256,
                packageValue.FileName,
                packageValue.SizeBytes));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            await _batchService.FailAttemptAsync(attempt.Value!.AttemptId, "Finance export package storage failed.", ct).ConfigureAwait(false);
            return Result<FinanceExportStoredPackageResult>.Fail(ex.Message);
        }
    }

    public async Task<Result<FinanceExportPackageDownloadResult>> GetStoredPackageAsync(Guid financeExportBatchId, CancellationToken ct = default)
    {
        if (financeExportBatchId == Guid.Empty)
        {
            return Result<FinanceExportPackageDownloadResult>.Fail("Finance export batch id is required.");
        }

        var document = await GetLatestPackageDocumentAsync(financeExportBatchId, ct).ConfigureAwait(false);
        if (document is null)
        {
            return Result<FinanceExportPackageDownloadResult>.Fail("Finance export package document was not found.");
        }

        var read = await _storage.ReadAsync(
                new ObjectStorageObjectReference(document.StorageContainer, document.StorageKey, ProfileName: ProfileName),
                ct)
            .ConfigureAwait(false);
        if (read is null)
        {
            return Result<FinanceExportPackageDownloadResult>.Fail("Finance export package object was not found.");
        }

        return Result<FinanceExportPackageDownloadResult>.Ok(new FinanceExportPackageDownloadResult(
            read.Content,
            string.IsNullOrWhiteSpace(read.ContentType) ? FinanceExportPackageBuilderService.PackageContentType : read.ContentType,
            string.IsNullOrWhiteSpace(read.FileName) ? document.FileName : read.FileName!,
            read.ContentLength,
            read.Sha256Hash ?? document.ContentHash));
    }

    private async Task<Result<StoredObjectReference>> StorePackageAsync(
        FinanceExportBatch batch,
        FinanceExportPackageContent package,
        CancellationToken ct)
    {
        var objectKey = BuildObjectKey(batch);
        var reference = new ObjectStorageObjectReference(ContainerName, objectKey, ProfileName: ProfileName);
        if (await _storage.ExistsAsync(reference, ct).ConfigureAwait(false))
        {
            var existing = await _storage.ReadAsync(reference, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return Result<StoredObjectReference>.Fail("Finance export package object could not be read.");
            }

            await using (existing.Content.ConfigureAwait(false))
            await using (var buffer = new MemoryStream())
            {
                await existing.Content.CopyToAsync(buffer, ct).ConfigureAwait(false);
                var existingHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
                if (!string.Equals(existingHash, package.PackageHashSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<StoredObjectReference>.Fail("Existing finance export package hash does not match the generated package.");
                }
            }

            return Result<StoredObjectReference>.Ok(new StoredObjectReference(ContainerName, objectKey));
        }

        var bytes = Encoding.UTF8.GetBytes(package.PackageJson);
        await using var content = new MemoryStream(bytes, writable: false);
        var write = await _storage.SaveAsync(
                new ObjectStorageWriteRequest(
                    ContainerName,
                    objectKey,
                    package.ContentType,
                    package.FileName,
                    content,
                    bytes.LongLength,
                    package.PackageHashSha256,
                    new Dictionary<string, string>
                    {
                        ["entity-type"] = EntityType,
                        ["entity-id"] = batch.Id.ToString("N"),
                        ["business-id"] = batch.BusinessId.ToString("N"),
                        ["export-key"] = batch.ExportKey
                    },
                    OverwritePolicy: ObjectOverwritePolicy.Disallow,
                    ProfileName: ProfileName),
                ct)
            .ConfigureAwait(false);
        return Result<StoredObjectReference>.Ok(new StoredObjectReference(write.ContainerName, write.ObjectKey));
    }

    private async Task<Result<Guid>> RegisterPackageDocumentAsync(
        FinanceExportBatch batch,
        FinanceExportPackageContent package,
        StoredObjectReference stored,
        CancellationToken ct)
    {
        var existing = await GetLatestPackageDocumentAsync(batch.Id, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<Guid>.Ok(existing.Id);
        }

        return await _documents.RegisterDocumentAsync(
                new RegisterDocumentRecordCommand(
                    EntityType,
                    batch.Id,
                    DocumentRecordKind.Evidence,
                    "Finance export package",
                    package.FileName,
                    package.ContentType,
                    package.SizeBytes,
                    package.PackageHashSha256,
                    ProfileName,
                    stored.StorageContainer,
                    stored.StorageKey,
                    Visibility: FoundationVisibility.Internal,
                    MetadataJson: "{\"source\":\"finance-export\"}"),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<DocumentRecordDto?> GetLatestPackageDocumentAsync(Guid batchId, CancellationToken ct)
        => (await _documents.GetDocumentsForEntityAsync(EntityType, batchId, FoundationVisibility.Internal, ct).ConfigureAwait(false))
            .Where(x => x.DocumentKind == DocumentRecordKind.Evidence &&
                        string.Equals(x.ContentType, FinanceExportPackageBuilderService.PackageContentType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

    private static string BuildObjectKey(FinanceExportBatch batch)
        => ObjectStorageKeyBuilder.Build(
            "finance-exports",
            batch.BusinessId.ToString("N"),
            $"{batch.PeriodStartUtc:yyyyMMdd}-{batch.PeriodEndUtc:yyyyMMdd}",
            batch.Id.ToString("N") + ".json");

    private sealed record StoredObjectReference(string StorageContainer, string StorageKey);
}

public sealed record FinanceExportStoredPackageResult(
    Guid BatchId,
    Guid AttemptId,
    Guid DocumentRecordId,
    string StorageContainer,
    string StorageKey,
    string PackageHashSha256,
    string FileName,
    long SizeBytes);

public sealed record FinanceExportPackageDownloadResult(
    Stream Content,
    string ContentType,
    string FileName,
    long? ContentLength,
    string? PackageHashSha256);
