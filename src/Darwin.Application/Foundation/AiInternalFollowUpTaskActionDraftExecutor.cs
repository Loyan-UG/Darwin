using System.Text.Json;
using System.Text.Json.Serialization;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;

namespace Darwin.Application.Foundation;

public sealed class AiInternalFollowUpTaskActionDraftExecutor : AiFollowUpTaskActionDraftExecutorBase
{
    public const string FollowUpTaskCommandType = "createinternalfollowuptask";

    public AiInternalFollowUpTaskActionDraftExecutor(InternalFollowUpTaskService tasks, IClock clock)
        : base(tasks, clock)
    {
    }

    public override string CommandType => FollowUpTaskCommandType;
    protected override string InvalidDraftMessage => "AI internal follow-up task draft is not executable.";
    protected override string InvalidPayloadMessage => "AI internal follow-up task payload is invalid.";
    protected override string SuccessSummary => "AI internal follow-up task created.";
}

public sealed class AiModuleReviewTaskActionDraftExecutor : AiFollowUpTaskActionDraftExecutorBase
{
    public const string ModuleReviewTaskCommandType = "createmodulereviewtask";

    public AiModuleReviewTaskActionDraftExecutor(InternalFollowUpTaskService tasks, IClock clock)
        : base(tasks, clock)
    {
    }

    public override string CommandType => ModuleReviewTaskCommandType;
    protected override string InvalidDraftMessage => "AI module review task draft is not executable.";
    protected override string InvalidPayloadMessage => "AI module review task payload is invalid.";
    protected override string SuccessSummary => "AI module review task created.";
}

public abstract class AiFollowUpTaskActionDraftExecutorBase : IAiActionDraftExecutor
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly InternalFollowUpTaskService _tasks;
    private readonly IClock _clock;

    protected AiFollowUpTaskActionDraftExecutorBase(InternalFollowUpTaskService tasks, IClock clock)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public string FeatureAreaCode => "*";
    public abstract string CommandType { get; }
    protected abstract string InvalidDraftMessage { get; }
    protected abstract string InvalidPayloadMessage { get; }
    protected abstract string SuccessSummary { get; }

    public bool CanExecute(AiActionDraft draft)
        => draft is
        {
            Status: AiActionDraftStatus.Approved,
            ExecutedAtUtc: null,
            TargetEntityId: not null
        } &&
           draft.RiskLevel != AiActionRiskLevel.High &&
           string.Equals(
               FoundationInputNormalizer.Key(draft.CommandType),
               FoundationInputNormalizer.Key(CommandType),
               StringComparison.OrdinalIgnoreCase);

    public async Task<Result<AiActionDraftExecutionResult>> ExecuteAsync(
        AiActionDraft draft,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (!CanExecute(draft) || draft.TargetEntityId is null || draft.TargetEntityId == Guid.Empty)
        {
            return Result<AiActionDraftExecutionResult>.Fail(InvalidDraftMessage);
        }

        if (actorUserId == Guid.Empty)
        {
            return Result<AiActionDraftExecutionResult>.Fail("AI follow-up task requires an actor.");
        }

        var payload = ParsePayload(draft.CommandPayloadJson);
        if (!payload.Succeeded || payload.Value is null)
        {
            return Result<AiActionDraftExecutionResult>.Fail(payload.Error ?? InvalidPayloadMessage);
        }

        var dueAtUtc = payload.Value.DueInHours.HasValue
            ? _clock.UtcNow.AddHours(Math.Clamp(payload.Value.DueInHours.Value, 1, 24 * 90))
            : payload.Value.DueAtUtc;

        var result = await _tasks.CreateAsync(new CreateInternalFollowUpTaskCommand(
            draft.BusinessId,
            draft.FeatureAreaCode,
            draft.TargetEntityType,
            draft.TargetEntityId.Value,
            payload.Value.Title,
            payload.Value.Description,
            payload.Value.Priority,
            dueAtUtc,
            payload.Value.AssignedToUserId,
            draft.Id,
            payload.Value.MetadataJson,
            actorUserId), ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return Result<AiActionDraftExecutionResult>.Fail(result.Error ?? "AI follow-up task creation failed.");
        }

        return Result<AiActionDraftExecutionResult>.Ok(new AiActionDraftExecutionResult(
            SuccessSummary,
            "InternalFollowUpTask",
            result.Value));
    }

    private static Result<AiInternalFollowUpTaskPayload> ParsePayload(string? json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<AiInternalFollowUpTaskPayload>(
                string.IsNullOrWhiteSpace(json) ? "{}" : json,
                PayloadJsonOptions);
            if (payload is null)
            {
                return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task payload is required.");
            }

            if (FoundationInputNormalizer.Required(payload.Title) is null)
            {
                return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task title is required.");
            }

            if (!Enum.IsDefined(payload.Priority))
            {
                return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task priority is invalid.");
            }

            if (payload.DueAtUtc.HasValue && payload.DueInHours.HasValue)
            {
                return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task must use either dueAtUtc or dueInHours, not both.");
            }

            if (FoundationInputNormalizer.LooksSensitive(payload.Title) ||
                FoundationInputNormalizer.LooksSensitive(payload.Description) ||
                FoundationInputNormalizer.LooksSensitive(payload.MetadataJson))
            {
                return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task payload must be secret-free.");
            }

            return Result<AiInternalFollowUpTaskPayload>.Ok(payload);
        }
        catch (JsonException)
        {
            return Result<AiInternalFollowUpTaskPayload>.Fail("AI follow-up task payload JSON is invalid.");
        }
    }
}

public sealed class AiInternalFollowUpTaskPayload
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public InternalFollowUpTaskPriority Priority { get; set; } = InternalFollowUpTaskPriority.Normal;
    public DateTime? DueAtUtc { get; set; }
    public int? DueInHours { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? MetadataJson { get; set; }
}
