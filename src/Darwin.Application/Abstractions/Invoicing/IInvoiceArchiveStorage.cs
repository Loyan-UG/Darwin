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
/// Provides a named implementation for issued invoice archive storage.
/// </summary>
public interface IInvoiceArchiveStorageProvider : IInvoiceArchiveStorage
{
    /// <summary>
    /// Gets the provider name used by the storage router.
    /// </summary>
    string ProviderName { get; }
}

/// <summary>
/// Known invoice archive storage provider names.
/// </summary>
public static class InvoiceArchiveStorageProviderNames
{
    public const string InternalDatabase = "InternalDatabase";
    public const string S3Compatible = "S3Compatible";
    public const string AzureBlob = "AzureBlob";
    public const string AwsS3 = "AwsS3";
    public const string Minio = "Minio";
    public const string FileSystem = "FileSystem";
}

/// <summary>
/// Selects the active invoice archive storage provider.
/// </summary>
public sealed class InvoiceArchiveStorageSelection
{
    /// <summary>
    /// Gets or sets the active provider name. Defaults to the internal/database fallback.
    /// </summary>
    public string ProviderName { get; set; } = InvoiceArchiveStorageProviderNames.InternalDatabase;

    /// <summary>
    /// Gets or sets the generic object-storage container used by external archive providers.
    /// </summary>
    public string? ObjectStorageContainerName { get; set; }
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
