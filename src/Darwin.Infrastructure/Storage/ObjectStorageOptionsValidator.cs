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
        }

        failures.AddRange(ValidateFileSystem(options.FileSystem));

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

    private static IEnumerable<string> ValidateFileSystem(FileSystemObjectStorageOptions options)
    {
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

        if (options.DefaultRetentionYears is < 1 or > 100)
        {
            yield return "ObjectStorage:AzureBlob:DefaultRetentionYears must be between 1 and 100.";
        }

        if (options.PresignedUrlExpiryMinutes is < 1 or > 1440)
        {
            yield return "ObjectStorage:AzureBlob:PresignedUrlExpiryMinutes must be between 1 and 1440.";
        }
    }
}
