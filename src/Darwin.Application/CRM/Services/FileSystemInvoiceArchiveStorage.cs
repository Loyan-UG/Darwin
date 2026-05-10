using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Services;

/// <summary>
/// Stores issued invoice archive artifacts under a configured file-system root.
/// </summary>
public sealed class FileSystemInvoiceArchiveStorage : IInvoiceArchiveStorageProvider
{
    private const string DefaultRetentionPolicyPrefix = "invoice-archive-retention:v1";
    private const string DefaultContentType = "application/json";
    private readonly IAppDbContext _db;
    private readonly FileSystemInvoiceArchiveStorageOptions _options;

    public FileSystemInvoiceArchiveStorage(
        IAppDbContext db,
        FileSystemInvoiceArchiveStorageOptions options)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ProviderName => InvoiceArchiveStorageProviderNames.FileSystem;

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

        var root = GetRootPath();
        var path = GetArtifactPath(root, invoice.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, artifact.Payload, Encoding.UTF8, ct).ConfigureAwait(false);
        await WriteMetadataAsync(root, invoice.Id, artifact, ct).ConfigureAwait(false);

        invoice.IssuedAtUtc ??= artifact.IssuedAtUtc;
        invoice.IssuedSnapshotJson ??= artifact.Payload;
        invoice.IssuedSnapshotHashSha256 ??= ComputeSha256(artifact.Payload);
        invoice.ArchiveGeneratedAtUtc ??= artifact.IssuedAtUtc;

        var retentionYears = await GetRetentionYearsAsync(ct).ConfigureAwait(false);
        invoice.ArchiveRetainUntilUtc ??= artifact.IssuedAtUtc.AddYears(retentionYears);
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

        var path = GetArtifactPath(GetRootPath(), invoice.Id);
        string? payload = null;
        if (File.Exists(path))
        {
            payload = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        payload ??= invoice.IssuedSnapshotJson;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var metadata = await ReadMetadataAsync(GetRootPath(), invoice.Id, ct).ConfigureAwait(false);

        return new InvoiceArchiveStorageArtifact(
            invoice.Id,
            invoice.IssuedAtUtc.Value,
            metadata?.ContentType ?? DefaultContentType,
            metadata?.FileName ?? GetFileName(invoice.Id),
            payload);
    }

    public async Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return false;
        }

        var invoiceExists = await _db.Set<Invoice>()
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == invoiceId &&
                     !x.IsDeleted &&
                     !x.ArchivePurgedAtUtc.HasValue &&
                     (x.IssuedAtUtc.HasValue || !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson)),
                ct)
            .ConfigureAwait(false);

        if (!invoiceExists)
        {
            return false;
        }

        return File.Exists(GetArtifactPath(GetRootPath(), invoiceId)) ||
               await _db.Set<Invoice>()
                   .AsNoTracking()
                   .AnyAsync(x => x.Id == invoiceId && !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson), ct)
                   .ConfigureAwait(false);
    }

    public Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var path = GetArtifactPath(GetRootPath(), invoice.Id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var metadataPath = GetMetadataPath(GetRootPath(), invoice.Id);
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        invoice.IssuedSnapshotJson = null;
        invoice.IssuedSnapshotHashSha256 = null;
        invoice.ArchivePurgedAtUtc = purgedAtUtc;
        invoice.ArchivePurgeReason = string.IsNullOrWhiteSpace(reason) ? "Retention period elapsed" : reason.Trim();

        return Task.CompletedTask;
    }

    private string GetRootPath()
    {
        if (string.IsNullOrWhiteSpace(_options.RootPath))
        {
            throw new InvalidOperationException("Invoice archive file-system root path is not configured.");
        }

        return Path.GetFullPath(_options.RootPath.Trim());
    }

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

    private static string GetArtifactPath(string root, Guid invoiceId)
    {
        var id = invoiceId.ToString("N");
        return Path.Combine(root, "invoices", id[..2], id[2..4], GetFileName(invoiceId));
    }

    private static string GetMetadataPath(string root, Guid invoiceId)
    {
        var id = invoiceId.ToString("N");
        return Path.Combine(root, "invoices", id[..2], id[2..4], $"invoice-{id}-metadata.json");
    }

    private static string GetFileName(Guid invoiceId) =>
        $"invoice-{invoiceId:N}-issued-snapshot.json";

    private static async Task WriteMetadataAsync(
        string root,
        Guid invoiceId,
        InvoiceArchiveStorageArtifact artifact,
        CancellationToken ct)
    {
        var metadata = new StoredArchiveMetadata(
            string.IsNullOrWhiteSpace(artifact.ContentType) ? DefaultContentType : artifact.ContentType.Trim(),
            GetFileName(invoiceId));
        var payload = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(GetMetadataPath(root, invoiceId), payload, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    private static async Task<StoredArchiveMetadata?> ReadMetadataAsync(string root, Guid invoiceId, CancellationToken ct)
    {
        var path = GetMetadataPath(root, invoiceId);
        if (!File.Exists(path))
        {
            return null;
        }

        var payload = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<StoredArchiveMetadata>(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record StoredArchiveMetadata(string ContentType, string FileName);
}
