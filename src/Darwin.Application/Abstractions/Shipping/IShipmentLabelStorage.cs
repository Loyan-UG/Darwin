namespace Darwin.Application.Abstractions.Shipping;

/// <summary>
/// Stores carrier label documents in a publicly reachable media/document location.
/// </summary>
public interface IShipmentLabelStorage
{
    Task<string> SaveLabelAsync(Guid shipmentId, string provider, byte[] content, string contentType, CancellationToken ct = default);
}

/// <summary>
/// Named carrier-label storage provider.
/// </summary>
public interface IShipmentLabelStorageProvider : IShipmentLabelStorage
{
    string ProviderName { get; }
}

public static class ShipmentLabelStorageProviderNames
{
    public const string FileSystem = "FileSystem";
    public const string ObjectStorage = "ObjectStorage";
}
