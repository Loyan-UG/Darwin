using System.Security.Cryptography;
using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Media;

public sealed record StoredImageUpload(string PublicUrl, string FileName);

public interface IUploadedImageStorageService
{
    Task<StoredImageUpload> SaveAsync(Stream stream, string fileName, string? contentType, CancellationToken ct = default);
}

public sealed class UploadedImageStorageService : IUploadedImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IHostEnvironment _environment;
    private readonly MediaStorageOptions _options;

    public UploadedImageStorageService(IHostEnvironment environment, IOptions<MediaStorageOptions> options)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<StoredImageUpload> SaveAsync(Stream stream, string fileName, string? contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            extension = ExtensionFromContentType(contentType);
        }

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidDataException("Unsupported image file type.");
        }

        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var uploadsRoot = MediaStoragePathResolver.ResolveRootPath(_environment.ContentRootPath, _options);
        Directory.CreateDirectory(uploadsRoot);
        var path = Path.Combine(uploadsRoot, storedName);

        await using var target = File.Create(path);
        await stream.CopyToAsync(target, ct).ConfigureAwait(false);

        return new StoredImageUpload(MediaStoragePathResolver.BuildPublicUrl(_options, storedName), storedName);
    }

    private static string? ExtensionFromContentType(string? contentType)
    {
        return contentType?.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => null
        };
    }
}
