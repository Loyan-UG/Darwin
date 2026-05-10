using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Shipping.Dhl;

/// <summary>
/// Selects carrier-label storage without breaking the file-system fallback.
/// </summary>
public sealed class ShipmentLabelStorageRouter : IShipmentLabelStorage
{
    private const string ShipmentLabelsProfileName = "ShipmentLabels";
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ObjectStorageOptions> _options;

    public ShipmentLabelStorageRouter(IServiceProvider serviceProvider, IOptions<ObjectStorageOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<string> SaveLabelAsync(Guid shipmentId, string provider, byte[] content, string contentType, CancellationToken ct = default)
        => ActiveProvider.SaveLabelAsync(shipmentId, provider, content, contentType, ct);

    private IShipmentLabelStorage ActiveProvider
    {
        get
        {
            var objectStorageProvider = _options.Value.Profiles.TryGetValue(ShipmentLabelsProfileName, out var profile)
                ? profile.Provider
                : _options.Value.Provider;

            return objectStorageProvider == ObjectStorageProviderKind.S3Compatible ||
                objectStorageProvider == ObjectStorageProviderKind.AzureBlob ||
                objectStorageProvider == ObjectStorageProviderKind.FileSystem
                ? _serviceProvider.GetRequiredService<ObjectStorageShipmentLabelStorage>()
                : _serviceProvider.GetRequiredService<FileSystemShipmentLabelStorage>();
        }
    }
}
