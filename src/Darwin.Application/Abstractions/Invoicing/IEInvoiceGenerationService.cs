using Darwin.Domain.Entities.CRM;

namespace Darwin.Application.Abstractions.Invoicing;

/// <summary>
/// Generates legally relevant e-invoice artifacts behind a provider-neutral boundary.
/// </summary>
public interface IEInvoiceGenerationService
{
    /// <summary>
    /// Generates an e-invoice artifact for an issued invoice snapshot when a compliant provider is configured.
    /// </summary>
    Task<EInvoiceGenerationResult> GenerateAsync(
        Invoice invoice,
        EInvoiceGenerationRequest request,
        CancellationToken ct = default);
}

public sealed record EInvoiceGenerationRequest(EInvoiceArtifactFormat Format);

public sealed record EInvoiceSourceReadinessResult(
    bool IsReady,
    IReadOnlyList<string> MissingFields)
{
    public static EInvoiceSourceReadinessResult Ready { get; } = new(true, Array.Empty<string>());
}

public sealed record EInvoiceGenerationResult(
    EInvoiceGenerationStatus Status,
    string Message,
    EInvoiceArtifact? Artifact = null,
    EInvoiceArtifactStorageResult? Storage = null)
{
    public bool IsGenerated => Status == EInvoiceGenerationStatus.Generated && Artifact is not null;
}

public sealed record EInvoiceArtifact(
    Guid InvoiceId,
    EInvoiceArtifactFormat Format,
    string ContentType,
    string FileName,
    byte[] Content,
    string ValidationProfile,
    DateTime GeneratedAtUtc);

public enum EInvoiceArtifactFormat
{
    ZugferdFacturX = 1,
    XRechnung = 2
}

public enum EInvoiceGenerationStatus
{
    Generated = 1,
    NotConfigured = 2,
    SourceSnapshotUnavailable = 3,
    UnsupportedFormat = 4,
    ValidationFailed = 5,
    InvoiceUnavailable = 6
}
