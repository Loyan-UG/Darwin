using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Shipping.Dhl;

/// <summary>
/// Stores generated carrier labels through the generic object-storage boundary.
/// </summary>
public sealed class ObjectStorageShipmentLabelStorage : IShipmentLabelStorageProvider
{
    private const string ShipmentLabelsProfileName = "ShipmentLabels";
    private readonly IObjectStorageService _objectStorage;
    private readonly IOptions<ObjectStorageOptions> _options;

    public ObjectStorageShipmentLabelStorage(
        IObjectStorageService objectStorage,
        IOptions<ObjectStorageOptions> options)
    {
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ProviderName => ShipmentLabelStorageProviderNames.ObjectStorage;

    public async Task<string> SaveLabelAsync(Guid shipmentId, string provider, byte[] content, string contentType, CancellationToken ct = default)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment id is required.", nameof(shipmentId));
        }

        if (content is null || content.Length == 0)
        {
            throw new ArgumentException("Label content is required.", nameof(content));
        }

        var (profile, applyFallbackPrefix) = ResolveProfile();
        var normalizedProvider = ObjectStorageKeyBuilder.NormalizeSegment(
            string.IsNullOrWhiteSpace(provider) ? "carrier" : provider.Trim().ToLowerInvariant());
        var fileName = $"{normalizedProvider}-label-{shipmentId:N}.pdf";
        var objectKey = applyFallbackPrefix
            ? ObjectStorageKeyBuilder.Build(
                "shipments",
                normalizedProvider,
                shipmentId.ToString("N"),
                "labels",
                fileName)
            : ObjectStorageKeyBuilder.Build(
                normalizedProvider,
                shipmentId.ToString("N"),
                "labels",
                fileName);
        await using var stream = new MemoryStream(content, writable: false);

        var result = await _objectStorage.SaveAsync(
            new ObjectStorageWriteRequest(
                profile.ContainerName ?? string.Empty,
                objectKey,
                string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType.Trim(),
                fileName,
                stream,
                content.Length,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["shipment-id"] = shipmentId.ToString("N"),
                    ["carrier-provider"] = normalizedProvider
                },
                OverwritePolicy: ObjectOverwritePolicy.Disallow,
                ProfileName: ShipmentLabelsProfileName),
            ct).ConfigureAwait(false);

        if (result.StorageUri is not null)
        {
            return result.StorageUri.ToString();
        }

        var temporaryUrl = await _objectStorage.GetTemporaryReadUrlAsync(
            new ObjectStorageTemporaryUrlRequest(
                new ObjectStorageObjectReference(profile.ContainerName ?? string.Empty, objectKey, ProfileName: ShipmentLabelsProfileName),
                TimeSpan.FromMinutes(Math.Clamp(_options.Value.S3Compatible.PresignedUrlExpiryMinutes, 1, 1440))),
            ct).ConfigureAwait(false);

        return temporaryUrl?.ToString() ?? objectKey;
    }

    private (ObjectStorageProfileOptions Profile, bool ApplyFallbackPrefix) ResolveProfile()
    {
        if (!_options.Value.Profiles.TryGetValue(ShipmentLabelsProfileName, out var profile))
        {
            return (new ObjectStorageProfileOptions
            {
                Provider = _options.Value.Provider,
                ContainerName = _options.Value.S3Compatible.BucketName,
                Prefix = "shipments"
            }, true);
        }

        return (profile, false);
    }
}
