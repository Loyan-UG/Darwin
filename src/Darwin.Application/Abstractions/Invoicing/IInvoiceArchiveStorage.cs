using Darwin.Domain.Entities.CRM;

namespace Darwin.Application.Abstractions.Invoicing;

/// <summary>
/// Provides the storage boundary for issued invoice archive artifacts.
/// </summary>
public interface IInvoiceArchiveStorage
{
    /// <summary>
    /// Saves an issued invoice archive artifact and returns persisted archive metadata.
    /// </summary>
    Task<InvoiceArchiveStorageResult> SaveAsync(Invoice invoice, InvoiceArchiveStorageArtifact artifact, CancellationToken ct = default);

    /// <summary>
    /// Reads the issued invoice archive artifact for an invoice.
    /// </summary>
    Task<InvoiceArchiveStorageArtifact?> ReadAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a non-purged archive artifact exists for an invoice.
    /// </summary>
    Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Removes the archive payload after retention expires while preserving audit metadata.
    /// </summary>
    Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default);
}

/// <summary>
/// Represents a persisted invoice archive artifact payload.
/// </summary>
public sealed record InvoiceArchiveStorageArtifact(
    Guid InvoiceId,
    DateTime IssuedAtUtc,
    string ContentType,
    string FileName,
    string Payload);

/// <summary>
/// Represents metadata returned after saving an invoice archive artifact.
/// </summary>
public sealed record InvoiceArchiveStorageResult(
    string HashSha256,
    DateTime GeneratedAtUtc,
    DateTime RetainUntilUtc,
    string RetentionPolicyVersion);
