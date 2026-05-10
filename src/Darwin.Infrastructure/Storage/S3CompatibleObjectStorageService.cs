using System.Net;
using System.Security.Cryptography;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class S3CompatibleObjectStorageService : IObjectStorageService
{
    private const string Sha256MetadataKey = "sha256";
    private const string FileNameMetadataKey = "file-name";
    private const string RetentionModeMetadataKey = "retention-mode";
    private const string RetentionUntilMetadataKey = "retention-until-utc";
    private readonly IAmazonS3 _client;
    private readonly S3CompatibleObjectStorageOptions _options;
    private readonly ObjectStorageCapabilityReporter _capabilities;
    private readonly SemaphoreSlim _bucketValidationLock = new(1, 1);
    private readonly HashSet<string> _validatedBuckets = new(StringComparer.Ordinal);

    public S3CompatibleObjectStorageService(
        IOptions<ObjectStorageOptions> options,
        ObjectStorageCapabilityReporter capabilities)
        : this(CreateClient(options.Value.S3Compatible), options.Value.S3Compatible, capabilities)
    {
    }

    internal S3CompatibleObjectStorageService(
        IAmazonS3 client,
        S3CompatibleObjectStorageOptions options,
        ObjectStorageCapabilityReporter capabilities)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bucket = ResolveBucket(request.ContainerName);
        var key = ValidateObjectKey(request.ObjectKey);
        await EnsureBucketReadyForWriteAsync(bucket, ct).ConfigureAwait(false);
        var content = await BufferAndHashAsync(request.Content, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.ExpectedSha256Hash) &&
            !string.Equals(request.ExpectedSha256Hash.Trim(), content.HashSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Object content hash does not match the expected SHA-256 hash.");
        }

        var put = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content.Stream,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType.Trim(),
            AutoCloseStream = false
        };

        put.Metadata[Sha256MetadataKey] = content.HashSha256;
        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            put.Metadata[FileNameMetadataKey] = Path.GetFileName(request.FileName.Trim());
        }

        if (request.Metadata is not null)
        {
            foreach (var item in request.Metadata)
            {
                if (!string.IsNullOrWhiteSpace(item.Key) && item.Value is not null)
                {
                    put.Metadata[ObjectStorageKeyBuilder.NormalizeSegment(item.Key)] = item.Value;
                }
            }
        }

        ApplyRetention(put, request);
        ApplyServerSideEncryption(put);

        if (request.OverwritePolicy == ObjectOverwritePolicy.Disallow)
        {
            put.Headers["If-None-Match"] = "*";
        }

        var response = await _client.PutObjectAsync(put, ct).ConfigureAwait(false);
        var isImmutable = request.RetentionMode != ObjectRetentionMode.None || request.LegalHold;

        return new ObjectStorageWriteResult(
            ObjectStorageProviderKind.S3Compatible,
            bucket,
            key,
            response.VersionId,
            response.ETag,
            content.HashSha256,
            content.Length,
            DateTime.UtcNow,
            request.RetentionUntilUtc,
            request.RetentionMode,
            request.LegalHold,
            BuildStorageUri(bucket, key),
            isImmutable,
            request.Metadata);
    }

    public async Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var request = new GetObjectRequest
        {
            BucketName = ResolveBucket(reference.ContainerName),
            Key = ValidateObjectKey(reference.ObjectKey),
            VersionId = reference.VersionId
        };

        try
        {
            var response = await _client.GetObjectAsync(request, ct).ConfigureAwait(false);
            return new ObjectStorageReadResult(
                response.ResponseStream,
                response.Headers.ContentType ?? "application/octet-stream",
                TryGetMetadata(response.Metadata, FileNameMetadataKey),
                response.Headers.ContentLength,
                TryGetMetadata(response.Metadata, Sha256MetadataKey),
                ToDictionary(response.Metadata),
                response.ObjectLockRetainUntilDate,
                ToRetentionMode(response.ObjectLockMode),
                response.ObjectLockLegalHoldStatus == ObjectLockLegalHoldStatus.On);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        var metadata = await GetMetadataAsync(reference, ct).ConfigureAwait(false);
        return metadata is not null;
    }

    public async Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var request = new GetObjectMetadataRequest
        {
            BucketName = ResolveBucket(reference.ContainerName),
            Key = ValidateObjectKey(reference.ObjectKey),
            VersionId = reference.VersionId
        };

        try
        {
            var response = await _client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
            var retentionMode = ToRetentionMode(response.ObjectLockMode);
            var legalHold = response.ObjectLockLegalHoldStatus == ObjectLockLegalHoldStatus.On;
            return new ObjectStorageObjectMetadata(
                ObjectStorageProviderKind.S3Compatible,
                request.BucketName,
                request.Key,
                response.VersionId,
                response.ETag,
                response.Headers.ContentType,
                TryGetMetadata(response.Metadata, FileNameMetadataKey),
                response.Headers.ContentLength,
                TryGetMetadata(response.Metadata, Sha256MetadataKey),
                response.LastModified,
                response.ObjectLockRetainUntilDate,
                retentionMode,
                legalHold,
                retentionMode != ObjectRetentionMode.None || legalHold,
                ToDictionary(response.Metadata));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = ResolveBucket(request.Reference.ContainerName),
            Key = ValidateObjectKey(request.Reference.ObjectKey),
            VersionId = request.Reference.VersionId
        }, ct).ConfigureAwait(false);
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Expiry <= TimeSpan.Zero || request.Expiry > TimeSpan.FromDays(1))
        {
            throw new InvalidOperationException("Temporary object URL expiry must be between one second and one day.");
        }

        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = ResolveBucket(request.Reference.ContainerName),
            Key = ValidateObjectKey(request.Reference.ObjectKey),
            VersionId = request.Reference.VersionId,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(request.Expiry),
            Protocol = _options.UseSsl ? Protocol.HTTPS : Protocol.HTTP
        });

        return Task.FromResult<Uri?>(Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null);
    }

    public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
        => _capabilities.GetCapabilities(ObjectStorageProviderKind.S3Compatible);

    private static IAmazonS3 CreateClient(S3CompatibleObjectStorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle || options.UsePathStyle,
            UseHttp = !options.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            config.ServiceURL = options.Endpoint.Trim();
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region.Trim());
        }

        var credentials = new BasicAWSCredentials(options.AccessKey?.Trim(), options.SecretKey?.Trim());
        return new AmazonS3Client(credentials, config);
    }

    private string ResolveBucket(string containerName)
    {
        var bucket = string.IsNullOrWhiteSpace(containerName) ? _options.BucketName : containerName;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new InvalidOperationException("S3-compatible bucket name is not configured.");
        }

        return bucket.Trim();
    }

    private async Task EnsureBucketReadyForWriteAsync(string bucket, CancellationToken ct)
    {
        if (!_options.CreateBucketIfMissing &&
            (!_options.RequireObjectLock || _options.ObjectLockValidationMode == ObjectStorageValidationMode.Disabled))
        {
            return;
        }

        if (_validatedBuckets.Contains(bucket))
        {
            return;
        }

        await _bucketValidationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_validatedBuckets.Contains(bucket))
            {
                return;
            }

            await EnsureBucketExistsAsync(bucket, ct).ConfigureAwait(false);
            if (_options.RequireObjectLock &&
                _options.ObjectLockValidationMode == ObjectStorageValidationMode.FailFast)
            {
                await ValidateBucketObjectLockAsync(bucket, ct).ConfigureAwait(false);
            }

            _validatedBuckets.Add(bucket);
        }
        finally
        {
            _bucketValidationLock.Release();
        }
    }

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken ct)
    {
        try
        {
            _ = await _client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = bucket }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound && _options.CreateBucketIfMissing)
        {
            await _client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucket,
                ObjectLockEnabledForBucket = _options.RequireObjectLock
            }, ct).ConfigureAwait(false);

            if (_options.RequireObjectLock)
            {
                await _client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = bucket,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                }, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ValidateBucketObjectLockAsync(string bucket, CancellationToken ct)
    {
        var versioning = await _client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = bucket }, ct).ConfigureAwait(false);
        if (versioning.VersioningConfig?.Status != VersionStatus.Enabled)
        {
            throw new InvalidOperationException("S3-compatible object storage requires bucket versioning before immutable archive writes.");
        }

        try
        {
            var objectLock = await _client.GetObjectLockConfigurationAsync(new GetObjectLockConfigurationRequest { BucketName = bucket }, ct).ConfigureAwait(false);
            if (objectLock.ObjectLockConfiguration is null)
            {
                throw new InvalidOperationException("S3-compatible object storage requires Object Lock configuration before immutable archive writes.");
            }
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("S3-compatible object storage requires Object Lock configuration before immutable archive writes.", ex);
        }
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

    private void ApplyRetention(PutObjectRequest put, ObjectStorageWriteRequest request)
    {
        if (request.RetentionUntilUtc.HasValue)
        {
            put.Metadata[RetentionUntilMetadataKey] = request.RetentionUntilUtc.Value.ToUniversalTime().ToString("O");
            put.ObjectLockRetainUntilDate = request.RetentionUntilUtc.Value.ToUniversalTime();
        }

        if (request.RetentionMode != ObjectRetentionMode.None)
        {
            put.Metadata[RetentionModeMetadataKey] = request.RetentionMode.ToString();
            put.ObjectLockMode = request.RetentionMode == ObjectRetentionMode.Compliance
                ? ObjectLockMode.Compliance
                : ObjectLockMode.Governance;
        }

        if (request.LegalHold)
        {
            put.ObjectLockLegalHoldStatus = ObjectLockLegalHoldStatus.On;
        }
    }

    private void ApplyServerSideEncryption(PutObjectRequest put)
    {
        if (string.IsNullOrWhiteSpace(_options.ServerSideEncryption))
        {
            return;
        }

        put.ServerSideEncryptionMethod = _options.ServerSideEncryption.Trim().ToUpperInvariant() switch
        {
            "AES256" => ServerSideEncryptionMethod.AES256,
            "AWS:KMS" => ServerSideEncryptionMethod.AWSKMS,
            "AWS_KMS" => ServerSideEncryptionMethod.AWSKMS,
            _ => ServerSideEncryptionMethod.AES256
        };
    }

    private Uri? BuildStorageUri(string bucket, string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl) &&
            Uri.TryCreate(_options.PublicBaseUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.Ordinal), UriKind.Absolute, out var publicUri))
        {
            return publicUri;
        }

        return Uri.TryCreate($"s3://{bucket}/{key}", UriKind.Absolute, out var uri) ? uri : null;
    }

    private static ObjectRetentionMode ToRetentionMode(ObjectLockMode mode)
    {
        if (mode == ObjectLockMode.Compliance)
        {
            return ObjectRetentionMode.Compliance;
        }

        return mode == ObjectLockMode.Governance ? ObjectRetentionMode.Governance : ObjectRetentionMode.None;
    }

    private static string? TryGetMetadata(MetadataCollection metadata, string key)
    {
        var value = metadata[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyDictionary<string, string> ToDictionary(MetadataCollection metadata)
        => metadata.Keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToDictionary(key => key, key => metadata[key] ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    private sealed record BufferedContent(MemoryStream Stream, string HashSha256, long Length);
}
