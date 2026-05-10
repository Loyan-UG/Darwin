using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class ObjectStorageServiceRouter : IObjectStorageService
{
    private readonly ObjectStorageOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ObjectStorageCapabilityReporter _capabilities;

    public ObjectStorageServiceRouter(
        IOptions<ObjectStorageOptions> options,
        IServiceProvider serviceProvider,
        ObjectStorageCapabilityReporter capabilities)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(request.ProviderKind, request.ProfileName);
        return ResolveProvider(resolved.ProviderKind).SaveAsync(
            request with
            {
                ContainerName = ResolveContainerName(request.ContainerName, resolved.Profile),
                ObjectKey = ResolveObjectKey(request.ObjectKey, resolved.Profile),
                ProviderKind = resolved.ProviderKind
            },
            ct);
    }

    public Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(reference.ProviderKind, reference.ProfileName);
        return ResolveProvider(resolved.ProviderKind).ReadAsync(
            reference with
            {
                ContainerName = ResolveContainerName(reference.ContainerName, resolved.Profile),
                ObjectKey = ResolveObjectKey(reference.ObjectKey, resolved.Profile),
                ProviderKind = resolved.ProviderKind
            },
            ct);
    }

    public Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(reference.ProviderKind, reference.ProfileName);
        return ResolveProvider(resolved.ProviderKind).ExistsAsync(
            reference with
            {
                ContainerName = ResolveContainerName(reference.ContainerName, resolved.Profile),
                ObjectKey = ResolveObjectKey(reference.ObjectKey, resolved.Profile),
                ProviderKind = resolved.ProviderKind
            },
            ct);
    }

    public Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(reference.ProviderKind, reference.ProfileName);
        return ResolveProvider(resolved.ProviderKind).GetMetadataAsync(
            reference with
            {
                ContainerName = ResolveContainerName(reference.ContainerName, resolved.Profile),
                ObjectKey = ResolveObjectKey(reference.ObjectKey, resolved.Profile),
                ProviderKind = resolved.ProviderKind
            },
            ct);
    }

    public Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(request.Reference.ProviderKind, request.Reference.ProfileName);
        var reference = request.Reference with
        {
            ContainerName = ResolveContainerName(request.Reference.ContainerName, resolved.Profile),
            ObjectKey = ResolveObjectKey(request.Reference.ObjectKey, resolved.Profile),
            ProviderKind = resolved.ProviderKind
        };
        return ResolveProvider(resolved.ProviderKind).DeleteAsync(request with { Reference = reference }, ct);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
    {
        var resolved = ResolveSelection(request.Reference.ProviderKind, request.Reference.ProfileName);
        var reference = request.Reference with
        {
            ContainerName = ResolveContainerName(request.Reference.ContainerName, resolved.Profile),
            ObjectKey = ResolveObjectKey(request.Reference.ObjectKey, resolved.Profile),
            ProviderKind = resolved.ProviderKind
        };
        return ResolveProvider(resolved.ProviderKind).GetTemporaryReadUrlAsync(request with { Reference = reference }, ct);
    }

    public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
        => _capabilities.GetCapabilities(ResolveProviderKind(selection.ProviderKind, selection.ProfileName));

    private IObjectStorageService ResolveProvider(ObjectStorageProviderKind providerKind)
    {
        return providerKind switch
        {
            ObjectStorageProviderKind.S3Compatible => _serviceProvider.GetRequiredService<S3CompatibleObjectStorageService>(),
            ObjectStorageProviderKind.FileSystem => _serviceProvider.GetRequiredService<FileSystemObjectStorageService>(),
            ObjectStorageProviderKind.Database => throw new InvalidOperationException("Generic object storage does not implement the database/internal provider. Use the use-case-specific internal fallback provider."),
            ObjectStorageProviderKind.AzureBlob => _serviceProvider.GetRequiredService<AzureBlobObjectStorageService>(),
            _ => throw new InvalidOperationException("Configured object storage provider is not supported.")
        };
    }

    private ObjectStorageProviderKind ResolveProviderKind(ObjectStorageProviderKind? providerKind, string? profileName)
        => ResolveSelection(providerKind, profileName).ProviderKind;

    private ResolvedObjectStorageSelection ResolveSelection(ObjectStorageProviderKind? providerKind, string? profileName)
    {
        if (providerKind.HasValue)
        {
            return new ResolvedObjectStorageSelection(providerKind.Value, null);
        }

        if (!string.IsNullOrWhiteSpace(profileName) &&
            _options.Profiles.TryGetValue(profileName.Trim(), out var profile))
        {
            return new ResolvedObjectStorageSelection(profile.Provider, profile);
        }

        if (!string.IsNullOrWhiteSpace(_options.ActiveProfile) &&
            _options.Profiles.TryGetValue(_options.ActiveProfile.Trim(), out var activeProfile))
        {
            return new ResolvedObjectStorageSelection(activeProfile.Provider, activeProfile);
        }

        return new ResolvedObjectStorageSelection(_options.Provider, null);
    }

    private static string ResolveContainerName(string containerName, ObjectStorageProfileOptions? profile)
    {
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            return containerName;
        }

        return string.IsNullOrWhiteSpace(profile?.ContainerName) ? containerName : profile.ContainerName.Trim();
    }

    private static string ResolveObjectKey(string objectKey, ObjectStorageProfileOptions? profile)
    {
        if (string.IsNullOrWhiteSpace(profile?.Prefix))
        {
            return objectKey;
        }

        var prefix = NormalizePrefix(profile.Prefix);
        var key = objectKey.Trim().Replace('\\', '/');
        return key.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            ? key
            : prefix + "/" + key;
    }

    private static string NormalizePrefix(string prefix)
    {
        var segments = prefix
            .Trim()
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Object storage profile prefix must contain at least one safe segment.");
        }

        return ObjectStorageKeyBuilder.Build(segments);
    }

    private sealed record ResolvedObjectStorageSelection(
        ObjectStorageProviderKind ProviderKind,
        ObjectStorageProfileOptions? Profile);
}
