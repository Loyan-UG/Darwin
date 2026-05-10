namespace Darwin.Application.Abstractions.Invoicing;

/// <summary>
/// Stores generated e-invoice artifacts through a provider-neutral use-case boundary.
/// </summary>
public interface IEInvoiceArtifactStorage
{
    /// <summary>
    /// Saves a generated e-invoice artifact and returns persistence metadata.
    /// </summary>
    Task<EInvoiceArtifactStorageResult> SaveAsync(EInvoiceArtifact artifact, CancellationToken ct = default);
}

/// <summary>
/// Represents persisted e-invoice artifact metadata.
/// </summary>
public sealed record EInvoiceArtifactStorageResult(
    string Provider,
    string ContainerName,
    string ObjectKey,
    string? VersionId,
    string Sha256Hash,
    long ContentLength,
    DateTime CreatedAtUtc,
    DateTime? RetentionUntilUtc,
    bool IsImmutable);

/// <summary>
/// Skips artifact persistence when no compliant generator/storage pipeline is configured.
/// </summary>
public sealed class NullEInvoiceArtifactStorage : IEInvoiceArtifactStorage
{
    public Task<EInvoiceArtifactStorageResult> SaveAsync(EInvoiceArtifact artifact, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return Task.FromResult(new EInvoiceArtifactStorageResult(
            "None",
            string.Empty,
            string.Empty,
            null,
            string.Empty,
            artifact.Content.LongLength,
            artifact.GeneratedAtUtc,
            null,
            false));
    }
}
