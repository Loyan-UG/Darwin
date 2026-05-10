using System.Security.Cryptography;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class AzureBlobObjectStorageService : IObjectStorageService
{
    private const string Sha256MetadataKey = "sha256";
    private const string FileNameMetadataKey = "file-name";
    private const string RetentionModeMetadataKey = "retention-mode";
    private const string RetentionUntilMetadataKey = "retention-until-utc";
    private const string LegalHoldMetadataKey = "legal-hold";

    private readonly BlobServiceClient _client;
    private readonly AzureBlobObjectStorageOptions _options;
    private readonly ObjectStorageCapabilityReporter _capabilities;
    private readonly SemaphoreSlim _containerValidationLock = new(1, 1);
    private readonly HashSet<string> _validatedImmutableContainers = new(StringComparer.Ordinal);

    public AzureBlobObjectStorageService(IOptions<ObjectStorageOptions> options, ObjectStorageCapabilityReporter capabilities)
        : this(CreateClient(options.Value.AzureBlob), options.Value.AzureBlob, capabilities)
    {
    }

    internal AzureBlobObjectStorageService(
        BlobServiceClient client,
        AzureBlobObjectStorageOptions options,
        ObjectStorageCapabilityReporter capabilities)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.RequireImmutabilityPolicy &&
            !request.RetentionUntilUtc.HasValue &&
            request.RetentionMode == ObjectRetentionMode.None &&
            !request.LegalHold)
        {
            throw new InvalidOperationException("Azure Blob immutable storage requires retention metadata or legal hold for this write.");
        }

        var containerName = ResolveContainer(request.ContainerName);
        var key = BuildBlobName(request.ObjectKey);
        var container = _client.GetBlobContainerClient(containerName);
        await EnsureContainerReadyForImmutableWriteAsync(container, ct).ConfigureAwait(false);
        var blob = container.GetBlobClient(key);
        var content = await BufferAndHashAsync(request.Content, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.ExpectedSha256Hash) &&
            !string.Equals(request.ExpectedSha256Hash.Trim(), content.HashSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Object content hash does not match the expected SHA-256 hash.");
        }

        var metadata = BuildMetadata(request, content.HashSha256);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType.Trim()
            },
            Metadata = metadata,
            Conditions = request.OverwritePolicy == ObjectOverwritePolicy.Disallow
                ? new BlobRequestConditions { IfNoneMatch = ETag.All }
                : null,
            ImmutabilityPolicy = BuildNativeImmutabilityPolicy(request),
            LegalHold = request.LegalHold
        };

        var response = await blob.UploadAsync(content.Stream, uploadOptions, ct).ConfigureAwait(false);
        var createdAtUtc = DateTime.UtcNow;

        return new ObjectStorageWriteResult(
            ObjectStorageProviderKind.AzureBlob,
            containerName,
            key,
            response.Value.VersionId,
            response.Value.ETag.ToString(),
            content.HashSha256,
            content.Length,
            createdAtUtc,
            request.RetentionUntilUtc,
            request.RetentionMode,
            request.LegalHold,
            BuildStorageUri(containerName, key) ?? blob.Uri,
            IsImmutableByPolicy(request.RetentionUntilUtc, request.RetentionMode, request.LegalHold),
            metadata);
    }

    public async Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var blob = ResolveBlob(reference);
        try
        {
            var download = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            var details = download.Value.Details;
            var metadata = details.Metadata;
            var retentionUntil = details.ImmutabilityPolicy?.ExpiresOn?.UtcDateTime ?? TryGetRetentionUntil(metadata);
            var retentionMode = ToRetentionMode(details.ImmutabilityPolicy) ?? TryGetRetentionMode(metadata);
            var legalHold = details.HasLegalHold || string.Equals(TryGetMetadata(metadata, LegalHoldMetadataKey), bool.TrueString, StringComparison.OrdinalIgnoreCase);
            return new ObjectStorageReadResult(
                download.Value.Content,
                details.ContentType ?? "application/octet-stream",
                TryGetMetadata(metadata, FileNameMetadataKey),
                details.ContentLength,
                TryGetMetadata(metadata, Sha256MetadataKey),
                new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase),
                retentionUntil,
                retentionMode,
                legalHold);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return await ResolveBlob(reference).ExistsAsync(ct).ConfigureAwait(false);
    }

    public async Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var blob = ResolveBlob(reference);
        try
        {
            var properties = await blob.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            var metadata = properties.Value.Metadata;
            var retentionUntil = properties.Value.ImmutabilityPolicy?.ExpiresOn?.UtcDateTime ?? TryGetRetentionUntil(metadata);
            var retentionMode = ToRetentionMode(properties.Value.ImmutabilityPolicy) ?? TryGetRetentionMode(metadata);
            var legalHold = properties.Value.HasLegalHold || string.Equals(TryGetMetadata(metadata, LegalHoldMetadataKey), bool.TrueString, StringComparison.OrdinalIgnoreCase);
            return new ObjectStorageObjectMetadata(
                ObjectStorageProviderKind.AzureBlob,
                ResolveContainer(reference.ContainerName),
                BuildBlobName(reference.ObjectKey),
                properties.Value.VersionId,
                properties.Value.ETag.ToString(),
                properties.Value.ContentType,
                TryGetMetadata(metadata, FileNameMetadataKey),
                properties.Value.ContentLength,
                TryGetMetadata(metadata, Sha256MetadataKey),
                properties.Value.CreatedOn.UtcDateTime,
                retentionUntil,
                retentionMode,
                legalHold,
                IsImmutableByPolicy(retentionUntil, retentionMode, legalHold),
                new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Object delete reason is required.");
        }

        var metadata = await GetMetadataAsync(request.Reference, ct).ConfigureAwait(false);
        if (metadata is null)
        {
            return;
        }

        if (metadata.IsImmutable && !request.AllowLockedDelete)
        {
            throw new InvalidOperationException("Object is under retention or legal hold and cannot be deleted by this request.");
        }

        await ResolveBlob(request.Reference).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct).ConfigureAwait(false);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Expiry <= TimeSpan.Zero || request.Expiry > TimeSpan.FromDays(1))
        {
            throw new InvalidOperationException("Temporary object URL expiry must be between one second and one day.");
        }

        var blob = ResolveBlob(request.Reference);
        if (blob.CanGenerateSasUri)
        {
            return Task.FromResult<Uri?>(blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(request.Expiry)));
        }

        return Task.FromResult(BuildStorageUri(request.Reference.ContainerName, request.Reference.ObjectKey));
    }

    public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
        => _capabilities.GetCapabilities(ObjectStorageProviderKind.AzureBlob);

    private async Task EnsureContainerReadyForImmutableWriteAsync(BlobContainerClient container, CancellationToken ct)
    {
        if (!_options.RequireImmutabilityPolicy ||
            _options.ImmutabilityValidationMode != ObjectStorageValidationMode.FailFast)
        {
            return;
        }

        if (_validatedImmutableContainers.Contains(container.Name))
        {
            return;
        }

        await _containerValidationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_validatedImmutableContainers.Contains(container.Name))
            {
                return;
            }

            var properties = await container.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            if (properties.Value.HasImmutabilityPolicy != true)
            {
                throw new InvalidOperationException("Azure Blob immutable storage requires a container immutability policy before immutable archive writes.");
            }

            _validatedImmutableContainers.Add(container.Name);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new InvalidOperationException("Azure Blob immutable storage container does not exist.", ex);
        }
        finally
        {
            _containerValidationLock.Release();
        }
    }

    private BlobClient ResolveBlob(ObjectStorageObjectReference reference)
    {
        var container = _client.GetBlobContainerClient(ResolveContainer(reference.ContainerName));
        var blob = container.GetBlobClient(BuildBlobName(reference.ObjectKey));
        return string.IsNullOrWhiteSpace(reference.VersionId) ? blob : blob.WithVersion(reference.VersionId);
    }

    private string ResolveContainer(string containerName)
    {
        var container = string.IsNullOrWhiteSpace(containerName) ? _options.ContainerName : containerName;
        if (string.IsNullOrWhiteSpace(container))
        {
            throw new InvalidOperationException("Azure Blob container name is not configured.");
        }

        return ObjectStorageKeyBuilder.NormalizeSegment(container);
    }

    private string BuildBlobName(string objectKey)
    {
        var key = ValidateObjectKey(objectKey);
        if (string.IsNullOrWhiteSpace(_options.BlobPrefix))
        {
            return key;
        }

        var prefix = NormalizePrefix(_options.BlobPrefix);
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
            throw new InvalidOperationException("Azure Blob prefix must contain at least one safe segment.");
        }

        return ObjectStorageKeyBuilder.Build(segments);
    }

    private static string ValidateObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("Object key is required.", nameof(objectKey));
        }

        var key = objectKey.Trim().Replace('\\', '/');
        if (key.StartsWith("/", StringComparison.Ordinal) ||
            key.EndsWith("/", StringComparison.Ordinal) ||
            key.Contains("//", StringComparison.Ordinal) ||
            key.Split('/').Any(segment => segment is "." or ".." || segment.Length == 0) ||
            key.Any(char.IsControl))
        {
            throw new ArgumentException("Object key is not a safe normalized storage key.", nameof(objectKey));
        }

        return key;
    }

    private Uri? BuildStorageUri(string container, string key)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return null;
        }

        _ = ResolveContainer(container);
        var uri = _options.PublicBaseUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(BuildBlobName(key)).Replace("%2F", "/", StringComparison.Ordinal);
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) ? result : null;
    }

    private static Dictionary<string, string> BuildMetadata(ObjectStorageWriteRequest request, string hash)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Sha256MetadataKey] = hash
        };

        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            metadata[FileNameMetadataKey] = Path.GetFileName(request.FileName.Trim());
        }

        if (request.RetentionUntilUtc.HasValue)
        {
            metadata[RetentionUntilMetadataKey] = request.RetentionUntilUtc.Value.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (request.RetentionMode != ObjectRetentionMode.None)
        {
            metadata[RetentionModeMetadataKey] = request.RetentionMode.ToString();
        }

        if (request.LegalHold)
        {
            metadata[LegalHoldMetadataKey] = bool.TrueString;
        }

        if (request.Metadata is null)
        {
            return metadata;
        }

        foreach (var item in request.Metadata)
        {
            if (!string.IsNullOrWhiteSpace(item.Key) && item.Value is not null)
            {
                metadata[ObjectStorageKeyBuilder.NormalizeSegment(item.Key)] = item.Value;
            }
        }

        return metadata;
    }

    private static async Task<BufferedContent> BufferAndHashAsync(Stream content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        var buffer = new MemoryStream();
        using var hasher = SHA256.Create();
        await using (var crypto = new CryptoStream(buffer, hasher, CryptoStreamMode.Write, leaveOpen: true))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await content.CopyToAsync(crypto, ct).ConfigureAwait(false);
        }

        buffer.Position = 0;
        return new BufferedContent(buffer, Convert.ToHexString(hasher.Hash ?? Array.Empty<byte>()).ToLowerInvariant(), buffer.Length);
    }

    private static DateTime? TryGetRetentionUntil(IDictionary<string, string> metadata)
    {
        var value = TryGetMetadata(metadata, RetentionUntilMetadataKey);
        return DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static ObjectRetentionMode TryGetRetentionMode(IDictionary<string, string> metadata)
    {
        var value = TryGetMetadata(metadata, RetentionModeMetadataKey);
        return Enum.TryParse<ObjectRetentionMode>(value, ignoreCase: true, out var parsed) ? parsed : ObjectRetentionMode.None;
    }

    private static string? TryGetMetadata(IDictionary<string, string> metadata, string key)
        => metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static BlobImmutabilityPolicy? BuildNativeImmutabilityPolicy(ObjectStorageWriteRequest request)
    {
        if (!request.RetentionUntilUtc.HasValue && request.RetentionMode == ObjectRetentionMode.None)
        {
            return null;
        }

        return new BlobImmutabilityPolicy
        {
            ExpiresOn = request.RetentionUntilUtc.HasValue
                ? new DateTimeOffset(request.RetentionUntilUtc.Value.ToUniversalTime())
                : DateTimeOffset.UtcNow.AddYears(1),
            PolicyMode = request.RetentionMode == ObjectRetentionMode.None
                ? BlobImmutabilityPolicyMode.Unlocked
                : BlobImmutabilityPolicyMode.Locked
        };
    }

    private static ObjectRetentionMode? ToRetentionMode(BlobImmutabilityPolicy? policy)
    {
        if (policy is null || policy.PolicyMode == BlobImmutabilityPolicyMode.Mutable)
        {
            return null;
        }

        return policy.PolicyMode == BlobImmutabilityPolicyMode.Locked
            ? ObjectRetentionMode.Compliance
            : ObjectRetentionMode.Governance;
    }

    private static bool IsImmutableByPolicy(DateTime? retentionUntilUtc, ObjectRetentionMode retentionMode, bool legalHold)
        => legalHold || retentionMode != ObjectRetentionMode.None || retentionUntilUtc > DateTime.UtcNow;

    private static BlobServiceClient CreateClient(AzureBlobObjectStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        if (options.UseManagedIdentity && !string.IsNullOrWhiteSpace(options.AccountName))
        {
            var uri = new Uri($"https://{options.AccountName.Trim()}.blob.core.windows.net");
            var credential = string.IsNullOrWhiteSpace(options.ClientId)
                ? new ManagedIdentityCredential(new ManagedIdentityCredentialOptions())
                : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(options.ClientId.Trim()));
            return new BlobServiceClient(uri, credential);
        }

        throw new InvalidOperationException("Azure Blob storage requires a connection string or managed identity account configuration.");
    }

    private sealed record BufferedContent(MemoryStream Stream, string HashSha256, long Length);
}
