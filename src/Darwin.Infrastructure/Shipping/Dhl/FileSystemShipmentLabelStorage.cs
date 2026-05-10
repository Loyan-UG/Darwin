using Darwin.Application.Abstractions.Shipping;
using Darwin.Infrastructure.Media;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Shipping.Dhl;

/// <summary>
/// Stores generated carrier labels in the shared media storage root.
/// </summary>
public sealed class FileSystemShipmentLabelStorage : IShipmentLabelStorageProvider
{
    private readonly IHostEnvironment _environment;
    private readonly IOptions<MediaStorageOptions> _options;

    public FileSystemShipmentLabelStorage(IHostEnvironment environment, IOptions<MediaStorageOptions> options)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ProviderName => ShipmentLabelStorageProviderNames.FileSystem;

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

        var options = _options.Value;
        var root = MediaStoragePathResolver.ResolveRootPath(_environment.ContentRootPath, options);
        Directory.CreateDirectory(root);

        var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? "carrier" : provider.Trim().ToLowerInvariant();
        var fileName = $"{normalizedProvider}-label-{shipmentId:N}.pdf";
        var filePath = Path.Combine(root, fileName);

        await File.WriteAllBytesAsync(filePath, content, ct).ConfigureAwait(false);
        return MediaStoragePathResolver.BuildPublicUrl(options, fileName);
    }
}
