using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Storage;
using Darwin.Shared.Results;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Billing;

public sealed class FinanceExportFileDeliveryAdapter : IFinanceExportConnectorAdapter
{
    public const string AdapterCodeValue = "file-delivery";
    public const string ProfileName = "FinanceExportOutbound";
    public const string ContainerName = "finance-exports-outbound";

    private readonly IObjectStorageService _storage;
    private readonly IOptions<ObjectStorageOptions> _options;
    private readonly IClock _clock;

    public FinanceExportFileDeliveryAdapter(
        IObjectStorageService storage,
        IOptions<ObjectStorageOptions> options,
        IClock clock)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public string AdapterCode => AdapterCodeValue;

    public bool CanDeliver(FinanceExportConnectorTarget target)
        => target.Kind == ExternalSystemKind.Accounting && IsOutboundProfileReady(_options.Value);

    public async Task<Result<FinanceExportConnectorAdapterDeliveryResult>> DeliverAsync(
        FinanceExportConnectorAdapterDeliveryRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FinanceExportBatchId == Guid.Empty)
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export batch id is required.");
        }

        if (request.BusinessId == Guid.Empty)
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Business id is required.");
        }

        if (request.Target.ExternalSystemId == Guid.Empty || request.Target.Kind != ExternalSystemKind.Accounting)
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export file delivery requires an accounting target.");
        }

        if (!IsOutboundProfileReady(_options.Value))
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export outbound storage is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.PackageHashSha256))
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export package hash is required.");
        }

        if (request.PeriodStartUtc >= request.PeriodEndUtc)
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export period is invalid.");
        }

        var objectKey = BuildObjectKey(request);
        var fileName = BuildFileName(request);
        var reference = new ObjectStorageObjectReference(ContainerName, objectKey, ProfileName: ProfileName);

        try
        {
            if (await _storage.ExistsAsync(reference, ct).ConfigureAwait(false))
            {
                var existing = await ReadExistingObjectAsync(reference, ct).ConfigureAwait(false);
                if (!string.Equals(existing.HashSha256, request.PackageHashSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Existing finance export delivery object hash does not match the package.");
                }

                return Result<FinanceExportConnectorAdapterDeliveryResult>.Ok(BuildResult(existing.ObjectKey ?? objectKey, fileName, alreadyDelivered: true));
            }

            var write = await _storage.SaveAsync(
                    new ObjectStorageWriteRequest(
                        ContainerName,
                        objectKey,
                        request.PackageContentType,
                        fileName,
                        request.PackageContent,
                        request.PackageContentLength,
                        request.PackageHashSha256,
                        new Dictionary<string, string>
                        {
                            ["entity-type"] = FinanceExportConnectorDeliveryService.EntityType,
                            ["entity-id"] = request.FinanceExportBatchId.ToString("N"),
                            ["business-id"] = request.BusinessId.ToString("N"),
                            ["external-system-id"] = request.Target.ExternalSystemId.ToString("N"),
                            ["export-key"] = request.ExportKey,
                            ["posting-status-mode"] = request.PostingStatusMode.ToString()
                        },
                        OverwritePolicy: ObjectOverwritePolicy.Disallow,
                        ProfileName: ProfileName),
                    ct)
                .ConfigureAwait(false);

            if (!string.Equals(write.Sha256Hash, request.PackageHashSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail("Finance export delivery object hash does not match the package.");
            }

            return Result<FinanceExportConnectorAdapterDeliveryResult>.Ok(BuildResult(write.ObjectKey, fileName, alreadyDelivered: false));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result<FinanceExportConnectorAdapterDeliveryResult>.Fail(ex.Message);
        }
    }

    public static bool IsOutboundProfileReady(ObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Profiles.TryGetValue(ProfileName, out var profile) &&
               profile.Provider is ObjectStorageProviderKind.FileSystem or ObjectStorageProviderKind.S3Compatible or ObjectStorageProviderKind.AzureBlob;
    }

    internal static string BuildObjectKey(FinanceExportConnectorAdapterDeliveryRequest request)
        => ObjectStorageKeyBuilder.Build(
            "finance-exports",
            "outbound",
            request.BusinessId.ToString("N"),
            request.Target.ExternalSystemId.ToString("N"),
            $"{EnsureUtc(request.PeriodStartUtc):yyyyMMdd}-{EnsureUtc(request.PeriodEndUtc):yyyyMMdd}",
            request.FinanceExportBatchId.ToString("N") + ".json");

    internal static string BuildFileName(FinanceExportConnectorAdapterDeliveryRequest request)
        => $"finance-export-{request.BusinessId:N}-{EnsureUtc(request.PeriodStartUtc):yyyyMMdd}-{EnsureUtc(request.PeriodEndUtc):yyyyMMdd}-{request.FinanceExportBatchId:N}.json";

    private async Task<ExistingDeliveryObject> ReadExistingObjectAsync(ObjectStorageObjectReference reference, CancellationToken ct)
    {
        var metadata = await _storage.GetMetadataAsync(reference, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(metadata?.Sha256Hash))
        {
            return new ExistingDeliveryObject(metadata.Sha256Hash, metadata.ObjectKey);
        }

        var read = await _storage.ReadAsync(reference, ct).ConfigureAwait(false);
        if (read is null)
        {
            return new ExistingDeliveryObject(null, null);
        }

        await using (read.Content.ConfigureAwait(false))
        {
            if (!string.IsNullOrWhiteSpace(read.Sha256Hash))
            {
                return new ExistingDeliveryObject(read.Sha256Hash, null);
            }

            var hash = await System.Security.Cryptography.SHA256.HashDataAsync(read.Content, ct).ConfigureAwait(false);
            return new ExistingDeliveryObject(Convert.ToHexString(hash).ToLowerInvariant(), null);
        }
    }

    private FinanceExportConnectorAdapterDeliveryResult BuildResult(string objectKey, string fileName, bool alreadyDelivered)
        => new(
            objectKey,
            fileName,
            _clock.UtcNow,
            alreadyDelivered ? "File delivery already stored with matching hash." : "File delivery stored with matching hash.");

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed record ExistingDeliveryObject(string? HashSha256, string? ObjectKey);
}
