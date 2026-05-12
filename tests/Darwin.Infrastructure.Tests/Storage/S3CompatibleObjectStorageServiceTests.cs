using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.Infrastructure.Tests.Storage;

public sealed class S3CompatibleObjectStorageServiceTests
{
    [Fact]
    public async Task SaveAsync_Should_FailFast_When_ObjectLock_Is_Required_And_Bucket_Versioning_Is_Not_Enabled()
    {
        var client = new Mock<IAmazonS3>(MockBehavior.Strict);
        client.Setup(x => x.GetBucketLocationAsync(
                It.Is<GetBucketLocationRequest>(r => r.BucketName == "archive"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetBucketLocationResponse());
        client.Setup(x => x.GetBucketVersioningAsync(
                It.Is<GetBucketVersioningRequest>(r => r.BucketName == "archive"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetBucketVersioningResponse
            {
                VersioningConfig = new S3BucketVersioningConfig
                {
                    Status = VersionStatus.Off
                }
            });

        var storage = CreateStorage(client.Object, new S3CompatibleObjectStorageOptions
        {
            BucketName = "archive",
            AccessKey = "configured-outside-git",
            SecretKey = "configured-outside-git",
            Region = "eu-central-1",
            RequireObjectLock = true,
            ObjectLockValidationMode = ObjectStorageValidationMode.FailFast
        });

        var save = async () => await storage.SaveAsync(CreateWriteRequest(), TestContext.Current.CancellationToken);

        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("S3-compatible object storage requires bucket versioning before immutable archive writes.");
        client.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_Should_Create_Missing_ObjectLock_Bucket_And_Enable_Versioning_When_Configured()
    {
        PutBucketRequest? putBucket = null;
        PutBucketVersioningRequest? putVersioning = null;
        PutObjectRequest? putObject = null;

        var client = new Mock<IAmazonS3>(MockBehavior.Strict);
        client.Setup(x => x.GetBucketLocationAsync(
                It.Is<GetBucketLocationRequest>(r => r.BucketName == "archive"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("missing")
            {
                StatusCode = HttpStatusCode.NotFound
            });
        client.Setup(x => x.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutBucketRequest, CancellationToken>((request, _) => putBucket = request)
            .ReturnsAsync(new PutBucketResponse());
        client.Setup(x => x.PutBucketVersioningAsync(It.IsAny<PutBucketVersioningRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutBucketVersioningRequest, CancellationToken>((request, _) => putVersioning = request)
            .ReturnsAsync(new PutBucketVersioningResponse());
        client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => putObject = request)
            .ReturnsAsync(new PutObjectResponse
            {
                VersionId = "version-1",
                ETag = "\"etag\""
            });

        var storage = CreateStorage(client.Object, new S3CompatibleObjectStorageOptions
        {
            BucketName = "archive",
            AccessKey = "configured-outside-git",
            SecretKey = "configured-outside-git",
            Region = "eu-central-1",
            CreateBucketIfMissing = true,
            RequireObjectLock = true,
            DefaultRetentionMode = ObjectRetentionMode.Compliance,
            ObjectLockValidationMode = ObjectStorageValidationMode.Warn
        });

        var result = await storage.SaveAsync(CreateWriteRequest(), TestContext.Current.CancellationToken);

        result.Provider.Should().Be(ObjectStorageProviderKind.S3Compatible);
        result.VersionId.Should().Be("version-1");
        putBucket.Should().NotBeNull();
        putBucket!.ObjectLockEnabledForBucket.Should().BeTrue();
        putVersioning.Should().NotBeNull();
        putVersioning!.VersioningConfig.Status.Should().Be(VersionStatus.Enabled);
        putObject.Should().NotBeNull();
        putObject!.BucketName.Should().Be("archive");
        putObject.ObjectLockMode.Should().Be(ObjectLockMode.Compliance);
        putObject.ObjectLockRetainUntilDate.Should().NotBeNull();
        putObject.Headers["If-None-Match"].Should().Be("*");
    }

    private static S3CompatibleObjectStorageService CreateStorage(IAmazonS3 client, S3CompatibleObjectStorageOptions s3Options)
    {
        var options = Options.Create(new ObjectStorageOptions
        {
            S3Compatible = s3Options
        });

        return new S3CompatibleObjectStorageService(
            client,
            s3Options,
            new ObjectStorageCapabilityReporter(options));
    }

    private static ObjectStorageWriteRequest CreateWriteRequest()
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes("{\"invoice\":\"test\"}"));
        return new ObjectStorageWriteRequest(
            ContainerName: "archive",
            ObjectKey: "invoices/2026/05/invoice.json",
            ContentType: "application/json",
            FileName: "invoice.json",
            Content: content,
            RetentionUntilUtc: DateTime.UtcNow.AddYears(10),
            RetentionMode: ObjectRetentionMode.Compliance,
            OverwritePolicy: ObjectOverwritePolicy.Disallow);
    }
}
