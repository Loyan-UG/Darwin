using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

public interface IFinanceExportConnectorAdapter
{
    string AdapterCode { get; }

    bool CanDeliver(FinanceExportConnectorTarget target);

    Task<Result<FinanceExportConnectorAdapterDeliveryResult>> DeliverAsync(
        FinanceExportConnectorAdapterDeliveryRequest request,
        CancellationToken ct = default);
}

public sealed class FinanceExportConnectorDeliveryService
{
    public const string EntityType = FinanceExportPackageStorageService.EntityType;

    private readonly IAppDbContext _db;
    private readonly FinanceExportBatchService _batchService;
    private readonly FinanceExportPackageStorageService _packageStorage;
    private readonly ExternalSystemReferenceService _references;
    private readonly IReadOnlyList<IFinanceExportConnectorAdapter> _adapters;

    public FinanceExportConnectorDeliveryService(
        IAppDbContext db,
        FinanceExportBatchService batchService,
        FinanceExportPackageStorageService packageStorage,
        ExternalSystemReferenceService references,
        IEnumerable<IFinanceExportConnectorAdapter> adapters)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _packageStorage = packageStorage ?? throw new ArgumentNullException(nameof(packageStorage));
        _references = references ?? throw new ArgumentNullException(nameof(references));
        _adapters = adapters?.ToList() ?? throw new ArgumentNullException(nameof(adapters));
    }

    public async Task<Result<FinanceExportConnectorDeliveryResult>> DeliverAsync(
        FinanceExportConnectorDeliveryCommand command,
        CancellationToken ct = default)
    {
        if (command.FinanceExportBatchId == Guid.Empty)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail("Finance export batch id is required.");
        }

        var batch = await _db.Set<FinanceExportBatch>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.FinanceExportBatchId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail("Finance export batch was not found.");
        }

        if (batch.Status != FinanceExportBatchStatus.Generated)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail("Only generated finance export batches can be delivered.");
        }

        var externalSystem = await _db.Set<ExternalSystem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batch.ExternalSystemId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (externalSystem is null || !externalSystem.IsActive || externalSystem.Kind != ExternalSystemKind.Accounting)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail("External system must be an active accounting system.");
        }

        var target = new FinanceExportConnectorTarget(
            externalSystem.Id,
            externalSystem.Code,
            externalSystem.Name,
            externalSystem.Kind,
            externalSystem.BaseUrl,
            externalSystem.MetadataJson);
        var adapter = _adapters.FirstOrDefault(x => x.CanDeliver(target));
        if (adapter is null)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail("No finance export connector adapter is available for the target system.");
        }

        var attempt = await _batchService.StartAttemptAsync(batch.Id, "{\"purpose\":\"connector-delivery\"}", ct).ConfigureAwait(false);
        if (!attempt.Succeeded)
        {
            return Result<FinanceExportConnectorDeliveryResult>.Fail(attempt.Error!);
        }

        var attemptId = attempt.Value!.AttemptId;
        var package = await _packageStorage.GetStoredPackageAsync(batch.Id, ct).ConfigureAwait(false);
        if (!package.Succeeded)
        {
            await FailDeliveryAttemptAsync(attemptId, "Finance export package could not be read.", ct).ConfigureAwait(false);
            return Result<FinanceExportConnectorDeliveryResult>.Fail(package.Error!);
        }

        var packageValue = package.Value!;
        await using var packageStream = packageValue.Content;
        var request = new FinanceExportConnectorAdapterDeliveryRequest(
            batch.Id,
            batch.BusinessId,
            batch.ExportKey,
            batch.PeriodStartUtc,
            batch.PeriodEndUtc,
            batch.PostingStatusMode,
            target,
            packageStream,
            packageValue.ContentType,
            packageValue.FileName,
            packageValue.PackageHashSha256 ?? batch.PackageHashSha256 ?? string.Empty,
            packageValue.ContentLength);
        var delivered = await adapter.DeliverAsync(request, ct).ConfigureAwait(false);
        if (!delivered.Succeeded)
        {
            await FailDeliveryAttemptAsync(attemptId, "Finance export connector delivery failed.", ct).ConfigureAwait(false);
            return Result<FinanceExportConnectorDeliveryResult>.Fail(delivered.Error!);
        }

        var response = delivered.Value!;
        var validation = ValidateAdapterResponse(response);
        if (!validation.Succeeded)
        {
            await FailDeliveryAttemptAsync(attemptId, "Finance export connector response was invalid.", ct).ConfigureAwait(false);
            return Result<FinanceExportConnectorDeliveryResult>.Fail(validation.Error!);
        }

        var reference = await _references.UpsertReferenceAsync(
                new UpsertExternalReferenceCommand(
                    externalSystem.Id,
                    EntityType,
                    batch.Id,
                    ExternalReferenceKind.Export,
                    response.RemoteId,
                    response.RemoteDisplayId,
                    SourceOfTruth.External,
                    IsPrimary: true,
                    LastSeenAtUtc: response.DeliveredAtUtc,
                    MetadataJson: BuildReferenceMetadata(adapter.AdapterCode, response.SafeSummary)),
                ct)
            .ConfigureAwait(false);
        if (!reference.Succeeded)
        {
            await FailDeliveryAttemptAsync(attemptId, "Finance export target reference could not be recorded.", ct).ConfigureAwait(false);
            return Result<FinanceExportConnectorDeliveryResult>.Fail(reference.Error!);
        }

        var markDelivered = await _batchService.MarkDeliveredAsync(
                attemptId,
                request.PackageHashSha256,
                request.PackageContentType,
                request.PackageFileName,
                BuildDeliveryMetadata(adapter.AdapterCode, response.RemoteId, response.SafeSummary),
                ct)
            .ConfigureAwait(false);
        if (!markDelivered.Succeeded)
        {
            await FailDeliveryAttemptAsync(attemptId, "Finance export delivery could not be completed.", ct).ConfigureAwait(false);
            return Result<FinanceExportConnectorDeliveryResult>.Fail(markDelivered.Error!);
        }

        return Result<FinanceExportConnectorDeliveryResult>.Ok(new FinanceExportConnectorDeliveryResult(
            batch.Id,
            attemptId,
            reference.Value,
            response.RemoteId,
            response.RemoteDisplayId));
    }

    private Task<Result> FailDeliveryAttemptAsync(Guid attemptId, string error, CancellationToken ct)
        => _batchService.FailAttemptAsync(attemptId, error, ct, markBatchFailed: false);

    private static Result ValidateAdapterResponse(FinanceExportConnectorAdapterDeliveryResult response)
    {
        if (FoundationInputNormalizer.Required(response.RemoteId) is null)
        {
            return Result.Fail("Remote delivery id is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(response.RemoteId) ||
            FoundationInputNormalizer.LooksSensitive(response.RemoteDisplayId) ||
            FoundationInputNormalizer.LooksSensitive(response.SafeSummary))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance export connector metadata.");
        }

        return Result.Ok();
    }

    private static string BuildReferenceMetadata(string adapterCode, string? safeSummary)
        => JsonSerializer.Serialize(new
        {
            source = "finance-export-connector",
            adapter = FoundationInputNormalizer.Optional(adapterCode),
            summary = FoundationInputNormalizer.Optional(safeSummary)
        });

    private static string BuildDeliveryMetadata(string adapterCode, string remoteId, string? safeSummary)
        => JsonSerializer.Serialize(new
        {
            purpose = "connector-delivery",
            adapter = FoundationInputNormalizer.Optional(adapterCode),
            remoteId = FoundationInputNormalizer.Required(remoteId),
            summary = FoundationInputNormalizer.Optional(safeSummary)
        });
}

public sealed record FinanceExportConnectorDeliveryCommand(Guid FinanceExportBatchId);

public sealed record FinanceExportConnectorTarget(
    Guid ExternalSystemId,
    string Code,
    string Name,
    ExternalSystemKind Kind,
    string? BaseUrl,
    string MetadataJson);

public sealed record FinanceExportConnectorAdapterDeliveryRequest(
    Guid FinanceExportBatchId,
    Guid BusinessId,
    string ExportKey,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    FinanceExportPostingStatusMode PostingStatusMode,
    FinanceExportConnectorTarget Target,
    Stream PackageContent,
    string PackageContentType,
    string PackageFileName,
    string PackageHashSha256,
    long? PackageContentLength);

public sealed record FinanceExportConnectorAdapterDeliveryResult(
    string RemoteId,
    string? RemoteDisplayId = null,
    DateTime? DeliveredAtUtc = null,
    string? SafeSummary = null);

public sealed record FinanceExportConnectorDeliveryResult(
    Guid FinanceExportBatchId,
    Guid AttemptId,
    Guid ExternalReferenceId,
    string RemoteId,
    string? RemoteDisplayId);
