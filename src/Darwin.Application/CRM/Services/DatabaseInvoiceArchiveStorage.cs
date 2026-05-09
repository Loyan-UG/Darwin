using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Services;

/// <summary>
/// Stores issued invoice archive artifacts on the invoice row.
/// </summary>
public sealed class DatabaseInvoiceArchiveStorage : IInvoiceArchiveStorage
{
    private const string DefaultRetentionPolicyPrefix = "invoice-archive-retention:v1";
    private readonly IAppDbContext _db;

    public DatabaseInvoiceArchiveStorage(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

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

        invoice.IssuedAtUtc ??= artifact.IssuedAtUtc;
        invoice.IssuedSnapshotJson ??= artifact.Payload;
        invoice.IssuedSnapshotHashSha256 ??= ComputeSha256(invoice.IssuedSnapshotJson);
        invoice.ArchiveGeneratedAtUtc ??= artifact.IssuedAtUtc;

        var retentionYears = await GetRetentionYearsAsync(ct).ConfigureAwait(false);
        invoice.ArchiveRetainUntilUtc ??= artifact.IssuedAtUtc.AddYears(retentionYears);
        invoice.ArchiveRetentionPolicyVersion ??= $"{DefaultRetentionPolicyPrefix}:{retentionYears}y";

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

        if (invoice is null ||
            !invoice.IssuedAtUtc.HasValue ||
            string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))
        {
            return null;
        }

        return new InvoiceArchiveStorageArtifact(
            invoice.Id,
            invoice.IssuedAtUtc.Value,
            "application/json",
            $"invoice-{invoice.Id:N}-issued-snapshot.json",
            invoice.IssuedSnapshotJson);
    }

    public Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return _db.Set<Invoice>()
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == invoiceId &&
                     !x.IsDeleted &&
                     !x.ArchivePurgedAtUtc.HasValue &&
                     !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson),
                ct);
    }

    public Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        invoice.IssuedSnapshotJson = null;
        invoice.IssuedSnapshotHashSha256 = null;
        invoice.ArchivePurgedAtUtc = purgedAtUtc;
        invoice.ArchivePurgeReason = string.IsNullOrWhiteSpace(reason) ? "Retention period elapsed" : reason.Trim();

        return Task.CompletedTask;
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

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
