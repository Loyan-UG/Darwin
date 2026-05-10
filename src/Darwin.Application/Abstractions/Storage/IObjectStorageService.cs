namespace Darwin.Application.Abstractions.Storage;

/// <summary>
/// Provides provider-neutral object storage operations for application use cases.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Saves an object and returns provider-neutral persistence metadata.
    /// </summary>
    Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Reads an object payload and metadata.
    /// </summary>
    Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default);

    /// <summary>
    /// Checks whether an object exists.
    /// </summary>
    Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default);

    /// <summary>
    /// Reads object metadata without returning the payload stream.
    /// </summary>
    Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default);

    /// <summary>
    /// Deletes an object only when the caller and provider policy explicitly allow deletion.
    /// </summary>
    Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Creates a temporary read URL when the selected provider supports it.
    /// </summary>
    Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets provider capabilities for the selected storage container.
    /// </summary>
    ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection);
}
