using System.Text.Json;
using System.Text.Json.Serialization;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;

namespace Darwin.Application.Foundation;

public sealed class AiTimelineActionDraftExecutor : IAiActionDraftExecutor
{
    public const string TimelineCommandType = "addinternaltimelineentry";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AiTimelineActionDraftExecutor(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public string FeatureAreaCode => "*";
    public string CommandType => TimelineCommandType;

    public bool CanExecute(AiActionDraft draft)
        => draft is
        {
            Status: AiActionDraftStatus.Approved,
            ExecutedAtUtc: null,
            TargetEntityId: not null
        } &&
           draft.RiskLevel != AiActionRiskLevel.High &&
           string.Equals(FoundationInputNormalizer.Key(draft.CommandType), TimelineCommandType, StringComparison.OrdinalIgnoreCase);

    public Task<Result<AiActionDraftExecutionResult>> ExecuteAsync(AiActionDraft draft, Guid actorUserId, CancellationToken ct = default)
    {
        if (!CanExecute(draft) || draft.TargetEntityId is null || draft.TargetEntityId == Guid.Empty)
        {
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail("AI timeline action draft is not executable."));
        }

        if (actorUserId == Guid.Empty)
        {
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail("AI timeline action requires an actor."));
        }

        var targetEntityType = FoundationInputNormalizer.Required(draft.TargetEntityType);
        if (targetEntityType is null)
        {
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail("AI timeline action requires a target entity type."));
        }

        var payload = ParsePayload(draft.CommandPayloadJson);
        if (!payload.Succeeded || payload.Value is null)
        {
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail(payload.Error ?? "AI timeline action payload is invalid."));
        }

        if (payload.Value.EntryType == AiTimelineEntryType.Note)
        {
            var body = FoundationInputNormalizer.Required(payload.Value.Body);
            if (body is null || FoundationInputNormalizer.LooksSensitive(body))
            {
                return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail("AI timeline note body is required and must be safe."));
            }

            var note = new Note
            {
                Id = Guid.NewGuid(),
                EntityType = targetEntityType,
                EntityId = draft.TargetEntityId.Value,
                Body = body,
                Visibility = FoundationVisibility.Internal,
                IsPinned = false,
                MetadataJson = BuildMetadata(draft.Id),
                CreatedAtUtc = _clock.UtcNow,
                CreatedByUserId = actorUserId
            };
            _db.Set<Note>().Add(note);
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Ok(new AiActionDraftExecutionResult(
                "AI internal note created.",
                "Note",
                note.Id)));
        }

        var activityType = FoundationInputNormalizer.Key(payload.Value.ActivityType) ?? "ai-review";
        var title = FoundationInputNormalizer.Required(payload.Value.Title);
        var summary = FoundationInputNormalizer.Optional(payload.Value.Summary);
        if (title is null ||
            FoundationInputNormalizer.LooksSensitive(activityType) ||
            FoundationInputNormalizer.LooksSensitive(title) ||
            FoundationInputNormalizer.LooksSensitive(summary))
        {
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Fail("AI timeline activity fields are required and must be safe."));
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            EntityType = targetEntityType,
            EntityId = draft.TargetEntityId.Value,
            ActivityType = activityType,
            OccurredAtUtc = _clock.UtcNow,
            ActorUserId = actorUserId,
            Title = title,
            Summary = summary,
            Visibility = FoundationVisibility.Internal,
            MetadataJson = BuildMetadata(draft.Id),
            CreatedAtUtc = _clock.UtcNow,
            CreatedByUserId = actorUserId
        };
        _db.Set<Activity>().Add(activity);
        return Task.FromResult(Result<AiActionDraftExecutionResult>.Ok(new AiActionDraftExecutionResult(
            "AI internal activity created.",
            "Activity",
            activity.Id)));
    }

    private static Result<AiTimelinePayload> ParsePayload(string? json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<AiTimelinePayload>(
                string.IsNullOrWhiteSpace(json) ? "{}" : json,
                PayloadJsonOptions);
            if (payload is null)
            {
                return Result<AiTimelinePayload>.Fail("AI timeline action payload is required.");
            }

            if (!Enum.IsDefined(payload.EntryType))
            {
                return Result<AiTimelinePayload>.Fail("AI timeline action entry type is invalid.");
            }

            return Result<AiTimelinePayload>.Ok(payload);
        }
        catch (JsonException)
        {
            return Result<AiTimelinePayload>.Fail("AI timeline action payload JSON is invalid.");
        }
    }

    private static string BuildMetadata(Guid draftId)
        => JsonSerializer.Serialize(new { source = "ai-action-handoff", actionDraftId = draftId });
}

public enum AiTimelineEntryType
{
    Note = 0,
    Activity = 1
}

public sealed class AiTimelinePayload
{
    public AiTimelineEntryType EntryType { get; set; }
    public string? Body { get; set; }
    public string? ActivityType { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
}
