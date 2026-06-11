using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class BusinessEventService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public BusinessEventService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<Guid>> AddEventAsync(AddBusinessEventCommand command, CancellationToken ct = default)
    {
        var validation = ValidateEvent(command);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error!);
        }

        var eventKey = FoundationInputNormalizer.Optional(command.EventKey);
        if (eventKey is not null)
        {
            var existingId = await _db.Set<BusinessEvent>()
                .AsNoTracking()
                .Where(x => x.EventKey == eventKey && !x.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (existingId != Guid.Empty)
            {
                return Result<Guid>.Ok(existingId);
            }
        }

        var businessEvent = new BusinessEvent
        {
            BusinessId = command.BusinessId,
            EntityType = FoundationInputNormalizer.Required(command.EntityType)!,
            EntityId = command.EntityId,
            EventType = FoundationInputNormalizer.Required(command.EventType)!,
            EventKey = eventKey,
            OccurredAtUtc = command.OccurredAtUtc == default ? _clock.UtcNow : command.OccurredAtUtc,
            ActorUserId = command.ActorUserId,
            Source = command.Source,
            Severity = command.Severity,
            Visibility = command.Visibility,
            Title = FoundationInputNormalizer.Required(command.Title)!,
            Summary = FoundationInputNormalizer.Optional(command.Summary),
            CorrelationId = FoundationInputNormalizer.Optional(command.CorrelationId),
            CausationId = FoundationInputNormalizer.Optional(command.CausationId),
            PayloadJson = FoundationInputNormalizer.Json(command.PayloadJson),
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<BusinessEvent>().Add(businessEvent);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(businessEvent.Id);
    }

    public async Task<Result<Guid>> AddAuditTrailAsync(AddAuditTrailCommand command, CancellationToken ct = default)
    {
        var validation = ValidateAuditTrail(command);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error!);
        }

        var auditTrail = new AuditTrail
        {
            BusinessId = command.BusinessId,
            EntityType = FoundationInputNormalizer.Required(command.EntityType)!,
            EntityId = command.EntityId,
            Action = command.Action,
            OccurredAtUtc = command.OccurredAtUtc == default ? _clock.UtcNow : command.OccurredAtUtc,
            ActorUserId = command.ActorUserId,
            BusinessEventId = command.BusinessEventId,
            Reason = FoundationInputNormalizer.Optional(command.Reason),
            CorrelationId = FoundationInputNormalizer.Optional(command.CorrelationId),
            ChangeSetJson = FoundationInputNormalizer.Json(command.ChangeSetJson),
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<AuditTrail>().Add(auditTrail);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(auditTrail.Id);
    }

    public async Task<IReadOnlyList<BusinessEventDto>> GetEventsForEntityAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<BusinessEventDto>();
        }

        var query = _db.Set<BusinessEvent>()
            .AsNoTracking()
            .Where(x => x.EntityType == normalizedEntityType && x.EntityId == entityId && !x.IsDeleted);

        if (maxVisibility.HasValue)
        {
            var allowedVisibilities = ResolveAllowedVisibilities(maxVisibility.Value);
            query = query.Where(x => allowedVisibilities.Contains(x.Visibility));
        }

        return await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => MapEvent(x))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditTrailDto>> GetAuditTrailForEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<AuditTrailDto>();
        }

        return await _db.Set<AuditTrail>()
            .AsNoTracking()
            .Where(x => x.EntityType == normalizedEntityType && x.EntityId == entityId && !x.IsDeleted)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => MapAuditTrail(x))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BusinessEventDto>> GetEventsByCorrelationAsync(
        string? correlationId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
    {
        var normalizedCorrelationId = FoundationInputNormalizer.Optional(correlationId);
        if (normalizedCorrelationId is null)
        {
            return Array.Empty<BusinessEventDto>();
        }

        var query = _db.Set<BusinessEvent>()
            .AsNoTracking()
            .Where(x => x.CorrelationId == normalizedCorrelationId && !x.IsDeleted);

        if (maxVisibility.HasValue)
        {
            var allowedVisibilities = ResolveAllowedVisibilities(maxVisibility.Value);
            query = query.Where(x => allowedVisibilities.Contains(x.Visibility));
        }

        return await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => MapEvent(x))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static Result ValidateEvent(AddBusinessEventCommand command)
    {
        if (FoundationInputNormalizer.Required(command.EntityType) is null)
        {
            return Result.Fail("Entity type is required.");
        }

        if (FoundationInputNormalizer.Required(command.EventType) is null)
        {
            return Result.Fail("Event type is required.");
        }

        if (FoundationInputNormalizer.Required(command.Title) is null)
        {
            return Result.Fail("Event title is required.");
        }

        if (!Enum.IsDefined(typeof(BusinessEventSource), command.Source))
        {
            return Result.Fail("Event source is required.");
        }

        if (!Enum.IsDefined(typeof(BusinessEventSeverity), command.Severity))
        {
            return Result.Fail("Event severity is required.");
        }

        if (ContainsSensitiveEventData(command))
        {
            return Result.Fail("Sensitive secrets must not be stored in business event payload or metadata.");
        }

        return Result.Ok();
    }

    private static Result ValidateAuditTrail(AddAuditTrailCommand command)
    {
        if (FoundationInputNormalizer.Required(command.EntityType) is null)
        {
            return Result.Fail("Entity type is required.");
        }

        if (command.EntityId == Guid.Empty)
        {
            return Result.Fail("Entity id is required.");
        }

        if (!Enum.IsDefined(typeof(AuditTrailAction), command.Action))
        {
            return Result.Fail("Audit action is required.");
        }

        if (ContainsSensitiveAuditData(command))
        {
            return Result.Fail("Sensitive secrets must not be stored in audit trail change set or metadata.");
        }

        return Result.Ok();
    }

    private static bool ContainsSensitiveEventData(AddBusinessEventCommand command)
        => FoundationInputNormalizer.LooksSensitive(command.PayloadJson) ||
           FoundationInputNormalizer.LooksSensitive(command.MetadataJson);

    private static bool ContainsSensitiveAuditData(AddAuditTrailCommand command)
        => FoundationInputNormalizer.LooksSensitive(command.ChangeSetJson) ||
           FoundationInputNormalizer.LooksSensitive(command.MetadataJson);

    private static FoundationVisibility[] ResolveAllowedVisibilities(FoundationVisibility maxVisibility)
        => Enum.GetValues<FoundationVisibility>()
            .Where(value => Convert.ToInt32(value) <= Convert.ToInt32(maxVisibility))
            .ToArray();

    private static BusinessEventDto MapEvent(BusinessEvent businessEvent)
        => new(
            businessEvent.Id,
            businessEvent.BusinessId,
            businessEvent.EntityType,
            businessEvent.EntityId,
            businessEvent.EventType,
            businessEvent.EventKey,
            businessEvent.OccurredAtUtc,
            businessEvent.ActorUserId,
            businessEvent.Source,
            businessEvent.Severity,
            businessEvent.Visibility,
            businessEvent.Title,
            businessEvent.Summary,
            businessEvent.CorrelationId,
            businessEvent.CausationId,
            businessEvent.PayloadJson,
            businessEvent.MetadataJson);

    private static AuditTrailDto MapAuditTrail(AuditTrail auditTrail)
        => new(
            auditTrail.Id,
            auditTrail.BusinessId,
            auditTrail.EntityType,
            auditTrail.EntityId,
            auditTrail.Action,
            auditTrail.OccurredAtUtc,
            auditTrail.ActorUserId,
            auditTrail.BusinessEventId,
            auditTrail.Reason,
            auditTrail.CorrelationId,
            auditTrail.ChangeSetJson,
            auditTrail.MetadataJson);
}

