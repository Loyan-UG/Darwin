namespace Darwin.Application.Abstractions.Storage;

public enum ObjectStorageProviderKind
{
    Database = 0,
    FileSystem = 1,
    S3Compatible = 2,
    AzureBlob = 3
}

public enum ObjectRetentionMode
{
    None = 0,
    Governance = 1,
    Compliance = 2
}

public enum ObjectOverwritePolicy
{
    Disallow = 0,
    AllowOnlyIfNotLocked = 1,
    Allow = 2
}

public enum ObjectStorageValidationMode
{
    Disabled = 0,
    Warn = 1,
    FailFast = 2
}

public sealed record ObjectStorageContainerSelection(
    string ContainerName,
    ObjectStorageProviderKind? ProviderKind = null,
    string? ProfileName = null);

public sealed record ObjectStorageObjectReference(
    string ContainerName,
    string ObjectKey,
    string? VersionId = null,
    ObjectStorageProviderKind? ProviderKind = null,
    string? ProfileName = null);

public sealed record ObjectStorageWriteRequest(
    string ContainerName,
    string ObjectKey,
    string ContentType,
    string? FileName,
    Stream Content,
    long? ContentLength = null,
    string? ExpectedSha256Hash = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTime? RetentionUntilUtc = null,
    ObjectRetentionMode RetentionMode = ObjectRetentionMode.None,
    bool LegalHold = false,
    ObjectOverwritePolicy OverwritePolicy = ObjectOverwritePolicy.Disallow,
    ObjectStorageProviderKind? ProviderKind = null,
    string? ProfileName = null);

public sealed record ObjectStorageWriteResult(
    ObjectStorageProviderKind Provider,
    string ContainerName,
    string ObjectKey,
    string? VersionId,
    string? ETag,
    string Sha256Hash,
    long ContentLength,
    DateTime CreatedAtUtc,
    DateTime? RetentionUntilUtc,
    ObjectRetentionMode RetentionMode,
    bool LegalHold,
    Uri? StorageUri,
    bool IsImmutable,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ObjectStorageReadResult(
    Stream Content,
    string ContentType,
    string? FileName,
    long? ContentLength,
    string? Sha256Hash,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTime? RetentionUntilUtc = null,
    ObjectRetentionMode RetentionMode = ObjectRetentionMode.None,
    bool LegalHold = false);

public sealed record ObjectStorageObjectMetadata(
    ObjectStorageProviderKind Provider,
    string ContainerName,
    string ObjectKey,
    string? VersionId,
    string? ETag,
    string? ContentType,
    string? FileName,
    long? ContentLength,
    string? Sha256Hash,
    DateTime? CreatedAtUtc,
    DateTime? RetentionUntilUtc,
    ObjectRetentionMode RetentionMode,
    bool LegalHold,
    bool IsImmutable,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ObjectStorageDeleteRequest(
    ObjectStorageObjectReference Reference,
    string Reason,
    bool AllowLockedDelete = false);

public sealed record ObjectStorageTemporaryUrlRequest(
    ObjectStorageObjectReference Reference,
    TimeSpan Expiry);

public sealed record ObjectStorageCapabilities(
    ObjectStorageProviderKind Provider,
    bool SupportsVersioning,
    bool SupportsObjectLock,
    bool SupportsRetention,
    bool SupportsLegalHold,
    bool SupportsTemporaryUrls,
    bool SupportsServerSideEncryption,
    bool SupportsConditionalWrites,
    bool SupportsNativeImmutability);
