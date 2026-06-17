using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class AiGovernanceService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public AiGovernanceService(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result<Guid>> UpsertSensitiveFieldPolicyAsync(UpsertAiSensitiveFieldPolicyCommand command, CancellationToken ct = default)
    {
        var entityType = NormalizeToken(command.EntityType);
        var fieldPath = NormalizeFieldPath(command.FieldPath);
        var purposeKey = NormalizeToken(command.PurposeKey) ?? "default";
        if (entityType is null || fieldPath is null)
        {
            return Result<Guid>.Fail("AI sensitive field policy requires entity type and field path.");
        }

        if (!Enum.IsDefined(command.DataCategory) ||
            !Enum.IsDefined(command.SensitivityLevel) ||
            !Enum.IsDefined(command.Decision))
        {
            return Result<Guid>.Fail("AI sensitive field policy enum values are invalid.");
        }

        if (command.Decision == AiAccessDecision.AllowRaw &&
            (command.SensitivityLevel is AiSensitivityLevel.Restricted or AiSensitivityLevel.Secret ||
             command.DataCategory is AiSensitiveDataCategory.Credential or AiSensitiveDataCategory.ProviderPayload or AiSensitiveDataCategory.DocumentContent or AiSensitiveDataCategory.InvoiceArchive))
        {
            return Result<Guid>.Fail("Raw AI access is not allowed for restricted, secret, credential, provider, document-content, or archive fields.");
        }

        var metadataJson = NormalizeJson(command.MetadataJson);
        if (ContainsSensitiveText(command.RedactionRule) ||
            ContainsSensitiveText(command.Description) ||
            ContainsSensitiveText(metadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in AI policy metadata.");
        }

        var policy = await _db.Set<AiSensitiveFieldPolicy>()
            .FirstOrDefaultAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.EntityType == entityType &&
                x.FieldPath == fieldPath &&
                x.PurposeKey == purposeKey &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (policy is null)
        {
            policy = new AiSensitiveFieldPolicy
            {
                BusinessId = command.BusinessId,
                EntityType = entityType,
                FieldPath = fieldPath,
                PurposeKey = purposeKey
            };
            _db.Set<AiSensitiveFieldPolicy>().Add(policy);
        }

        policy.DataCategory = command.DataCategory;
        policy.SensitivityLevel = command.SensitivityLevel;
        policy.Decision = command.Decision;
        policy.IsActive = command.IsActive;
        policy.RedactionRule = FoundationInputNormalizer.Optional(command.RedactionRule);
        policy.Description = FoundationInputNormalizer.Optional(command.Description);
        policy.MetadataJson = metadataJson;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(command.BusinessId, "AiSensitiveFieldPolicy", policy.Id, "ai.policy.upserted", $"ai.policy.upserted:{policy.Id:N}", command.ActorUserId, new { policy.Id, policy.BusinessId, policy.EntityType, policy.FieldPath, policy.PurposeKey, policy.DataCategory, policy.SensitivityLevel, policy.Decision }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(policy.Id);
    }

    public async Task<AiAccessDecision> EvaluateFieldAccessAsync(string? entityType, string? fieldPath, string? purposeKey, Guid? businessId = null, CancellationToken ct = default)
    {
        var normalizedEntity = NormalizeToken(entityType);
        var normalizedField = NormalizeFieldPath(fieldPath);
        var normalizedPurpose = NormalizeToken(purposeKey) ?? "default";
        if (normalizedEntity is null || normalizedField is null)
        {
            return AiAccessDecision.Deny;
        }

        var policy = await _db.Set<AiSensitiveFieldPolicy>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == normalizedEntity &&
                x.FieldPath == normalizedField &&
                x.PurposeKey == normalizedPurpose &&
                x.IsActive &&
                !x.IsDeleted &&
                (x.BusinessId == businessId || x.BusinessId == null))
            .OrderByDescending(x => x.BusinessId.HasValue)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return policy?.Decision ?? AiAccessDecision.Deny;
    }

    public async Task<Result<Guid>> CreateRecommendationAsync(CreateAiRecommendationCommand command, CancellationToken ct = default)
    {
        var featureAreaCode = NormalizeToken(command.FeatureAreaCode);
        var recommendationType = NormalizeToken(command.RecommendationType);
        var title = FoundationInputNormalizer.Required(command.Title);
        var summary = FoundationInputNormalizer.Required(command.Summary);
        var rationale = FoundationInputNormalizer.Required(command.Rationale);
        if (featureAreaCode is null || recommendationType is null || title is null || summary is null || rationale is null)
        {
            return Result<Guid>.Fail("AI recommendation requires feature area, type, title, summary, and rationale.");
        }

        if (command.ConfidenceScore is < 0 or > 100)
        {
            return Result<Guid>.Fail("AI recommendation confidence must be between 0 and 100.");
        }

        var sourceEntityType = NormalizeToken(command.SourceEntityType);
        if (command.SourceEntityId.HasValue && sourceEntityType is null)
        {
            return Result<Guid>.Fail("AI recommendation source entity type is required when source id is provided.");
        }

        var metadataJson = NormalizeJson(command.MetadataJson);
        if (ContainsSensitiveText(title) || ContainsSensitiveText(summary) || ContainsSensitiveText(rationale) || ContainsSensitiveText(metadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in AI recommendation content.");
        }

        var recommendation = new AiRecommendation
        {
            BusinessId = command.BusinessId,
            FeatureAreaCode = featureAreaCode,
            RecommendationType = recommendationType,
            SourceEntityType = sourceEntityType,
            SourceEntityId = command.SourceEntityId,
            Title = title,
            Summary = summary,
            Rationale = rationale,
            ConfidenceScore = command.ConfidenceScore,
            ExpiresAtUtc = command.ExpiresAtUtc,
            MetadataJson = metadataJson
        };

        _db.Set<AiRecommendation>().Add(recommendation);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(command.BusinessId, "AiRecommendation", recommendation.Id, "ai.recommendation.created", $"ai.recommendation.created:{recommendation.Id:N}", command.ActorUserId, new { recommendation.Id, recommendation.BusinessId, recommendation.FeatureAreaCode, recommendation.RecommendationType, recommendation.ConfidenceScore }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(recommendation.Id);
    }

    public async Task<Result<Guid>> CreateActionDraftAsync(CreateAiActionDraftCommand command, CancellationToken ct = default)
    {
        var featureAreaCode = NormalizeToken(command.FeatureAreaCode);
        var targetEntityType = NormalizeToken(command.TargetEntityType);
        var commandType = NormalizeToken(command.CommandType);
        var summary = FoundationInputNormalizer.Required(command.Summary);
        var commandPayloadJson = NormalizeJson(command.CommandPayloadJson);
        if (featureAreaCode is null || targetEntityType is null || commandType is null || summary is null)
        {
            return Result<Guid>.Fail("AI action draft requires feature area, target entity type, command type, and summary.");
        }

        if (!Enum.IsDefined(command.RiskLevel))
        {
            return Result<Guid>.Fail("AI action draft risk level is invalid.");
        }

        if (command.RecommendationId.HasValue)
        {
            var recommendationExists = await _db.Set<AiRecommendation>()
                .AnyAsync(x => x.Id == command.RecommendationId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (!recommendationExists)
            {
                return Result<Guid>.Fail("AI recommendation was not found.");
            }
        }

        var metadataJson = NormalizeJson(command.MetadataJson);
        if (ContainsSensitiveText(summary) || ContainsSensitiveText(commandPayloadJson) || ContainsSensitiveText(metadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in AI action draft content.");
        }

        var draft = new AiActionDraft
        {
            BusinessId = command.BusinessId,
            RecommendationId = command.RecommendationId,
            FeatureAreaCode = featureAreaCode,
            TargetEntityType = targetEntityType,
            TargetEntityId = command.TargetEntityId,
            CommandType = commandType,
            CommandPayloadJson = commandPayloadJson,
            Summary = summary,
            RiskLevel = command.RiskLevel,
            MetadataJson = metadataJson
        };

        _db.Set<AiActionDraft>().Add(draft);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(command.BusinessId, "AiActionDraft", draft.Id, "ai.action_draft.created", $"ai.action_draft.created:{draft.Id:N}", command.ActorUserId, new { draft.Id, draft.BusinessId, draft.FeatureAreaCode, draft.TargetEntityType, draft.CommandType, draft.RiskLevel, draft.Status }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(draft.Id);
    }

    public async Task<Result> SubmitActionDraftForApprovalAsync(Guid draftId, Guid actorUserId, CancellationToken ct = default)
        => await SubmitActionDraftForApprovalAsync(draftId, actorUserId, null, ct).ConfigureAwait(false);

    public async Task<Result> SubmitActionDraftForApprovalAsync(Guid draftId, Guid actorUserId, byte[]? rowVersion, CancellationToken ct = default)
    {
        if (draftId == Guid.Empty || actorUserId == Guid.Empty)
        {
            return Result.Fail("AI action draft and actor are required.");
        }

        var draft = await _db.Set<AiActionDraft>().FirstOrDefaultAsync(x => x.Id == draftId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (draft is null)
        {
            return Result.Fail("AI action draft was not found.");
        }

        if (draft.Status != AiActionDraftStatus.Draft)
        {
            return Result.Fail("Only draft AI actions can be submitted for approval.");
        }

        if (!MatchesRowVersion(draft.RowVersion, rowVersion))
        {
            return Result.Fail("AI action draft was modified by another user.");
        }

        draft.Status = AiActionDraftStatus.PendingApproval;
        draft.SubmittedByUserId = actorUserId;
        draft.SubmittedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecordEventAsync(draft.BusinessId, "AiActionDraft", draft.Id, "ai.action_draft.submitted", $"ai.action_draft.submitted:{draft.Id:N}", actorUserId, new { draft.Id, draft.BusinessId, draft.Status }, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public Task<Result> ApproveActionDraftAsync(Guid draftId, Guid approverUserId, string reason, CancellationToken ct = default)
        => DecideActionDraftAsync(draftId, approverUserId, reason, AiActionApprovalDecision.Approved, ct);

    public Task<Result> ApproveActionDraftAsync(Guid draftId, Guid approverUserId, string reason, byte[]? rowVersion, CancellationToken ct = default)
        => DecideActionDraftAsync(draftId, approverUserId, reason, AiActionApprovalDecision.Approved, ct, rowVersion);

    public Task<Result> RejectActionDraftAsync(Guid draftId, Guid reviewerUserId, string reason, CancellationToken ct = default)
        => DecideActionDraftAsync(draftId, reviewerUserId, reason, AiActionApprovalDecision.Rejected, ct);
    
    public Task<Result> RejectActionDraftAsync(Guid draftId, Guid reviewerUserId, string reason, byte[]? rowVersion, CancellationToken ct = default)
        => DecideActionDraftAsync(draftId, reviewerUserId, reason, AiActionApprovalDecision.Rejected, ct, rowVersion);

    public async Task<Result> ReviewRecommendationAsync(Guid recommendationId, AiRecommendationStatus status, Guid reviewerUserId, string reason, byte[]? rowVersion = null, CancellationToken ct = default)
    {
        if (recommendationId == Guid.Empty || reviewerUserId == Guid.Empty)
        {
            return Result.Fail("AI recommendation and reviewer are required.");
        }

        if (status is not (AiRecommendationStatus.Accepted or AiRecommendationStatus.Dismissed or AiRecommendationStatus.Expired))
        {
            return Result.Fail("AI recommendation review status is invalid.");
        }

        var normalizedReason = FoundationInputNormalizer.Required(reason);
        if (normalizedReason is null || ContainsSensitiveText(normalizedReason))
        {
            return Result.Fail("AI recommendation review requires a safe reason.");
        }

        var recommendation = await _db.Set<AiRecommendation>()
            .FirstOrDefaultAsync(x => x.Id == recommendationId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (recommendation is null)
        {
            return Result.Fail("AI recommendation was not found.");
        }

        if (recommendation.Status != AiRecommendationStatus.Open)
        {
            return Result.Fail("Only open AI recommendations can be reviewed.");
        }

        if (!MatchesRowVersion(recommendation.RowVersion, rowVersion))
        {
            return Result.Fail("AI recommendation was modified by another user.");
        }

        recommendation.Status = status;
        recommendation.ReviewedAtUtc = _clock.UtcNow;
        recommendation.ReviewedByUserId = reviewerUserId;
        recommendation.ReviewReason = normalizedReason;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        var eventType = status switch
        {
            AiRecommendationStatus.Accepted => "ai.recommendation.accepted",
            AiRecommendationStatus.Dismissed => "ai.recommendation.dismissed",
            _ => "ai.recommendation.expired"
        };
        await RecordEventAsync(recommendation.BusinessId, "AiRecommendation", recommendation.Id, eventType, $"{eventType}:{recommendation.Id:N}", reviewerUserId, new { recommendation.Id, recommendation.BusinessId, recommendation.Status }, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<Result> DecideActionDraftAsync(Guid draftId, Guid actorUserId, string reason, AiActionApprovalDecision decision, CancellationToken ct, byte[]? rowVersion = null)
    {
        if (draftId == Guid.Empty || actorUserId == Guid.Empty)
        {
            return Result.Fail("AI action draft and actor are required.");
        }

        var normalizedReason = FoundationInputNormalizer.Required(reason);
        if (normalizedReason is null || ContainsSensitiveText(normalizedReason))
        {
            return Result.Fail("AI action approval requires a safe reason.");
        }

        var draft = await _db.Set<AiActionDraft>().FirstOrDefaultAsync(x => x.Id == draftId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (draft is null)
        {
            return Result.Fail("AI action draft was not found.");
        }

        if (draft.Status != AiActionDraftStatus.PendingApproval)
        {
            return Result.Fail("Only pending AI action drafts can be approved or rejected.");
        }

        if (!MatchesRowVersion(draft.RowVersion, rowVersion))
        {
            return Result.Fail("AI action draft was modified by another user.");
        }

        var now = _clock.UtcNow;
        draft.Status = decision == AiActionApprovalDecision.Approved ? AiActionDraftStatus.Approved : AiActionDraftStatus.Rejected;
        draft.ReviewReason = normalizedReason;
        if (decision == AiActionApprovalDecision.Approved)
        {
            draft.ApprovedAtUtc = now;
            draft.ApprovedByUserId = actorUserId;
        }
        else
        {
            draft.RejectedAtUtc = now;
            draft.RejectedByUserId = actorUserId;
        }

        _db.Set<AiActionApproval>().Add(new AiActionApproval
        {
            AiActionDraftId = draft.Id,
            Decision = decision,
            DecidedAtUtc = now,
            DecidedByUserId = actorUserId,
            Reason = normalizedReason
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        var eventType = decision == AiActionApprovalDecision.Approved ? "ai.action_draft.approved" : "ai.action_draft.rejected";
        await RecordEventAsync(draft.BusinessId, "AiActionDraft", draft.Id, eventType, $"{eventType}:{draft.Id:N}", actorUserId, new { draft.Id, draft.BusinessId, draft.Status, Decision = decision }, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task RecordEventAsync(Guid? businessId, string entityType, Guid entityId, string eventType, string eventKey, Guid? actorUserId, object payload, CancellationToken ct)
    {
        if (_events is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var result = await _events.AddEventAsync(new AddBusinessEventCommand(
            businessId,
            entityType,
            entityId,
            eventType,
            eventKey,
            now,
            actorUserId,
            BusinessEventSource.Automation,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            eventType,
            PayloadJson: JsonSerializer.Serialize(payload)), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error ?? "AiGovernanceEventFailed");
        }
    }

    private static string? NormalizeToken(string? value)
        => FoundationInputNormalizer.Key(value);

    private static string? NormalizeFieldPath(string? value)
        => FoundationInputNormalizer.Required(value)?.Trim().ToLowerInvariant();

    private static string NormalizeJson(string? value)
        => FoundationInputNormalizer.Json(value);

    private static bool ContainsSensitiveText(string? value)
        => FoundationInputNormalizer.LooksSensitive(value);

    private static bool MatchesRowVersion(byte[] current, byte[]? expected)
    {
        if (expected is null || expected.Length == 0 || current.Length == 0)
        {
            return true;
        }

        return current.SequenceEqual(expected);
    }
}

public sealed record UpsertAiSensitiveFieldPolicyCommand(
    Guid? BusinessId,
    string? EntityType,
    string? FieldPath,
    string? PurposeKey,
    AiSensitiveDataCategory DataCategory,
    AiSensitivityLevel SensitivityLevel,
    AiAccessDecision Decision,
    bool IsActive = true,
    string? RedactionRule = null,
    string? Description = null,
    string? MetadataJson = null,
    Guid? ActorUserId = null);

public sealed record CreateAiRecommendationCommand(
    Guid? BusinessId,
    string? FeatureAreaCode,
    string? RecommendationType,
    string? Title,
    string? Summary,
    string? Rationale,
    int ConfidenceScore,
    string? SourceEntityType = null,
    Guid? SourceEntityId = null,
    DateTime? ExpiresAtUtc = null,
    string? MetadataJson = null,
    Guid? ActorUserId = null);

public sealed record CreateAiActionDraftCommand(
    Guid? BusinessId,
    Guid? RecommendationId,
    string? FeatureAreaCode,
    string? TargetEntityType,
    Guid? TargetEntityId,
    string? CommandType,
    string? CommandPayloadJson,
    string? Summary,
    AiActionRiskLevel RiskLevel,
    string? MetadataJson = null,
    Guid? ActorUserId = null);