public sealed record AddBusinessEventCommand(
    Guid? BusinessId,
    string? EntityType,
    Guid? EntityId,
    string? EventType,
    string? EventKey,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    BusinessEventSource Source,
    BusinessEventSeverity Severity,
    FoundationVisibility Visibility,
    string? Title,
    string? Summary = null,
    string? CorrelationId = null,
    string? CausationId = null,
    string? PayloadJson = null,
    string? MetadataJson = null);

public sealed record AddAuditTrailCommand(
    Guid? BusinessId,
    string? EntityType,
    Guid EntityId,
    AuditTrailAction Action,
    DateTime OccurredAtUtc,
    Guid? ActorUserId = null,
    Guid? BusinessEventId = null,
    string? Reason = null,
    string? CorrelationId = null,
    string? ChangeSetJson = null,
    string? MetadataJson = null);

public sealed record BusinessEventDto(
    Guid Id,
    Guid? BusinessId,
    string EntityType,
    Guid? EntityId,
    string EventType,
    string? EventKey,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    BusinessEventSource Source,
    BusinessEventSeverity Severity,
    FoundationVisibility Visibility,
    string Title,
    string? Summary,
    string? CorrelationId,
    string? CausationId,
    string PayloadJson,
    string MetadataJson);

public sealed record AuditTrailDto(
    Guid Id,
    Guid? BusinessId,
    string EntityType,
    Guid EntityId,
    AuditTrailAction Action,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    Guid? BusinessEventId,
    string? Reason,
    string? CorrelationId,
    string ChangeSetJson,
    string MetadataJson);
