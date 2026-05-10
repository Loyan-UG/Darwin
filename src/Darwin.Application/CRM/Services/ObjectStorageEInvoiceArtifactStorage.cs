using System.Security.Cryptography;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Storage;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Services;

/// <summary>
/// Stores generated e-invoice artifacts through the generic object-storage boundary.
/// </summary>
public sealed class ObjectStorageEInvoiceArtifactStorage : IEInvoiceArtifactStorage
{
    private const string InvoiceArchiveProfileName = "InvoiceArchive";
    private readonly IAppDbContext _db;
    private readonly IObjectStorageService _objectStorage;
    private readonly InvoiceArchiveStorageSelection _selection;

    public ObjectStorageEInvoiceArtifactStorage(
        IAppDbContext db,
        IObjectStorageService objectStorage,
        InvoiceArchiveStorageSelection selection)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public async Task<EInvoiceArtifactStorageResult> SaveAsync(EInvoiceArtifact artifact, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact.InvoiceId == Guid.Empty)
        {
            throw new InvalidOperationException("E-invoice artifact invoice id is required.");
        }

        if (artifact.Content.Length == 0)
        {
            throw new InvalidOperationException("E-invoice artifact content is required.");
        }

        var hash = ComputeSha256(artifact.Content);
        var retainUntilUtc = artifact.GeneratedAtUtc.AddYears(await GetRetentionYearsAsync(ct).ConfigureAwait(false));
        await using var stream = new MemoryStream(artifact.Content, writable: false);

        var writeResult = await _objectStorage.SaveAsync(
            new ObjectStorageWriteRequest(
                ResolveContainerName(),
                ObjectStorageKeyBuilder.ForInvoiceArtifact(
                    artifact.InvoiceId,
                    artifact.GeneratedAtUtc,
                    "e-invoice",
                    artifact.Format.ToString(),
                    artifact.InvoiceId),
                string.IsNullOrWhiteSpace(artifact.ContentType) ? "application/octet-stream" : artifact.ContentType.Trim(),
                string.IsNullOrWhiteSpace(artifact.FileName) ? BuildFallbackFileName(artifact) : artifact.FileName.Trim(),
                stream,
                stream.Length,
                hash,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["invoice-id"] = artifact.InvoiceId.ToString("N"),
                    ["artifact-type"] = "e-invoice",
                    ["artifact-format"] = artifact.Format.ToString(),
                    ["validation-profile"] = artifact.ValidationProfile
                },
                retainUntilUtc,
                ObjectRetentionMode.Compliance,
                LegalHold: false,
                ObjectOverwritePolicy.Disallow,
                ProfileName: InvoiceArchiveProfileName),
            ct).ConfigureAwait(false);

        return new EInvoiceArtifactStorageResult(
            writeResult.Provider.ToString(),
            writeResult.ContainerName,
            writeResult.ObjectKey,
            writeResult.VersionId,
            writeResult.Sha256Hash,
            writeResult.ContentLength,
            writeResult.CreatedAtUtc,
            writeResult.RetentionUntilUtc,
            writeResult.IsImmutable);
    }

    private string ResolveContainerName()
        => string.IsNullOrWhiteSpace(_selection.ObjectStorageContainerName)
            ? string.Empty
            : _selection.ObjectStorageContainerName.Trim();

    private async Task<int> GetRetentionYearsAsync(CancellationToken ct)
    {
        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.InvoiceArchiveRetentionYears)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return Math.Clamp(settings ?? 10, 1, 30);
    }

    private static string BuildFallbackFileName(EInvoiceArtifact artifact)
        => $"invoice-{artifact.InvoiceId:N}-{artifact.Format.ToString().ToLowerInvariant()}.bin";

    private static string ComputeSha256(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
