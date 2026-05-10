using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class ObjectStorageCapabilityReporter
{
    private readonly IOptions<ObjectStorageOptions> _options;

    public ObjectStorageCapabilityReporter(IOptions<ObjectStorageOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ObjectStorageCapabilities GetCapabilities(ObjectStorageProviderKind? providerKind = null)
    {
        var provider = providerKind ?? _options.Value.Provider;
        return provider switch
        {
            ObjectStorageProviderKind.Database => new ObjectStorageCapabilities(
                provider,
                SupportsVersioning: false,
                SupportsObjectLock: false,
                SupportsRetention: false,
                SupportsLegalHold: false,
                SupportsTemporaryUrls: false,
                SupportsServerSideEncryption: false,
                SupportsConditionalWrites: true,
                SupportsNativeImmutability: false),

            ObjectStorageProviderKind.FileSystem => new ObjectStorageCapabilities(
                provider,
                SupportsVersioning: false,
                SupportsObjectLock: false,
                SupportsRetention: false,
                SupportsLegalHold: false,
                SupportsTemporaryUrls: !string.IsNullOrWhiteSpace(_options.Value.FileSystem.PublicBaseUrl),
                SupportsServerSideEncryption: false,
                SupportsConditionalWrites: true,
                SupportsNativeImmutability: false),

            ObjectStorageProviderKind.S3Compatible => new ObjectStorageCapabilities(
                provider,
                SupportsVersioning: true,
                SupportsObjectLock: _options.Value.S3Compatible.RequireObjectLock,
                SupportsRetention: _options.Value.S3Compatible.RequireObjectLock,
                SupportsLegalHold: _options.Value.S3Compatible.LegalHoldEnabled,
                SupportsTemporaryUrls: true,
                SupportsServerSideEncryption: !string.IsNullOrWhiteSpace(_options.Value.S3Compatible.ServerSideEncryption),
                SupportsConditionalWrites: true,
                SupportsNativeImmutability: _options.Value.S3Compatible.RequireObjectLock),

            ObjectStorageProviderKind.AzureBlob => new ObjectStorageCapabilities(
                provider,
                SupportsVersioning: true,
                SupportsObjectLock: false,
                SupportsRetention: _options.Value.AzureBlob.RequireImmutabilityPolicy,
                SupportsLegalHold: _options.Value.AzureBlob.LegalHoldEnabled,
                SupportsTemporaryUrls: true,
                SupportsServerSideEncryption: true,
                SupportsConditionalWrites: true,
                SupportsNativeImmutability: _options.Value.AzureBlob.RequireImmutabilityPolicy),

            _ => throw new InvalidOperationException($"Unsupported object storage provider '{provider}'.")
        };
    }
}
