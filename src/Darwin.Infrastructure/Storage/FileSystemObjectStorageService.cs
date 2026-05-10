using System.Security.Cryptography;
using System.Text.Json;
using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class FileSystemObjectStorageService : IObjectStorageService
{
    private readonly FileSystemObjectStorageOptions _options;
    private readonly ObjectStorageCapabilityReporter _capabilities;

    public FileSystemObjectStorageService(IOptions<ObjectStorageOptions> options, ObjectStorageCapabilityReporter capabilities)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.FileSystem ?? throw new ArgumentNullException(nameof(options));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var container = ValidateContainer(request.ContainerName);
        var key = ValidateObjectKey(request.ObjectKey);
        var fullPath = ResolvePath(container, key);
        var metadataPath = GetMetadataPath(fullPath);

        if (request.OverwritePolicy == ObjectOverwritePolicy.Disallow && File.Exists(fullPath))
        {
            throw new InvalidOperationException("Object already exists and overwrite is disallowed.");
        }

        if (_options.PreventOverwrite && request.OverwritePolicy != ObjectOverwritePolicy.Allow && File.Exists(fullPath))
        {
            throw new InvalidOperationException("Object overwrite is blocked by file-system storage policy.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Object storage path could not be resolved.");
        }

        if (_options.CreateDirectories)
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture) + ".tmp";
        using var hasher = SHA256.Create();
        await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        await using (var crypto = new CryptoStream(target, hasher, CryptoStreamMode.Write, leaveOpen: true))
        {
            if (request.Content.CanSeek)
            {
                request.Content.Position = 0;
            }

            await request.Content.CopyToAsync(crypto, ct).ConfigureAwait(false);
        }

        var hash = Convert.ToHexString(hasher.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.ExpectedSha256Hash) &&
            !string.Equals(hash, request.ExpectedSha256Hash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Object content hash does not match the expected SHA-256 hash.");
        }

        File.Move(tempPath, fullPath, overwrite: request.OverwritePolicy == ObjectOverwritePolicy.Allow);

        var metadata = new FileSystemObjectMetadata(
            request.ContentType,
            request.FileName,
            hash,
            new FileInfo(fullPath).Length,
            DateTime.UtcNow,
            request.RetentionUntilUtc,
            request.RetentionMode,
            request.LegalHold,
            request.Metadata);
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata), ct).ConfigureAwait(false);

        return new ObjectStorageWriteResult(
            ObjectStorageProviderKind.FileSystem,
            container,
            key,
            VersionId: null,
            ETag: hash,
            Sha256Hash: hash,
            ContentLength: metadata.ContentLength,
            CreatedAtUtc: metadata.CreatedAtUtc,
            RetentionUntilUtc: metadata.RetentionUntilUtc,
            RetentionMode: metadata.RetentionMode,
            LegalHold: metadata.LegalHold,
            StorageUri: BuildStorageUri(container, key),
            IsImmutable: false,
            Metadata: request.Metadata);
    }

    public async Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var fullPath = ResolvePath(ValidateContainer(reference.ContainerName), ValidateObjectKey(reference.ObjectKey));
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var metadata = await ReadMetadataAsync(fullPath, ct).ConfigureAwait(false);
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (!string.IsNullOrWhiteSpace(metadata?.Sha256Hash))
        {
            var actualHash = await ComputeSha256Async(stream, ct).ConfigureAwait(false);
            if (!string.Equals(actualHash, metadata.Sha256Hash.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException("Object content hash does not match stored metadata.");
            }

            stream.Position = 0;
        }

        return new ObjectStorageReadResult(
            stream,
            metadata?.ContentType ?? "application/octet-stream",
            metadata?.FileName,
            metadata?.ContentLength ?? stream.Length,
            metadata?.Sha256Hash,
            metadata?.Metadata,
            metadata?.RetentionUntilUtc,
            metadata?.RetentionMode ?? ObjectRetentionMode.None,
            metadata?.LegalHold ?? false);
    }

    public Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var fullPath = ResolvePath(ValidateContainer(reference.ContainerName), ValidateObjectKey(reference.ObjectKey));
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var container = ValidateContainer(reference.ContainerName);
        var key = ValidateObjectKey(reference.ObjectKey);
        var fullPath = ResolvePath(container, key);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var metadata = await ReadMetadataAsync(fullPath, ct).ConfigureAwait(false);
        var fileInfo = new FileInfo(fullPath);
        return new ObjectStorageObjectMetadata(
            ObjectStorageProviderKind.FileSystem,
            container,
            key,
            VersionId: null,
            ETag: metadata?.Sha256Hash,
            ContentType: metadata?.ContentType,
            FileName: metadata?.FileName,
            ContentLength: metadata?.ContentLength ?? fileInfo.Length,
            Sha256Hash: metadata?.Sha256Hash,
            CreatedAtUtc: metadata?.CreatedAtUtc ?? fileInfo.CreationTimeUtc,
            RetentionUntilUtc: metadata?.RetentionUntilUtc,
            RetentionMode: metadata?.RetentionMode ?? ObjectRetentionMode.None,
            LegalHold: metadata?.LegalHold ?? false,
            IsImmutable: false,
            Metadata: metadata?.Metadata);
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

        if ((metadata.RetentionUntilUtc > DateTime.UtcNow || metadata.LegalHold) && !request.AllowLockedDelete)
        {
            throw new InvalidOperationException("Object is under application-level retention or legal hold and cannot be deleted by this request.");
        }

        var fullPath = ResolvePath(ValidateContainer(request.Reference.ContainerName), ValidateObjectKey(request.Reference.ObjectKey));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var metadataPath = GetMetadataPath(fullPath);
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }
    }

    public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(BuildStorageUri(request.Reference.ContainerName, request.Reference.ObjectKey));
    }

    public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
        => _capabilities.GetCapabilities(ObjectStorageProviderKind.FileSystem);

    private string ResolvePath(string container, string key)
    {
        if (string.IsNullOrWhiteSpace(_options.RootPath))
        {
            throw new InvalidOperationException("ObjectStorage:FileSystem:RootPath is required.");
        }

        var root = Path.GetFullPath(_options.RootPath);
        var path = Path.GetFullPath(Path.Combine(root, container, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Object storage path resolved outside the configured root.");
        }

        return path;
    }

    private Uri? BuildStorageUri(string container, string key)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return null;
        }

        var uri = _options.PublicBaseUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(container).Replace("%2F", "/", StringComparison.Ordinal) + "/" + Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.Ordinal);
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) ? result : null;
    }

    private static async Task<FileSystemObjectMetadata?> ReadMetadataAsync(string fullPath, CancellationToken ct)
    {
        var metadataPath = GetMetadataPath(fullPath);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileSystemObjectMetadata>(json);
    }

    private static async Task<string> ComputeSha256Async(Stream content, CancellationToken ct)
    {
        using var hasher = SHA256.Create();
        var hash = await hasher.ComputeHashAsync(content, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetMetadataPath(string fullPath) => fullPath + ".metadata.json";

    private static string ValidateContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Object storage container name is required.", nameof(containerName));
        }

        return ObjectStorageKeyBuilder.NormalizeSegment(containerName);
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

    private sealed record FileSystemObjectMetadata(
        string ContentType,
        string? FileName,
        string Sha256Hash,
        long ContentLength,
        DateTime CreatedAtUtc,
        DateTime? RetentionUntilUtc,
        ObjectRetentionMode RetentionMode,
        bool LegalHold,
        IReadOnlyDictionary<string, string>? Metadata);
}
