using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Storage;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Services;

/// <summary>
/// Stores invoice archive artifacts through the generic object-storage boundary.
/// </summary>
public sealed class ObjectStorageInvoiceArchiveStorage : IInvoiceArchiveStorageProvider
{
    private const string DefaultRetentionPolicyPrefix = "invoice-archive-retention:v1";
    private readonly IAppDbContext _db;
    private readonly IObjectStorageService _objectStorage;
    private readonly InvoiceArchiveStorageSelection _selection;

    public ObjectStorageInvoiceArchiveStorage(
        IAppDbContext db,
        IObjectStorageService objectStorage,
        InvoiceArchiveStorageSelection selection)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public string ProviderName => InvoiceArchiveStorageProviderNames.S3Compatible;

    public async Task<InvoiceArchiveStorageResult> SaveAsync(
        Invoice invoice,
        InvoiceArchiveStorageArtifact artifact,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(artifact);

        if (invoice.Id != artifact.InvoiceId)
        {
            throw new InvalidOperationException("Archive artifact invoice id does not match the target invoice.");
        }

        if (string.IsNullOrWhiteSpace(artifact.Payload))
        {
            throw new InvalidOperationException("Archive artifact payload is required.");
        }

        var retentionYears = await GetRetentionYearsAsync(ct).ConfigureAwait(false);
        var retainUntilUtc = artifact.IssuedAtUtc.AddYears(retentionYears);
        var key = BuildIssuedSnapshotKey(invoice.Id, artifact.IssuedAtUtc);
        var hash = ComputeSha256(artifact.Payload);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(artifact.Payload));

        await _objectStorage.SaveAsync(
            new ObjectStorageWriteRequest(
                ResolveContainerName(),
                key,
                string.IsNullOrWhiteSpace(artifact.ContentType) ? "application/json" : artifact.ContentType.Trim(),
                BuildFileName(invoice.Id),
                stream,
                stream.Length,
                hash,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["invoice-id"] = invoice.Id.ToString("N"),
                    ["artifact-type"] = "issued-snapshot"
                },
                retainUntilUtc,
                ObjectRetentionMode.Compliance,
                LegalHold: false,
                ObjectOverwritePolicy.Disallow),
            ct).ConfigureAwait(false);

        invoice.IssuedAtUtc ??= artifact.IssuedAtUtc;
        invoice.IssuedSnapshotJson ??= artifact.Payload;
        invoice.IssuedSnapshotHashSha256 ??= hash;
        invoice.ArchiveGeneratedAtUtc ??= artifact.IssuedAtUtc;
        invoice.ArchiveRetainUntilUtc ??= retainUntilUtc;
        invoice.ArchiveRetentionPolicyVersion ??= $"{DefaultRetentionPolicyPrefix}:{retentionYears}y:{ProviderName}";

        return new InvoiceArchiveStorageResult(
            invoice.IssuedSnapshotHashSha256,
            invoice.ArchiveGeneratedAtUtc.Value,
            invoice.ArchiveRetainUntilUtc.Value,
            invoice.ArchiveRetentionPolicyVersion);
    }

    public async Task<InvoiceArchiveStorageArtifact?> ReadAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return null;
        }

        var invoice = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.Id == invoiceId && !x.IsDeleted && !x.ArchivePurgedAtUtc.HasValue)
            .Select(x => new
            {
                x.Id,
                x.IssuedAtUtc,
                x.IssuedSnapshotJson
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (invoice is null || !invoice.IssuedAtUtc.HasValue)
        {
            return null;
        }

        var key = BuildIssuedSnapshotKey(invoice.Id, invoice.IssuedAtUtc.Value);
        var stored = await _objectStorage.ReadAsync(
            new ObjectStorageObjectReference(ResolveContainerName(), key),
            ct).ConfigureAwait(false);

        if (stored is not null)
        {
            using var reader = new StreamReader(stored.Content, Encoding.UTF8, leaveOpen: false);
            var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(payload))
            {
                return new InvoiceArchiveStorageArtifact(
                    invoice.Id,
                    invoice.IssuedAtUtc.Value,
                    stored.ContentType,
                    stored.FileName ?? BuildFileName(invoice.Id),
                    payload);
            }
        }

        if (string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))
        {
            return null;
        }

        return new InvoiceArchiveStorageArtifact(
            invoice.Id,
            invoice.IssuedAtUtc.Value,
            "application/json",
            BuildFileName(invoice.Id),
            invoice.IssuedSnapshotJson);
    }

    public async Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return false;
        }

        var invoice = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.Id == invoiceId && !x.IsDeleted && !x.ArchivePurgedAtUtc.HasValue)
            .Select(x => new { x.Id, x.IssuedAtUtc, x.IssuedSnapshotJson })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (invoice is null || !invoice.IssuedAtUtc.HasValue)
        {
            return false;
        }

        var key = BuildIssuedSnapshotKey(invoice.Id, invoice.IssuedAtUtc.Value);
        return await _objectStorage.ExistsAsync(new ObjectStorageObjectReference(ResolveContainerName(), key), ct).ConfigureAwait(false) ||
               !string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson);
    }

    public async Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.IssuedAtUtc.HasValue)
        {
            await _objectStorage.DeleteAsync(
                new ObjectStorageDeleteRequest(
                    new ObjectStorageObjectReference(ResolveContainerName(), BuildIssuedSnapshotKey(invoice.Id, invoice.IssuedAtUtc.Value)),
                    string.IsNullOrWhiteSpace(reason) ? "Retention period elapsed" : reason.Trim()),
                ct).ConfigureAwait(false);
        }

        invoice.IssuedSnapshotJson = null;
        invoice.IssuedSnapshotHashSha256 = null;
        invoice.ArchivePurgedAtUtc = purgedAtUtc;
        invoice.ArchivePurgeReason = string.IsNullOrWhiteSpace(reason) ? "Retention period elapsed" : reason.Trim();
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

    private static string BuildIssuedSnapshotKey(Guid invoiceId, DateTime issuedAtUtc)
        => ObjectStorageKeyBuilder.ForInvoiceArchive(invoiceId, issuedAtUtc, "issued-snapshot", invoiceId);

    private static string BuildFileName(Guid invoiceId)
        => $"invoice-{invoiceId:N}-issued-snapshot.json";

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
