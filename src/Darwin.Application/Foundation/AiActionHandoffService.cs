using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public interface IAiActionDraftExecutor
{
    string FeatureAreaCode { get; }
    string CommandType { get; }
    bool CanExecute(AiActionDraft draft);
    Task<Result<AiActionDraftExecutionResult>> ExecuteAsync(AiActionDraft draft, Guid actorUserId, CancellationToken ct = default);
}

public sealed class AiActionHandoffService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    private readonly IReadOnlyList<IAiActionDraftExecutor> _executors;

    public AiActionHandoffService(
        IAppDbContext db,
        IClock clock,
        IEnumerable<IAiActionDraftExecutor> executors,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
        _executors = executors?.ToArray() ?? [];
    }

    public async Task<Result<AiActionHandoffResultDto>> ExecuteApprovedDraftAsync(
        ExecuteAiActionDraftCommand command,
        CancellationToken ct = default)
    {
        if (command.ActionDraftId == Guid.Empty || command.ActorUserId == Guid.Empty)
        {
            return Result<AiActionHandoffResultDto>.Fail("AI action draft and actor are required.");
        }

        var draft = await _db.Set<AiActionDraft>()
            .FirstOrDefaultAsync(x => x.Id == command.ActionDraftId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (draft is null)
        {
            return Result<AiActionHandoffResultDto>.Fail("AI action draft was not found.");
        }

        if (draft.Status != AiActionDraftStatus.Approved)
        {
            return Result<AiActionHandoffResultDto>.Fail("Only approved AI action drafts can be handed off for execution.");
        }

        if (draft.ExecutedAtUtc.HasValue)
        {
            return Result<AiActionHandoffResultDto>.Ok(new AiActionHandoffResultDto
            {
                ActionDraftId = draft.Id,
                ExecutedAtUtc = draft.ExecutedAtUtc.Value,
                ExecutionEventId = draft.ExecutionEventId,
                AlreadyExecuted = true
            });
        }

        if (!MatchesRowVersion(draft.RowVersion, command.RowVersion))
        {
            return Result<AiActionHandoffResultDto>.Fail("AI action draft was modified by another user.");
        }

        if (draft.RiskLevel == AiActionRiskLevel.High)
        {
            return Result<AiActionHandoffResultDto>.Fail("High-risk AI action drafts require a module-specific execution policy.");
        }

        if (FoundationInputNormalizer.LooksSensitive(command.OperatorReason) ||
            FoundationInputNormalizer.LooksSensitive(draft.CommandPayloadJson) ||
            FoundationInputNormalizer.LooksSensitive(draft.Summary))
        {
            return Result<AiActionHandoffResultDto>.Fail("AI action handoff content must not contain sensitive data.");
        }

        var executor = ResolveExecutor(draft);
        if (executor is null)
        {
            return Result<AiActionHandoffResultDto>.Fail("No AI action executor is registered for this approved draft.");
        }

        var execution = await executor.ExecuteAsync(draft, command.ActorUserId, ct).ConfigureAwait(false);
        if (!execution.Succeeded || execution.Value is null)
        {
            return Result<AiActionHandoffResultDto>.Fail(SafeError(execution.Error));
        }

        if (FoundationInputNormalizer.LooksSensitive(execution.Value.SafeSummary) ||
            FoundationInputNormalizer.LooksSensitive(execution.Value.ReferenceEntityType))
        {
            return Result<AiActionHandoffResultDto>.Fail("AI action executor returned sensitive execution metadata.");
        }

        var now = _clock.UtcNow;
        Guid? eventId = null;
        if (_events is not null)
        {
            var eventResult = await _events.AddEventAsync(new AddBusinessEventCommand(
                draft.BusinessId,
                "AiActionDraft",
                draft.Id,
                "ai.action_draft.executed",
                $"ai.action_draft.executed:{draft.Id:N}",
                now,
                command.ActorUserId,
                BusinessEventSource.Automation,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                "AI action draft executed",
                Summary: FoundationInputNormalizer.Optional(execution.Value.SafeSummary),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    draft.Id,
                    draft.BusinessId,
                    draft.FeatureAreaCode,
                    draft.TargetEntityType,
                    draft.TargetEntityId,
                    draft.CommandType,
                    draft.RiskLevel,
                    execution.Value.ReferenceEntityType,
                    execution.Value.ReferenceEntityId
                })), ct).ConfigureAwait(false);
            if (!eventResult.Succeeded)
            {
                return Result<AiActionHandoffResultDto>.Fail(eventResult.Error ?? "AI action handoff event failed.");
            }

            eventId = eventResult.Value;
        }

        draft.ExecutedAtUtc = now;
        draft.ExecutionEventId = eventId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<AiActionHandoffResultDto>.Ok(new AiActionHandoffResultDto
        {
            ActionDraftId = draft.Id,
            ExecutedAtUtc = now,
            ExecutionEventId = eventId,
            AlreadyExecuted = false,
            SafeSummary = FoundationInputNormalizer.Optional(execution.Value.SafeSummary),
            ReferenceEntityType = FoundationInputNormalizer.Optional(execution.Value.ReferenceEntityType),
            ReferenceEntityId = execution.Value.ReferenceEntityId
        });
    }

    private IAiActionDraftExecutor? ResolveExecutor(AiActionDraft draft)
        => _executors.FirstOrDefault(x =>
            MatchesRegistration(x.FeatureAreaCode, draft.FeatureAreaCode) &&
            MatchesRegistration(x.CommandType, draft.CommandType) &&
            x.CanExecute(draft));

    internal static bool MatchesRegistration(string registered, string actual)
    {
        var normalizedRegistered = FoundationInputNormalizer.Key(registered);
        return normalizedRegistered == "*" ||
               string.Equals(normalizedRegistered, FoundationInputNormalizer.Key(actual), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRowVersion(byte[] current, byte[]? expected)
    {
        if (expected is null || expected.Length == 0 || current.Length == 0)
        {
            return true;
        }

        return current.SequenceEqual(expected);
    }

    private static string SafeError(string? error)
        => FoundationInputNormalizer.LooksSensitive(error)
            ? "AI action handoff failed with a sensitive error that was not stored."
            : FoundationInputNormalizer.Optional(error) ?? "AI action handoff failed.";
}

public sealed record ExecuteAiActionDraftCommand(
    Guid ActionDraftId,
    Guid ActorUserId,
    byte[]? RowVersion = null,
    string? OperatorReason = null);

public sealed record AiActionDraftExecutionResult(
    string? SafeSummary = null,
    string? ReferenceEntityType = null,
    Guid? ReferenceEntityId = null);

public sealed class AiActionHandoffResultDto
{
    public Guid ActionDraftId { get; set; }
    public DateTime ExecutedAtUtc { get; set; }
    public Guid? ExecutionEventId { get; set; }
    public bool AlreadyExecuted { get; set; }
    public string? SafeSummary { get; set; }
    public string? ReferenceEntityType { get; set; }
    public Guid? ReferenceEntityId { get; set; }
}
