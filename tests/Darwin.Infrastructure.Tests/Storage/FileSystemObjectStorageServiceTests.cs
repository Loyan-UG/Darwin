using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Tests.Storage;

public sealed class FileSystemObjectStorageServiceTests
{
    [Fact]
    public async Task SaveReadAndDelete_Should_Preserve_Hash_Metadata_And_Content()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_object_storage_{Guid.NewGuid():N}");
        try
        {
            var storage = CreateStorage(root, publicBaseUrl: "https://media.example.test/storage");
            var bytes = Encoding.UTF8.GetBytes("stored payload");
            var expectedHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            var write = await storage.SaveAsync(new ObjectStorageWriteRequest(
                "media",
                "cms/2026/05/example.txt",
                "text/plain",
                "example.txt",
                new MemoryStream(bytes),
                bytes.Length,
                expectedHash,
                new Dictionary<string, string> { ["source"] = "test" }),
                TestContext.Current.CancellationToken);

            write.Provider.Should().Be(ObjectStorageProviderKind.FileSystem);
            write.StorageUri.Should().Be(new Uri("https://media.example.test/storage/media/cms/2026/05/example.txt"));
            write.Sha256Hash.Should().Be(expectedHash);

            var read = await storage.ReadAsync(new ObjectStorageObjectReference("media", "cms/2026/05/example.txt"), TestContext.Current.CancellationToken);

            read.Should().NotBeNull();
            await using (var content = read!.Content)
            using (var reader = new StreamReader(content, Encoding.UTF8))
            {
                (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be("stored payload");
                read.Sha256Hash.Should().Be(expectedHash);
                read.Metadata.Should().ContainKey("source");
            }

            await storage.DeleteAsync(new ObjectStorageDeleteRequest(new ObjectStorageObjectReference("media", "cms/2026/05/example.txt"), "test cleanup"), TestContext.Current.CancellationToken);

            (await storage.ExistsAsync(new ObjectStorageObjectReference("media", "cms/2026/05/example.txt"), TestContext.Current.CancellationToken)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReadAsync_Should_Reject_File_Content_That_Does_Not_Match_Stored_Hash()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_object_storage_{Guid.NewGuid():N}");
        try
        {
            var storage = CreateStorage(root);
            await storage.SaveAsync(new ObjectStorageWriteRequest(
                "media",
                "cms/2026/05/tamper.txt",
                "text/plain",
                "tamper.txt",
                new MemoryStream(Encoding.UTF8.GetBytes("original"))),
                TestContext.Current.CancellationToken);

            var fullPath = Path.Combine(root, "media", "cms", "2026", "05", "tamper.txt");
            await File.WriteAllTextAsync(fullPath, "changed", TestContext.Current.CancellationToken);

            var read = async () => await storage.ReadAsync(
                new ObjectStorageObjectReference("media", "cms/2026/05/tamper.txt"),
                TestContext.Current.CancellationToken);

            await read.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Object content hash does not match stored metadata.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_Should_Reject_PathTraversal_And_HashMismatch()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_object_storage_{Guid.NewGuid():N}");
        try
        {
            var storage = CreateStorage(root);

            var traversal = async () => await storage.SaveAsync(new ObjectStorageWriteRequest(
                "media",
                "../escape.txt",
                "text/plain",
                "escape.txt",
                new MemoryStream(Encoding.UTF8.GetBytes("x"))),
                TestContext.Current.CancellationToken);

            await traversal.Should().ThrowAsync<ArgumentException>();

            var mismatch = async () => await storage.SaveAsync(new ObjectStorageWriteRequest(
                "media",
                "safe/file.txt",
                "text/plain",
                "file.txt",
                new MemoryStream(Encoding.UTF8.GetBytes("x")),
                ExpectedSha256Hash: "not-a-real-hash"),
                TestContext.Current.CancellationToken);

            await mismatch.Should().ThrowAsync<InvalidOperationException>();
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Block_Retained_Objects_Unless_Explicitly_Allowed()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_object_storage_{Guid.NewGuid():N}");
        try
        {
            var storage = CreateStorage(root);
            await storage.SaveAsync(new ObjectStorageWriteRequest(
                "archive",
                "invoices/2026/05/invoice.json",
                "application/json",
                "invoice.json",
                new MemoryStream(Encoding.UTF8.GetBytes("{}")),
                RetentionUntilUtc: DateTime.UtcNow.AddYears(1),
                RetentionMode: ObjectRetentionMode.Governance),
                TestContext.Current.CancellationToken);

            var delete = async () => await storage.DeleteAsync(new ObjectStorageDeleteRequest(
                new ObjectStorageObjectReference("archive", "invoices/2026/05/invoice.json"),
                "retention test"),
                TestContext.Current.CancellationToken);

            await delete.Should().ThrowAsync<InvalidOperationException>();

            await storage.DeleteAsync(new ObjectStorageDeleteRequest(
                new ObjectStorageObjectReference("archive", "invoices/2026/05/invoice.json"),
                "retention test override",
                AllowLockedDelete: true),
                TestContext.Current.CancellationToken);

            (await storage.ExistsAsync(new ObjectStorageObjectReference("archive", "invoices/2026/05/invoice.json"), TestContext.Current.CancellationToken)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static FileSystemObjectStorageService CreateStorage(string root, string? publicBaseUrl = null)
    {
        Directory.CreateDirectory(root);
        var options = Options.Create(new ObjectStorageOptions
        {
            FileSystem = new FileSystemObjectStorageOptions
            {
                RootPath = root,
                PublicBaseUrl = publicBaseUrl
            }
        });
        return new FileSystemObjectStorageService(options, new ObjectStorageCapabilityReporter(options));
    }
}
