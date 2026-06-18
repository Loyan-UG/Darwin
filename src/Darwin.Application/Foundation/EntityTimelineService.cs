using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class EntityTimelineService
{
    private readonly IAppDbContext _db;

    public EntityTimelineService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> AddActivityAsync(AddActivityCommand command, CancellationToken ct = default)
    {
        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("Entity id is required.");
        }

        var entityType = FoundationInputNormalizer.Required(command.EntityType);
        var activityType = FoundationInputNormalizer.Required(command.ActivityType);
        var title = FoundationInputNormalizer.Required(command.Title);
        if (entityType is null)
        {
            return Result<Guid>.Fail("Entity type is required.");
        }

        if (activityType is null)
        {
            return Result<Guid>.Fail("Activity type is required.");
        }

        if (title is null)
        {
            return Result<Guid>.Fail("Activity title is required.");
        }

        var activity = new Activity
        {
            EntityType = entityType,
            EntityId = command.EntityId,
            ActivityType = activityType,
            OccurredAtUtc = command.OccurredAtUtc == default ? DateTime.UtcNow : command.OccurredAtUtc,
            ActorUserId = command.ActorUserId,
            Title = title,
            Summary = FoundationInputNormalizer.Optional(command.Summary),
            Visibility = command.Visibility,
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<Activity>().Add(activity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(activity.Id);
    }

    public async Task<Result<Guid>> AddNoteAsync(AddNoteCommand command, CancellationToken ct = default)
    {
        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("Entity id is required.");
        }

        var entityType = FoundationInputNormalizer.Required(command.EntityType);
        var body = FoundationInputNormalizer.Required(command.Body);
        if (entityType is null)
        {
            return Result<Guid>.Fail("Entity type is required.");
        }

        if (body is null)
        {
            return Result<Guid>.Fail("Note body is required.");
        }

        var note = new Note
        {
            EntityType = entityType,
            EntityId = command.EntityId,
            Body = body,
            Visibility = command.Visibility,
            IsPinned = command.IsPinned,
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Set<Note>().Add(note);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(note.Id);
    }

    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesForEntityAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<ActivityDto>();
        }

        var query = _db.Set<Activity>()
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
            .Select(x => new ActivityDto(
                x.Id,
                x.EntityType,
                x.EntityId,
                x.ActivityType,
                x.OccurredAtUtc,
                x.ActorUserId,
                x.Title,
                x.Summary,
                x.Visibility,
                x.MetadataJson))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NoteDto>> GetNotesForEntityAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<NoteDto>();
        }

        var query = _db.Set<Note>()
            .AsNoTracking()
            .Where(x => x.EntityType == normalizedEntityType && x.EntityId == entityId && !x.IsDeleted);

        if (maxVisibility.HasValue)
        {
            var allowedVisibilities = ResolveAllowedVisibilities(maxVisibility.Value);
            query = query.Where(x => allowedVisibilities.Contains(x.Visibility));
        }

        return await query
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new NoteDto(
                x.Id,
                x.EntityType,
                x.EntityId,
                x.Body,
                x.Visibility,
                x.IsPinned,
                x.MetadataJson,
                x.CreatedAtUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static FoundationVisibility[] ResolveAllowedVisibilities(FoundationVisibility maxVisibility)
        => Enum.GetValues<FoundationVisibility>()
            .Where(value => Convert.ToInt32(value) <= Convert.ToInt32(maxVisibility))
            .ToArray();
}

public sealed record AddActivityCommand(
    string? EntityType,
    Guid EntityId,
    string? ActivityType,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? Title,
    string? Summary = null,
    FoundationVisibility Visibility = FoundationVisibility.Internal,
    string? MetadataJson = null);

public sealed record AddNoteCommand(
    string? EntityType,
    Guid EntityId,
    string? Body,
    FoundationVisibility Visibility = FoundationVisibility.Internal,
    bool IsPinned = false,
    string? MetadataJson = null);

public sealed record ActivityDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string ActivityType,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string Title,
    string? Summary,
    FoundationVisibility Visibility,
    string MetadataJson);

public sealed record NoteDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Body,
    FoundationVisibility Visibility,
    bool IsPinned,
    string MetadataJson,
    DateTime CreatedAtUtc);
