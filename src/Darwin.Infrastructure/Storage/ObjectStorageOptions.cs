using Darwin.Application.Abstractions.Storage;

namespace Darwin.Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public ObjectStorageProviderKind Provider { get; set; } = ObjectStorageProviderKind.Database;

    public string? ActiveProfile { get; set; }

    public Dictionary<string, ObjectStorageProfileOptions> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public S3CompatibleObjectStorageOptions S3Compatible { get; set; } = new();

    public AzureBlobObjectStorageOptions AzureBlob { get; set; } = new();

    public FileSystemObjectStorageOptions FileSystem { get; set; } = new();
}

public sealed class ObjectStorageProfileOptions
{
    public ObjectStorageProviderKind Provider { get; set; } = ObjectStorageProviderKind.Database;

    public string? ContainerName { get; set; }

    public string? Prefix { get; set; }
}

public sealed class S3CompatibleObjectStorageOptions
{
    public string? Endpoint { get; set; }

    public string? Region { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? BucketName { get; set; }

    public bool UseSsl { get; set; } = true;

    public bool UsePathStyle { get; set; } = true;

    public bool ForcePathStyle { get; set; } = true;

    public bool CreateBucketIfMissing { get; set; }

    public bool RequireObjectLock { get; set; }

    public int DefaultRetentionYears { get; set; } = 10;

    public ObjectRetentionMode DefaultRetentionMode { get; set; } = ObjectRetentionMode.Governance;

    public bool LegalHoldEnabled { get; set; }

    public string? ServerSideEncryption { get; set; }

    public string? PublicBaseUrl { get; set; }

    public int PresignedUrlExpiryMinutes { get; set; } = 15;

    public ObjectStorageValidationMode ObjectLockValidationMode { get; set; } = ObjectStorageValidationMode.Warn;
}

public sealed class AzureBlobObjectStorageOptions
{
    public string? ConnectionString { get; set; }

    public string? AccountName { get; set; }

    public string? ContainerName { get; set; }

    public string? BlobPrefix { get; set; }

    public bool UseManagedIdentity { get; set; }

    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

    public string? PublicBaseUrl { get; set; }

    public bool RequireImmutabilityPolicy { get; set; }

    public int DefaultRetentionYears { get; set; } = 10;

    public bool LegalHoldEnabled { get; set; }

    public int PresignedUrlExpiryMinutes { get; set; } = 15;

    public ObjectStorageValidationMode ImmutabilityValidationMode { get; set; } = ObjectStorageValidationMode.Warn;
}

public sealed class FileSystemObjectStorageOptions
{
    public string? RootPath { get; set; }

    public string? PublicBaseUrl { get; set; }

    public bool CreateDirectories { get; set; } = true;

    public bool PreventOverwrite { get; set; } = true;

    public int DefaultRetentionYears { get; set; } = 10;
}
