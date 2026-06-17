using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class InternalFollowUpTaskService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public InternalFollowUpTaskService(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result<Guid>> CreateAsync(CreateInternalFollowUpTaskCommand command, CancellationToken ct = default)
    {
        var validation = ValidateCreate(command);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error!);
        }

        if (command.SourceAiActionDraftId.HasValue)
        {
            var existingId = await _db.Set<InternalFollowUpTask>()
                .Where(x => x.SourceAiActionDraftId == command.SourceAiActionDraftId.Value && !x.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (existingId != Guid.Empty)
            {
                return Result<Guid>.Ok(existingId);
            }
        }

        var task = new InternalFollowUpTask
        {
            BusinessId = command.BusinessId,
            FeatureAreaCode = FoundationInputNormalizer.Key(command.FeatureAreaCode)!,
            TargetEntityType = FoundationInputNormalizer.Required(command.TargetEntityType)!,
            TargetEntityId = command.TargetEntityId,
            Title = FoundationInputNormalizer.Required(command.Title)!,
            Description = FoundationInputNormalizer.Optional(command.Description),
            Priority = command.Priority,
            DueAtUtc = command.DueAtUtc,
            AssignedToUserId = command.AssignedToUserId,
            SourceAiActionDraftId = command.SourceAiActionDraftId,
            Status = InternalFollowUpTaskStatus.Open,
            MetadataJson = BuildMetadata(command.MetadataJson, command.SourceAiActionDraftId)
        };

        _db.Set<InternalFollowUpTask>().Add(task);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(task, "internal_follow_up_task.created", $"internal_follow_up_task.created:{task.Id:N}", command.ActorUserId, "Internal follow-up task created", ct).ConfigureAwait(false);
        return Result<Guid>.Ok(task.Id);
    }

    public async Task<Result> UpdateAsync(UpdateInternalFollowUpTaskCommand command, CancellationToken ct = default)
    {
        if (command.Id == Guid.Empty)
        {
            return Result.Fail("Internal follow-up task id is required.");
        }

        var task = await _db.Set<InternalFollowUpTask>()
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (task is null)
        {
            return Result.Fail("Internal follow-up task was not found.");
        }

        if (!MatchesRowVersion(task.RowVersion, command.RowVersion))
        {
            return Result.Fail("Internal follow-up task was modified by another user.");
        }

        if (task.Status is InternalFollowUpTaskStatus.Completed or InternalFollowUpTaskStatus.Cancelled)
        {
            return Result.Fail("Completed or cancelled internal follow-up tasks cannot be updated.");
        }

        var title = FoundationInputNormalizer.Required(command.Title);
        if (title is null)
        {
            return Result.Fail("Internal follow-up task title is required.");
        }

        if (!Enum.IsDefined(command.Priority) ||
            FoundationInputNormalizer.LooksSensitive(title) ||
            FoundationInputNormalizer.LooksSensitive(command.Description) ||
            FoundationInputNormalizer.LooksSensitive(command.MetadataJson))
        {
            return Result.Fail("Internal follow-up task fields must be valid and secret-free.");
        }

        task.Title = title;
        task.Description = FoundationInputNormalizer.Optional(command.Description);
        task.Priority = command.Priority;
        task.DueAtUtc = command.DueAtUtc;
        task.AssignedToUserId = command.AssignedToUserId;
        task.MetadataJson = BuildMetadata(command.MetadataJson, task.SourceAiActionDraftId);
        if (command.MarkInProgress && task.Status == InternalFollowUpTaskStatus.Open)
        {
            task.Status = InternalFollowUpTaskStatus.InProgress;
            task.StartedAtUtc ??= _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(task, "internal_follow_up_task.updated", $"internal_follow_up_task.updated:{task.Id:N}:{task.ModifiedAtUtc:O}", command.ActorUserId, "Internal follow-up task updated", ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> CompleteAsync(InternalFollowUpTaskLifecycleCommand command, CancellationToken ct = default)
        => await CompleteOrCancelAsync(command, complete: true, ct).ConfigureAwait(false);

    public async Task<Result> CancelAsync(InternalFollowUpTaskLifecycleCommand command, CancellationToken ct = default)
        => await CompleteOrCancelAsync(command, complete: false, ct).ConfigureAwait(false);

    private async Task<Result> CompleteOrCancelAsync(InternalFollowUpTaskLifecycleCommand command, bool complete, CancellationToken ct)
    {
        if (command.Id == Guid.Empty || command.ActorUserId == Guid.Empty)
        {
            return Result.Fail("Internal follow-up task and actor are required.");
        }

        var task = await _db.Set<InternalFollowUpTask>()
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (task is null)
        {
            return Result.Fail("Internal follow-up task was not found.");
        }

        if (!MatchesRowVersion(task.RowVersion, command.RowVersion))
        {
            return Result.Fail("Internal follow-up task was modified by another user.");
        }

        if (task.Status is InternalFollowUpTaskStatus.Completed or InternalFollowUpTaskStatus.Cancelled)
        {
            return Result.Fail("Internal follow-up task is already closed.");
        }

        if (FoundationInputNormalizer.LooksSensitive(command.Reason))
        {
            return Result.Fail("Internal follow-up task reason must be secret-free.");
        }

        var now = _clock.UtcNow;
        if (complete)
        {
            task.Status = InternalFollowUpTaskStatus.Completed;
            task.CompletedAtUtc = now;
            task.CompletedByUserId = command.ActorUserId;
            task.CompletionNotes = FoundationInputNormalizer.Optional(command.Reason);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await RecordEventAsync(task, "internal_follow_up_task.completed", $"internal_follow_up_task.completed:{task.Id:N}", command.ActorUserId, "Internal follow-up task completed", ct).ConfigureAwait(false);
        }
        else
        {
            var reason = FoundationInputNormalizer.Required(command.Reason);
            if (reason is null)
            {
                return Result.Fail("Cancellation reason is required.");
            }

            task.Status = InternalFollowUpTaskStatus.Cancelled;
            task.CancelledAtUtc = now;
            task.CancelledByUserId = command.ActorUserId;
            task.CancellationReason = reason;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await RecordEventAsync(task, "internal_follow_up_task.cancelled", $"internal_follow_up_task.cancelled:{task.Id:N}", command.ActorUserId, "Internal follow-up task cancelled", ct).ConfigureAwait(false);
        }

        return Result.Ok();
    }

    private async Task RecordEventAsync(InternalFollowUpTask task, string eventType, string eventKey, Guid? actorUserId, string title, CancellationToken ct)
    {
        if (_events is null)
        {
            return;
        }

        var result = await _events.AddEventAsync(new AddBusinessEventCommand(
            task.BusinessId,
            "InternalFollowUpTask",
            task.Id,
            eventType,
            eventKey,
            _clock.UtcNow,
            actorUserId,
            BusinessEventSource.Automation,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            title,
            PayloadJson: JsonSerializer.Serialize(new
            {
                task.Id,
                task.BusinessId,
                task.FeatureAreaCode,
                task.TargetEntityType,
                task.TargetEntityId,
                task.Status,
                task.Priority,
                task.DueAtUtc,
                task.AssignedToUserId,
                task.SourceAiActionDraftId
            })), ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error ?? "Internal follow-up task event failed.");
        }
    }

    private static Result ValidateCreate(CreateInternalFollowUpTaskCommand command)
    {
        if (command.TargetEntityId == Guid.Empty || command.ActorUserId == Guid.Empty)
        {
            return Result.Fail("Internal follow-up task target and actor are required.");
        }

        if (FoundationInputNormalizer.Key(command.FeatureAreaCode) is null ||
            FoundationInputNormalizer.Required(command.TargetEntityType) is null ||
            FoundationInputNormalizer.Required(command.Title) is null)
        {
            return Result.Fail("Internal follow-up task requires feature area, target entity, and title.");
        }

        if (!Enum.IsDefined(command.Priority) ||
            FoundationInputNormalizer.LooksSensitive(command.Title) ||
            FoundationInputNormalizer.LooksSensitive(command.Description) ||
            FoundationInputNormalizer.LooksSensitive(command.MetadataJson))
        {
            return Result.Fail("Internal follow-up task fields must be valid and secret-free.");
        }

        return Result.Ok();
    }

    private static string BuildMetadata(string? metadataJson, Guid? sourceAiActionDraftId)
    {
        var normalized = FoundationInputNormalizer.Json(metadataJson);
        if (sourceAiActionDraftId is null)
        {
            return normalized;
        }

        return JsonSerializer.Serialize(new
        {
            source = "ai-action-handoff",
            sourceAiActionDraftId,
            metadata = JsonSerializer.Deserialize<JsonElement>(normalized)
        });
    }

    private static bool MatchesRowVersion(byte[] current, byte[]? expected)
    {
        if (expected is null || expected.Length == 0 || current.Length == 0)
        {
            return true;
        }

        return current.SequenceEqual(expected);
    }
}

public sealed record CreateInternalFollowUpTaskCommand(
    Guid? BusinessId,
    string? FeatureAreaCode,
    string? TargetEntityType,
    Guid TargetEntityId,
    string? Title,
    string? Description,
    InternalFollowUpTaskPriority Priority,
    DateTime? DueAtUtc,
    Guid? AssignedToUserId,
    Guid? SourceAiActionDraftId,
    string? MetadataJson,
    Guid ActorUserId);

public sealed record UpdateInternalFollowUpTaskCommand(
    Guid Id,
    byte[]? RowVersion,
    string? Title,
    string? Description,
    InternalFollowUpTaskPriority Priority,
    DateTime? DueAtUtc,
    Guid? AssignedToUserId,
    bool MarkInProgress,
    string? MetadataJson,
    Guid ActorUserId);

public sealed record InternalFollowUpTaskLifecycleCommand(
    Guid Id,
    byte[]? RowVersion,
    Guid ActorUserId,
    string? Reason);
