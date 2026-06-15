using Darwin.Application.CRM.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;

namespace Darwin.Application.CRM.Queries;

public sealed class GetCrmFoundationPanelHandler
{
    private static readonly HashSet<string> AllowedEntityTypes = new(StringComparer.Ordinal)
    {
        CrmFoundationPrimitiveService.EntityTypes.Customer,
        CrmFoundationPrimitiveService.EntityTypes.Lead,
        CrmFoundationPrimitiveService.EntityTypes.Opportunity
    };

    private readonly CrmFoundationPrimitiveService _foundation;

    public GetCrmFoundationPanelHandler(CrmFoundationPrimitiveService foundation)
    {
        _foundation = foundation ?? throw new ArgumentNullException(nameof(foundation));
    }

    public async Task<Result<CrmFoundationPanelDto>> HandleAsync(
        string? entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var normalizedEntityType = NormalizeEntityType(entityType);
        if (normalizedEntityType is null)
        {
            return Result<CrmFoundationPanelDto>.Fail("CRM foundation entity type is not supported.");
        }

        if (entityId == Guid.Empty)
        {
            return Result<CrmFoundationPanelDto>.Fail("CRM foundation entity id is required.");
        }

        var references = await _foundation.GetExternalReferencesAsync(normalizedEntityType, entityId, ct).ConfigureAwait(false);
        var activities = await _foundation.GetActivitiesAsync(normalizedEntityType, entityId, FoundationVisibility.Internal, ct).ConfigureAwait(false);
        var notes = await _foundation.GetNotesAsync(normalizedEntityType, entityId, FoundationVisibility.Internal, ct).ConfigureAwait(false);
        var documents = await _foundation.GetDocumentsAsync(normalizedEntityType, entityId, FoundationVisibility.Internal, ct).ConfigureAwait(false);
        var events = await _foundation.GetEventsAsync(normalizedEntityType, entityId, FoundationVisibility.Internal, ct).ConfigureAwait(false);
        var audits = await _foundation.GetAuditTrailAsync(normalizedEntityType, entityId, ct).ConfigureAwait(false);

        return Result<CrmFoundationPanelDto>.Ok(new CrmFoundationPanelDto(
            normalizedEntityType,
            entityId,
            references,
            activities,
            notes,
            documents,
            events,
            audits));
    }

    internal static string? NormalizeEntityType(string? entityType)
    {
        var normalized = entityType?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var match = AllowedEntityTypes.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match;
    }
}

public sealed class AddCrmFoundationNoteHandler
{
    private readonly CrmFoundationPrimitiveService _foundation;

    public AddCrmFoundationNoteHandler(CrmFoundationPrimitiveService foundation)
    {
        _foundation = foundation ?? throw new ArgumentNullException(nameof(foundation));
    }

    public async Task<Result<Guid>> HandleAsync(
        AddCrmFoundationNoteCommand command,
        CancellationToken ct = default)
    {
        var normalizedEntityType = GetCrmFoundationPanelHandler.NormalizeEntityType(command.EntityType);
        if (normalizedEntityType is null)
        {
            return Result<Guid>.Fail("CRM foundation entity type is not supported.");
        }

        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("CRM foundation entity id is required.");
        }

        var body = command.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return Result<Guid>.Fail("Note body is required.");
        }

        return await _foundation.AddNoteAsync(
            normalizedEntityType,
            command.EntityId,
            body,
            FoundationVisibility.Internal,
            isPinned: false,
            metadataJson: """{"source":"webadmin-crm"}""",
            ct)
            .ConfigureAwait(false);
    }
}

public sealed record AddCrmFoundationNoteCommand(
    string? EntityType,
    Guid EntityId,
    string? Body);

public sealed record CrmFoundationPanelDto(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<ExternalReferenceDto> ExternalReferences,
    IReadOnlyList<ActivityDto> Activities,
    IReadOnlyList<NoteDto> Notes,
    IReadOnlyList<DocumentRecordDto> Documents,
    IReadOnlyList<BusinessEventDto> Events,
    IReadOnlyList<AuditTrailDto> AuditTrail);
