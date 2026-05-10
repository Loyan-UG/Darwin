using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Storage;

public sealed class ObjectStorageOptionsValidator : IValidateOptions<ObjectStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, ObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (!Enum.IsDefined(options.Provider))
        {
            failures.Add("ObjectStorage:Provider must be Database, FileSystem, S3Compatible, or AzureBlob.");
        }

        if (!string.IsNullOrWhiteSpace(options.ActiveProfile) &&
            !options.Profiles.ContainsKey(options.ActiveProfile.Trim()))
        {
            failures.Add("ObjectStorage:ActiveProfile must reference a configured profile.");
        }

        foreach (var profile in options.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Key))
            {
                failures.Add("ObjectStorage:Profiles keys must not be empty.");
            }

            if (!Enum.IsDefined(profile.Value.Provider))
            {
                failures.Add($"ObjectStorage:Profiles:{profile.Key}:Provider is not supported.");
            }

            if (!string.IsNullOrWhiteSpace(profile.Value.ContainerName) &&
                !IsSafeObjectStorageSegment(profile.Value.ContainerName))
            {
                failures.Add($"ObjectStorage:Profiles:{profile.Key}:ContainerName must be a safe storage segment.");
            }

            if (!string.IsNullOrWhiteSpace(profile.Value.Prefix) &&
                !IsSafeObjectStoragePrefix(profile.Value.Prefix))
            {
                failures.Add($"ObjectStorage:Profiles:{profile.Key}:Prefix must be a safe normalized storage prefix.");
            }
        }

        if (options.Provider == ObjectStorageProviderKind.FileSystem ||
            options.Profiles.Values.Any(x => x.Provider == ObjectStorageProviderKind.FileSystem))
        {
            failures.AddRange(ValidateFileSystem(options.FileSystem, requireRootPath: true));
        }
        else
        {
            failures.AddRange(ValidateFileSystem(options.FileSystem, requireRootPath: false));
        }

        if (options.Provider == ObjectStorageProviderKind.S3Compatible ||
            options.Profiles.Values.Any(x => x.Provider == ObjectStorageProviderKind.S3Compatible))
        {
            failures.AddRange(ValidateS3Compatible(options.S3Compatible));
        }

        if (options.Provider == ObjectStorageProviderKind.AzureBlob ||
            options.Profiles.Values.Any(x => x.Provider == ObjectStorageProviderKind.AzureBlob))
        {
            failures.AddRange(ValidateAzureBlob(options.AzureBlob));
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<string> ValidateFileSystem(FileSystemObjectStorageOptions options, bool requireRootPath)
    {
        if (requireRootPath && string.IsNullOrWhiteSpace(options.RootPath))
        {
            yield return "ObjectStorage:FileSystem:RootPath is required when the file-system object storage provider is selected.";
        }

        if (options.DefaultRetentionYears is < 1 or > 100)
        {
            yield return "ObjectStorage:FileSystem:DefaultRetentionYears must be between 1 and 100.";
        }
    }

    private static IEnumerable<string> ValidateS3Compatible(S3CompatibleObjectStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            yield return "ObjectStorage:S3Compatible:BucketName is required.";
        }

        if (string.IsNullOrWhiteSpace(options.AccessKey))
        {
            yield return "ObjectStorage:S3Compatible:AccessKey is required and must come from secure configuration.";
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            yield return "ObjectStorage:S3Compatible:SecretKey is required and must come from secure configuration.";
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint) && string.IsNullOrWhiteSpace(options.Region))
        {
            yield return "ObjectStorage:S3Compatible must specify Endpoint for MinIO/S3-compatible storage or Region for AWS S3.";
        }

        if (options.RequireObjectLock && options.ObjectLockValidationMode == ObjectStorageValidationMode.Disabled)
        {
            yield return "ObjectStorage:S3Compatible:ObjectLockValidationMode must not be Disabled when RequireObjectLock is true.";
        }

        if (options.RequireObjectLock && options.DefaultRetentionMode == ObjectRetentionMode.None)
        {
            yield return "ObjectStorage:S3Compatible:DefaultRetentionMode must be Governance or Compliance when RequireObjectLock is true.";
        }

        if (options.DefaultRetentionYears is < 1 or > 100)
        {
            yield return "ObjectStorage:S3Compatible:DefaultRetentionYears must be between 1 and 100.";
        }

        if (options.PresignedUrlExpiryMinutes is < 1 or > 1440)
        {
            yield return "ObjectStorage:S3Compatible:PresignedUrlExpiryMinutes must be between 1 and 1440.";
        }
    }

    private static IEnumerable<string> ValidateAzureBlob(AzureBlobObjectStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            yield return "ObjectStorage:AzureBlob:ContainerName is required.";
        }

        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);
        var hasManagedIdentity = options.UseManagedIdentity && !string.IsNullOrWhiteSpace(options.AccountName);
        if (!hasConnectionString && !hasManagedIdentity)
        {
            yield return "ObjectStorage:AzureBlob requires ConnectionString or UseManagedIdentity with AccountName.";
        }

        if (options.RequireImmutabilityPolicy && options.ImmutabilityValidationMode == ObjectStorageValidationMode.Disabled)
        {
            yield return "ObjectStorage:AzureBlob:ImmutabilityValidationMode must not be Disabled when RequireImmutabilityPolicy is true.";
        }

        if (!string.IsNullOrWhiteSpace(options.BlobPrefix) &&
            !IsSafeObjectStoragePrefix(options.BlobPrefix))
        {
            yield return "ObjectStorage:AzureBlob:BlobPrefix must be a safe normalized storage prefix.";
        }

        if (options.DefaultRetentionYears is < 1 or > 100)
        {
            yield return "ObjectStorage:AzureBlob:DefaultRetentionYears must be between 1 and 100.";
        }

        if (options.PresignedUrlExpiryMinutes is < 1 or > 1440)
        {
            yield return "ObjectStorage:AzureBlob:PresignedUrlExpiryMinutes must be between 1 and 1440.";
        }
    }

    private static bool IsSafeObjectStorageSegment(string value)
    {
        try
        {
            _ = ObjectStorageKeyBuilder.NormalizeSegment(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsSafeObjectStoragePrefix(string value)
    {
        try
        {
            var segments = value
                .Trim()
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                return false;
            }

            _ = ObjectStorageKeyBuilder.Build(segments);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
