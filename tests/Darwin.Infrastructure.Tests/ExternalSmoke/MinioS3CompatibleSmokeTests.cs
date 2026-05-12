using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace Darwin.Infrastructure.Tests.ExternalSmoke;

/// <summary>
/// Optional integration smoke tests for the S3-compatible provider against a real local MinIO instance.
/// These tests are intentionally environment-gated so normal CI does not require Docker, MinIO, or local credentials.
/// Retained smoke objects are not deleted because the local bucket is intentionally created with default COMPLIANCE retention.
/// </summary>
public sealed class MinioS3CompatibleSmokeTests
{
    private const string RunMinioSmokeVariable = "DARWIN_RUN_MINIO_SMOKE";

    [Fact]
    public async Task MinioSmoke_Should_Write_Read_Verify_Metadata_Block_Overwrite_And_Report_Capabilities()
    {
        var options = ReadOptionsOrSkip();
        var storage = CreateStorage(options);
        var objectKey = ObjectStorageKeyBuilder.Build("smoke", DateTime.UtcNow.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N"), "object.json");
        var bytes = Encoding.UTF8.GetBytes("{\"smoke\":\"minio\"}");
        var expectedHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var ct = TestContext.Current.CancellationToken;

        var write = await storage.SaveAsync(
            new ObjectStorageWriteRequest(
                ContainerName: options.BucketName!,
                ObjectKey: objectKey,
                ContentType: "application/json",
                FileName: "object.json",
                Content: new MemoryStream(bytes),
                ContentLength: bytes.Length,
                ExpectedSha256Hash: expectedHash,
                Metadata: new Dictionary<string, string>
                {
                    ["smoke-kind"] = "minio-local"
                }),
            ct);

        write.Provider.Should().Be(ObjectStorageProviderKind.S3Compatible);
        write.ContainerName.Should().Be(options.BucketName);
        write.ObjectKey.Should().Be(objectKey);
        write.Sha256Hash.Should().Be(expectedHash);
        write.VersionId.Should().NotBeNullOrWhiteSpace("local MinIO smoke bucket should have versioning enabled");

        var capabilities = storage.GetCapabilities(new ObjectStorageContainerSelection(options.BucketName!));
        capabilities.Provider.Should().Be(ObjectStorageProviderKind.S3Compatible);
        capabilities.SupportsVersioning.Should().BeTrue();
        capabilities.SupportsObjectLock.Should().BeTrue();
        capabilities.SupportsNativeImmutability.Should().BeTrue();
        capabilities.SupportsTemporaryUrls.Should().BeTrue();

        var metadata = await storage.GetMetadataAsync(new ObjectStorageObjectReference(options.BucketName!, objectKey), ct);
        metadata.Should().NotBeNull();
        metadata!.Sha256Hash.Should().Be(expectedHash);
        metadata.Metadata.Should().NotBeNull();
        metadata.Metadata!.Keys.Should().Contain(
            key => string.Equals(key, "smoke-kind", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "x-amz-meta-smoke-kind", StringComparison.OrdinalIgnoreCase));

        var read = await storage.ReadAsync(new ObjectStorageObjectReference(options.BucketName!, objectKey), ct);
        read.Should().NotBeNull();
        await using (read!.Content)
        using (var reader = new StreamReader(read.Content, Encoding.UTF8))
        {
            var payload = await reader.ReadToEndAsync(ct);
            payload.Should().Be("{\"smoke\":\"minio\"}");
        }

        var temporaryUrl = await storage.GetTemporaryReadUrlAsync(
            new ObjectStorageTemporaryUrlRequest(
                new ObjectStorageObjectReference(options.BucketName!, objectKey),
                TimeSpan.FromMinutes(5)),
            ct);
        temporaryUrl.Should().NotBeNull();

        var overwrite = async () => await storage.SaveAsync(
            new ObjectStorageWriteRequest(
                ContainerName: options.BucketName!,
                ObjectKey: objectKey,
                ContentType: "application/json",
                FileName: "object.json",
                Content: new MemoryStream(bytes),
                OverwritePolicy: ObjectOverwritePolicy.Disallow),
            ct);

        await overwrite.Should().ThrowAsync<Exception>("S3-compatible conditional writes should reject duplicate object keys");
    }

    [Fact]
    public async Task MinioSmoke_Should_Block_Delete_For_Retained_Object_At_Application_Boundary()
    {
        var options = ReadOptionsOrSkip();
        var storage = CreateStorage(options);
        var objectKey = ObjectStorageKeyBuilder.Build("smoke-retained", DateTime.UtcNow.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N"), "retained.json");
        var ct = TestContext.Current.CancellationToken;

        await storage.SaveAsync(
            new ObjectStorageWriteRequest(
                ContainerName: options.BucketName!,
                ObjectKey: objectKey,
                ContentType: "application/json",
                FileName: "retained.json",
                Content: new MemoryStream("{\"retained\":true}"u8.ToArray()),
                RetentionUntilUtc: DateTime.UtcNow.AddDays(1),
                RetentionMode: ObjectRetentionMode.Compliance,
                OverwritePolicy: ObjectOverwritePolicy.Disallow),
            ct);

        var delete = async () => await storage.DeleteAsync(
            new ObjectStorageDeleteRequest(
                new ObjectStorageObjectReference(options.BucketName!, objectKey),
                "MinIO retained smoke delete should be blocked"),
            ct);

        await delete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Object is under retention or legal hold and cannot be deleted by this request.");
    }

    private static S3CompatibleObjectStorageService CreateStorage(S3CompatibleObjectStorageOptions s3Options)
    {
        var options = Options.Create(new ObjectStorageOptions
        {
            Provider = ObjectStorageProviderKind.S3Compatible,
            S3Compatible = s3Options
        });

        return new S3CompatibleObjectStorageService(
            options,
            new ObjectStorageCapabilityReporter(options));
    }

    private static S3CompatibleObjectStorageOptions ReadOptionsOrSkip()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RunMinioSmokeVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw SkipException.ForSkip(
                "Local MinIO smoke is optional. Set DARWIN_RUN_MINIO_SMOKE=true and configure DARWIN_MINIO_* variables to run it.");
        }

        var endpoint = ReadRequiredEnvironment("DARWIN_MINIO_ENDPOINT");
        var accessKey = ReadRequiredEnvironment("DARWIN_MINIO_ACCESS_KEY");
        var secretKey = ReadRequiredEnvironment("DARWIN_MINIO_SECRET_KEY");
        var bucket = ReadRequiredEnvironment("DARWIN_MINIO_BUCKET");
        var region = Environment.GetEnvironmentVariable("DARWIN_MINIO_REGION");

        return new S3CompatibleObjectStorageOptions
        {
            Endpoint = endpoint,
            Region = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region.Trim(),
            AccessKey = accessKey,
            SecretKey = secretKey,
            BucketName = bucket,
            UseSsl = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            UsePathStyle = true,
            ForcePathStyle = true,
            RequireObjectLock = true,
            DefaultRetentionMode = ObjectRetentionMode.Compliance,
            LegalHoldEnabled = true,
            ObjectLockValidationMode = ObjectStorageValidationMode.FailFast
        };
    }

    private static string ReadRequiredEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw SkipException.ForSkip($"Local MinIO smoke is optional. Configure {name} before running it.");
        }

        return value.Trim();
    }
}
