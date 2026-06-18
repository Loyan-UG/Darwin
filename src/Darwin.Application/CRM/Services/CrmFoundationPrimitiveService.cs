using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Services;

public sealed class CrmFoundationPrimitiveService
{
    private readonly IAppDbContext _db;
    private readonly ExternalSystemReferenceService _externalReferences;
    private readonly EntityTimelineService _timeline;
    private readonly DocumentRecordService _documents;
    private readonly BusinessEventService _events;

    public CrmFoundationPrimitiveService(
        IAppDbContext db,
        ExternalSystemReferenceService externalReferences,
        EntityTimelineService timeline,
        DocumentRecordService documents,
        BusinessEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _externalReferences = externalReferences ?? throw new ArgumentNullException(nameof(externalReferences));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public static class EntityTypes
    {
        public const string Customer = "Customer";
        public const string Lead = "Lead";
        public const string Opportunity = "Opportunity";
        public const string Consent = "Consent";
        public const string CustomerSegment = "CustomerSegment";
        public const string Invoice = "Invoice";
    }

    public Task<Result<Guid>> UpsertExternalReferenceAsync(
        Guid externalSystemId,
        string entityType,
        Guid entityId,
        ExternalReferenceKind referenceKind,
        string externalId,
        string? externalDisplayId,
        SourceOfTruth sourceOfTruth,
        bool isPrimary = false,
        DateTime? lastSeenAtUtc = null,
        string? metadataJson = null,
        CancellationToken ct = default)
        => _externalReferences.UpsertReferenceAsync(
            new UpsertExternalReferenceCommand(
                externalSystemId,
                entityType,
                entityId,
                referenceKind,
                externalId,
                externalDisplayId,
                sourceOfTruth,
                isPrimary,
                IsActive: true,
                lastSeenAtUtc,
                metadataJson),
            ct);

    public Task<IReadOnlyList<ExternalReferenceDto>> GetExternalReferencesAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
        => _externalReferences.GetReferencesForEntityAsync(entityType, entityId, ct);

    public Task<Result<Guid>> AddNoteAsync(
        string entityType,
        Guid entityId,
        string body,
        FoundationVisibility visibility = FoundationVisibility.Internal,
        bool isPinned = false,
        string? metadataJson = null,
        CancellationToken ct = default)
        => _timeline.AddNoteAsync(
            new AddNoteCommand(entityType, entityId, body, visibility, isPinned, metadataJson),
            ct);

    public Task<IReadOnlyList<NoteDto>> GetNotesAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
        => _timeline.GetNotesForEntityAsync(entityType, entityId, maxVisibility, ct);

    public async Task<Result<Guid>> AddActivityAsync(
        string entityType,
        Guid entityId,
        string activityType,
        DateTime occurredAtUtc,
        Guid? actorUserId,
        string title,
        string? summary = null,
        FoundationVisibility visibility = FoundationVisibility.Internal,
        string? metadataJson = null,
        CancellationToken ct = default)
    {
        var normalizedMetadata = NormalizeJson(metadataJson);
        var existingId = await _db.Set<Activity>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == entityType &&
                x.EntityId == entityId &&
                x.ActivityType == activityType &&
                x.MetadataJson == normalizedMetadata &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existingId != Guid.Empty)
        {
            return Result<Guid>.Ok(existingId);
        }

        return await _timeline.AddActivityAsync(
            new AddActivityCommand(
                entityType,
                entityId,
                activityType,
                occurredAtUtc,
                actorUserId,
                title,
                summary,
                visibility,
                normalizedMetadata),
            ct)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
        => _timeline.GetActivitiesForEntityAsync(entityType, entityId, maxVisibility, ct);

    public Task<Result<Guid>> RegisterDocumentAsync(
        string entityType,
        Guid entityId,
        DocumentRecordKind documentKind,
        string title,
        string fileName,
        string contentType,
        long? sizeBytes,
        string? contentHash,
        string storageProvider,
        string storageContainer,
        string storageKey,
        Guid? mediaAssetId = null,
        FoundationVisibility visibility = FoundationVisibility.Internal,
        string? metadataJson = null,
        CancellationToken ct = default)
        => _documents.RegisterDocumentAsync(
            new RegisterDocumentRecordCommand(
                entityType,
                entityId,
                documentKind,
                title,
                fileName,
                contentType,
                sizeBytes,
                contentHash,
                storageProvider,
                storageContainer,
                storageKey,
                mediaAssetId,
                visibility,
                metadataJson),
            ct);

    public Task<IReadOnlyList<DocumentRecordDto>> GetDocumentsAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
        => _documents.GetDocumentsForEntityAsync(entityType, entityId, maxVisibility, ct);

    public Task<IReadOnlyList<BusinessEventDto>> GetEventsAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
        => _events.GetEventsForEntityAsync(entityType, entityId, maxVisibility, ct);

    public Task<IReadOnlyList<AuditTrailDto>> GetAuditTrailAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
        => _events.GetAuditTrailForEntityAsync(entityType, entityId, ct);

    public async Task<Result<Guid>> RecordLifecycleEventAsync(
        string entityType,
        Guid entityId,
        string eventType,
        string eventKey,
        DateTime occurredAtUtc,
        Guid? actorUserId,
        string title,
        string? summary = null,
        string? payloadJson = null,
        AuditTrailAction auditAction = AuditTrailAction.StatusChanged,
        string? reason = null,
        CancellationToken ct = default)
    {
        var normalizedEventKey = NormalizeRequired(eventKey);
        if (normalizedEventKey is null)
        {
            return Result<Guid>.Fail("Event key is required.");
        }

        var eventResult = await _events.AddEventAsync(
            new AddBusinessEventCommand(
                BusinessId: null,
                EntityType: entityType,
                EntityId: entityId,
                EventType: eventType,
                EventKey: normalizedEventKey,
                OccurredAtUtc: occurredAtUtc,
                ActorUserId: actorUserId,
                Source: BusinessEventSource.System,
                Severity: BusinessEventSeverity.Info,
                Visibility: FoundationVisibility.Internal,
                Title: title,
                Summary: summary,
                CorrelationId: normalizedEventKey,
                CausationId: null,
                PayloadJson: payloadJson,
                MetadataJson: BuildEventMetadata(normalizedEventKey)),
            ct)
            .ConfigureAwait(false);

        if (!eventResult.Succeeded)
        {
            return eventResult;
        }

        var existingAuditId = await _db.Set<AuditTrail>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == entityType &&
                x.EntityId == entityId &&
                x.Action == auditAction &&
                x.CorrelationId == normalizedEventKey &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existingAuditId == Guid.Empty)
        {
            var auditResult = await _events.AddAuditTrailAsync(
                new AddAuditTrailCommand(
                    BusinessId: null,
                    EntityType: entityType,
                    EntityId: entityId,
                    Action: auditAction,
                    OccurredAtUtc: occurredAtUtc,
                    ActorUserId: actorUserId,
                    BusinessEventId: eventResult.Value,
                    Reason: reason,
                    CorrelationId: normalizedEventKey,
                    ChangeSetJson: payloadJson,
                    MetadataJson: BuildEventMetadata(normalizedEventKey)),
                ct)
                .ConfigureAwait(false);

            if (!auditResult.Succeeded)
            {
                return Result<Guid>.Fail(auditResult.Error!);
            }
        }

        return eventResult;
    }

    private static string BuildEventMetadata(string eventKey) =>
        $$"""{"eventKey":"{{EscapeJson(eventKey)}}"}""";

    private static string NormalizeJson(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized;
    }

    private static string? NormalizeRequired(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
