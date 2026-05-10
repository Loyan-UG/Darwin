using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Storage;
using FluentAssertions;

namespace Darwin.Infrastructure.Tests.Storage;

public sealed class ObjectStorageOptionsValidatorTests
{
    [Fact]
    public void Validate_Should_Accept_Database_Default()
    {
        var result = new ObjectStorageOptionsValidator().Validate(null, new ObjectStorageOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Require_S3_Settings_When_S3Compatible_Is_Selected()
    {
        var options = new ObjectStorageOptions
        {
            Provider = ObjectStorageProviderKind.S3Compatible,
            S3Compatible = new S3CompatibleObjectStorageOptions
            {
                RequireObjectLock = true,
                ObjectLockValidationMode = ObjectStorageValidationMode.Disabled
            }
        };

        var result = new ObjectStorageOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(x => x.Contains("BucketName", StringComparison.Ordinal));
        result.Failures.Should().Contain(x => x.Contains("AccessKey", StringComparison.Ordinal));
        result.Failures.Should().Contain(x => x.Contains("SecretKey", StringComparison.Ordinal));
        result.Failures.Should().Contain(x => x.Contains("ObjectLockValidationMode", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Should_Accept_Minio_Style_S3_Settings_With_ObjectLock_FailFast()
    {
        var options = new ObjectStorageOptions
        {
            Provider = ObjectStorageProviderKind.S3Compatible,
            S3Compatible = new S3CompatibleObjectStorageOptions
            {
                Endpoint = "https://minio.example.internal",
                Region = "eu-central-1",
                AccessKey = "configured-outside-git",
                SecretKey = "configured-outside-git",
                BucketName = "darwin-invoice-archive",
                UsePathStyle = true,
                ForcePathStyle = true,
                RequireObjectLock = true,
                DefaultRetentionMode = ObjectRetentionMode.Compliance,
                ObjectLockValidationMode = ObjectStorageValidationMode.FailFast
            }
        };

        var result = new ObjectStorageOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Accept_Aws_Style_S3_Settings_With_Region()
    {
        var options = new ObjectStorageOptions
        {
            Provider = ObjectStorageProviderKind.S3Compatible,
            S3Compatible = new S3CompatibleObjectStorageOptions
            {
                Region = "eu-central-1",
                AccessKey = "configured-outside-git",
                SecretKey = "configured-outside-git",
                BucketName = "darwin-invoice-archive"
            }
        };

        var result = new ObjectStorageOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Require_Azure_Container_And_Credential_Boundary()
    {
        var options = new ObjectStorageOptions
        {
            Provider = ObjectStorageProviderKind.AzureBlob,
            AzureBlob = new AzureBlobObjectStorageOptions
            {
                RequireImmutabilityPolicy = true,
                ImmutabilityValidationMode = ObjectStorageValidationMode.Disabled
            }
        };

        var result = new ObjectStorageOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(x => x.Contains("ContainerName", StringComparison.Ordinal));
        result.Failures.Should().Contain(x => x.Contains("ConnectionString", StringComparison.Ordinal));
        result.Failures.Should().Contain(x => x.Contains("ImmutabilityValidationMode", StringComparison.Ordinal));
    }

    [Fact]
    public void CapabilityReporter_Should_Not_Claim_Native_Immutability_For_Database_Or_FileSystem()
    {
        var reporter = new ObjectStorageCapabilityReporter(
            Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions()));

        reporter.GetCapabilities(ObjectStorageProviderKind.Database).SupportsNativeImmutability.Should().BeFalse();
        reporter.GetCapabilities(ObjectStorageProviderKind.FileSystem).SupportsNativeImmutability.Should().BeFalse();
    }

    [Fact]
    public void CapabilityReporter_Should_Report_Native_Immutability_Only_When_ObjectLock_Is_Required_For_S3()
    {
        var reporter = new ObjectStorageCapabilityReporter(
            Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions
            {
                S3Compatible = new S3CompatibleObjectStorageOptions
                {
                    RequireObjectLock = true,
                    LegalHoldEnabled = true,
                    ServerSideEncryption = "AES256"
                }
            }));

        var capabilities = reporter.GetCapabilities(ObjectStorageProviderKind.S3Compatible);

        capabilities.SupportsNativeImmutability.Should().BeTrue();
        capabilities.SupportsLegalHold.Should().BeTrue();
        capabilities.SupportsServerSideEncryption.Should().BeTrue();
    }
}
