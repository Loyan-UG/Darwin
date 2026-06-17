using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class GetAiGovernanceReviewPageHandler
{
    private readonly IAppDbContext _db;

    public GetAiGovernanceReviewPageHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AiGovernanceReviewPageDto> HandleAsync(AiGovernanceReviewQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var search = NormalizeSearch(query.Query);

        var recommendationsQuery = _db.Set<AiRecommendation>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);
        if (query.BusinessId.HasValue)
        {
            recommendationsQuery = recommendationsQuery.Where(x => x.BusinessId == query.BusinessId);
        }

        if (query.RecommendationStatus.HasValue)
        {
            recommendationsQuery = recommendationsQuery.Where(x => x.Status == query.RecommendationStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            recommendationsQuery = recommendationsQuery.Where(x =>
                x.Title.Contains(search) ||
                x.Summary.Contains(search) ||
                x.FeatureAreaCode.Contains(search) ||
                x.RecommendationType.Contains(search));
        }

        var draftsQuery = _db.Set<AiActionDraft>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);
        if (query.BusinessId.HasValue)
        {
            draftsQuery = draftsQuery.Where(x => x.BusinessId == query.BusinessId);
        }

        if (query.ActionDraftStatus.HasValue)
        {
            draftsQuery = draftsQuery.Where(x => x.Status == query.ActionDraftStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            draftsQuery = draftsQuery.Where(x =>
                x.Summary.Contains(search) ||
                x.FeatureAreaCode.Contains(search) ||
                x.CommandType.Contains(search) ||
                x.TargetEntityType.Contains(search));
        }

        var recommendationTotal = await recommendationsQuery.CountAsync(ct).ConfigureAwait(false);
        var draftTotal = await draftsQuery.CountAsync(ct).ConfigureAwait(false);
        var openRecommendations = await _db.Set<AiRecommendation>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && (!query.BusinessId.HasValue || x.BusinessId == query.BusinessId) && x.Status == AiRecommendationStatus.Open, ct)
            .ConfigureAwait(false);
        var pendingDrafts = await _db.Set<AiActionDraft>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && (!query.BusinessId.HasValue || x.BusinessId == query.BusinessId) && x.Status == AiActionDraftStatus.PendingApproval, ct)
            .ConfigureAwait(false);
        var approvedNotExecuted = await _db.Set<AiActionDraft>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && (!query.BusinessId.HasValue || x.BusinessId == query.BusinessId) && x.Status == AiActionDraftStatus.Approved && x.ExecutedAtUtc == null, ct)
            .ConfigureAwait(false);

        var recommendations = await recommendationsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AiRecommendationListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                FeatureAreaCode = x.FeatureAreaCode,
                RecommendationType = x.RecommendationType,
                Title = x.Title,
                Summary = x.Summary,
                ConfidenceScore = x.ConfidenceScore,
                Status = x.Status,
                ExpiresAtUtc = x.ExpiresAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var drafts = await draftsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AiActionDraftListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                RecommendationId = x.RecommendationId,
                FeatureAreaCode = x.FeatureAreaCode,
                TargetEntityType = x.TargetEntityType,
                TargetEntityId = x.TargetEntityId,
                CommandType = x.CommandType,
                Summary = x.Summary,
                RiskLevel = x.RiskLevel,
                Status = x.Status,
                SubmittedAtUtc = x.SubmittedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                RejectedAtUtc = x.RejectedAtUtc,
                ExecutedAtUtc = x.ExecutedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new AiGovernanceReviewPageDto
        {
            BusinessId = query.BusinessId,
            Query = search ?? string.Empty,
            RecommendationStatus = query.RecommendationStatus,
            ActionDraftStatus = query.ActionDraftStatus,
            Page = page,
            PageSize = pageSize,
            RecommendationTotal = recommendationTotal,
            ActionDraftTotal = draftTotal,
            Summary = new AiGovernanceReviewSummaryDto
            {
                OpenRecommendations = openRecommendations,
                PendingActionDrafts = pendingDrafts,
                ApprovedNotExecutedDrafts = approvedNotExecuted
            },
            Recommendations = recommendations,
            ActionDrafts = drafts
        };
    }

    private static string? NormalizeSearch(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetAiRecommendationDetailHandler
{
    private readonly IAppDbContext _db;

    public GetAiRecommendationDetailHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AiRecommendationDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var recommendation = await _db.Set<AiRecommendation>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new AiRecommendationDetailDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                FeatureAreaCode = x.FeatureAreaCode,
                RecommendationType = x.RecommendationType,
                SourceEntityType = x.SourceEntityType,
                SourceEntityId = x.SourceEntityId,
                Title = x.Title,
                Summary = x.Summary,
                Rationale = x.Rationale,
                ConfidenceScore = x.ConfidenceScore,
                Status = x.Status,
                ExpiresAtUtc = x.ExpiresAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc,
                ReviewedByUserId = x.ReviewedByUserId,
                ReviewReason = x.ReviewReason,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (recommendation is not null)
        {
            recommendation.RowVersionText = Convert.ToBase64String(recommendation.RowVersion);
        }

        return recommendation;
    }
}

public sealed class GetAiActionDraftDetailHandler
{
    private readonly IAppDbContext _db;
    private readonly IReadOnlyList<IAiActionDraftExecutor> _executors;

    public GetAiActionDraftDetailHandler(IAppDbContext db, IEnumerable<IAiActionDraftExecutor> executors)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _executors = executors?.ToArray() ?? [];
    }

    public async Task<AiActionDraftDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var draft = await _db.Set<AiActionDraft>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new AiActionDraftDetailDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                RecommendationId = x.RecommendationId,
                FeatureAreaCode = x.FeatureAreaCode,
                TargetEntityType = x.TargetEntityType,
                TargetEntityId = x.TargetEntityId,
                CommandType = x.CommandType,
                CommandPayloadJson = x.CommandPayloadJson,
                Summary = x.Summary,
                RiskLevel = x.RiskLevel,
                Status = x.Status,
                SubmittedByUserId = x.SubmittedByUserId,
                SubmittedAtUtc = x.SubmittedAtUtc,
                ApprovedByUserId = x.ApprovedByUserId,
                ApprovedAtUtc = x.ApprovedAtUtc,
                RejectedByUserId = x.RejectedByUserId,
                RejectedAtUtc = x.RejectedAtUtc,
                ReviewReason = x.ReviewReason,
                ExecutedAtUtc = x.ExecutedAtUtc,
                ExecutionEventId = x.ExecutionEventId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        draft.RowVersionText = Convert.ToBase64String(draft.RowVersion);
        draft.CanExecute = draft.Status == AiActionDraftStatus.Approved &&
                           draft.ExecutedAtUtc is null &&
                           draft.RiskLevel != AiActionRiskLevel.High &&
                           draft.TargetEntityId.HasValue &&
                           _executors.Any(x =>
                               AiActionHandoffService.MatchesRegistration(x.FeatureAreaCode, draft.FeatureAreaCode) &&
                               AiActionHandoffService.MatchesRegistration(x.CommandType, draft.CommandType));
        draft.ExecutionReadinessMessage = draft.CanExecute
            ? "ReadyForExecution"
            : ResolveExecutionReadinessMessage(draft);

        draft.Approvals = await _db.Set<AiActionApproval>()
            .AsNoTracking()
            .Where(x => x.AiActionDraftId == id && !x.IsDeleted)
            .OrderByDescending(x => x.DecidedAtUtc)
            .Select(x => new AiActionApprovalDto
            {
                Id = x.Id,
                Decision = x.Decision,
                DecidedByUserId = x.DecidedByUserId,
                DecidedAtUtc = x.DecidedAtUtc,
                Reason = x.Reason
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return draft;
    }

    private static string ResolveExecutionReadinessMessage(AiActionDraftDetailDto draft)
    {
        if (draft.ExecutedAtUtc.HasValue)
        {
            return "AlreadyExecuted";
        }

        if (draft.Status != AiActionDraftStatus.Approved)
        {
            return "ApprovalRequired";
        }

        if (draft.RiskLevel == AiActionRiskLevel.High)
        {
            return "HighRiskExecutionBlocked";
        }

        if (!draft.TargetEntityId.HasValue)
        {
            return "TargetRequired";
        }

        return "NoExecutorAvailable";
    }
}

public sealed class GetInternalFollowUpTasksPageHandler
{
    private readonly IAppDbContext _db;

    public GetInternalFollowUpTasksPageHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<InternalFollowUpTasksPageDto> HandleAsync(InternalFollowUpTasksQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var search = NormalizeSearch(query.Query);

        var tasksQuery = _db.Set<InternalFollowUpTask>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.BusinessId.HasValue)
        {
            tasksQuery = tasksQuery.Where(x => x.BusinessId == query.BusinessId);
        }

        if (query.Status.HasValue)
        {
            tasksQuery = tasksQuery.Where(x => x.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            tasksQuery = tasksQuery.Where(x =>
                x.Title.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)) ||
                x.FeatureAreaCode.Contains(search) ||
                x.TargetEntityType.Contains(search));
        }

        var total = await tasksQuery.CountAsync(ct).ConfigureAwait(false);
        var open = await CountByStatusAsync(query.BusinessId, InternalFollowUpTaskStatus.Open, ct).ConfigureAwait(false);
        var inProgress = await CountByStatusAsync(query.BusinessId, InternalFollowUpTaskStatus.InProgress, ct).ConfigureAwait(false);
        var overdue = await _db.Set<InternalFollowUpTask>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted &&
                             (!query.BusinessId.HasValue || x.BusinessId == query.BusinessId) &&
                             x.DueAtUtc.HasValue &&
                             x.DueAtUtc < DateTime.UtcNow &&
                             x.Status != InternalFollowUpTaskStatus.Completed &&
                             x.Status != InternalFollowUpTaskStatus.Cancelled, ct)
            .ConfigureAwait(false);

        var items = await tasksQuery
            .OrderBy(x => x.Status == InternalFollowUpTaskStatus.Completed || x.Status == InternalFollowUpTaskStatus.Cancelled)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueAtUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InternalFollowUpTaskListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                FeatureAreaCode = x.FeatureAreaCode,
                TargetEntityType = x.TargetEntityType,
                TargetEntityId = x.TargetEntityId,
                Title = x.Title,
                Status = x.Status,
                Priority = x.Priority,
                DueAtUtc = x.DueAtUtc,
                AssignedToUserId = x.AssignedToUserId,
                SourceAiActionDraftId = x.SourceAiActionDraftId,
                CreatedAtUtc = x.CreatedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new InternalFollowUpTasksPageDto
        {
            BusinessId = query.BusinessId,
            Query = search ?? string.Empty,
            Status = query.Status,
            Page = page,
            PageSize = pageSize,
            Total = total,
            OpenCount = open,
            InProgressCount = inProgress,
            OverdueCount = overdue,
            Items = items
        };
    }

    private Task<int> CountByStatusAsync(Guid? businessId, InternalFollowUpTaskStatus status, CancellationToken ct)
        => _db.Set<InternalFollowUpTask>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && (!businessId.HasValue || x.BusinessId == businessId) && x.Status == status, ct);

    private static string? NormalizeSearch(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetInternalFollowUpTaskDetailHandler
{
    private readonly IAppDbContext _db;

    public GetInternalFollowUpTaskDetailHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<InternalFollowUpTaskDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var dto = await _db.Set<InternalFollowUpTask>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new InternalFollowUpTaskDetailDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                FeatureAreaCode = x.FeatureAreaCode,
                TargetEntityType = x.TargetEntityType,
                TargetEntityId = x.TargetEntityId,
                Title = x.Title,
                Description = x.Description,
                Status = x.Status,
                Priority = x.Priority,
                DueAtUtc = x.DueAtUtc,
                AssignedToUserId = x.AssignedToUserId,
                SourceAiActionDraftId = x.SourceAiActionDraftId,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc,
                CompletedByUserId = x.CompletedByUserId,
                CancelledByUserId = x.CancelledByUserId,
                CompletionNotes = x.CompletionNotes,
                CancellationReason = x.CancellationReason,
                MetadataJson = x.MetadataJson,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (dto is not null)
        {
            dto.RowVersionText = Convert.ToBase64String(dto.RowVersion);
        }

        return dto;
    }
}

public sealed record AiGovernanceReviewQuery(
    Guid? BusinessId = null,
    string? Query = null,
    AiRecommendationStatus? RecommendationStatus = null,
    AiActionDraftStatus? ActionDraftStatus = null,
    int Page = 1,
    int PageSize = 20);

public sealed record InternalFollowUpTasksQuery(
    Guid? BusinessId = null,
    string? Query = null,
    InternalFollowUpTaskStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed class AiGovernanceReviewPageDto
{
    public Guid? BusinessId { get; set; }
    public string Query { get; set; } = string.Empty;
    public AiRecommendationStatus? RecommendationStatus { get; set; }
    public AiActionDraftStatus? ActionDraftStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int RecommendationTotal { get; set; }
    public int ActionDraftTotal { get; set; }
    public AiGovernanceReviewSummaryDto Summary { get; set; } = new();
    public List<AiRecommendationListItemDto> Recommendations { get; set; } = new();
    public List<AiActionDraftListItemDto> ActionDrafts { get; set; } = new();
}

public sealed class AiGovernanceReviewSummaryDto
{
    public int OpenRecommendations { get; set; }
    public int PendingActionDrafts { get; set; }
    public int ApprovedNotExecutedDrafts { get; set; }
}

public class AiRecommendationListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? BusinessId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string RecommendationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public AiRecommendationStatus Status { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AiRecommendationDetailDto : AiRecommendationListItemDto
{
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewReason { get; set; }
    public string RowVersionText { get; set; } = string.Empty;
}

public class AiActionDraftListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? BusinessId { get; set; }
    public Guid? RecommendationId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid? TargetEntityId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public AiActionRiskLevel RiskLevel { get; set; }
    public AiActionDraftStatus Status { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AiActionDraftDetailDto : AiActionDraftListItemDto
{
    public string CommandPayloadJson { get; set; } = "{}";
    public Guid? SubmittedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public string? ReviewReason { get; set; }
    public Guid? ExecutionEventId { get; set; }
    public bool CanExecute { get; set; }
    public string ExecutionReadinessMessage { get; set; } = string.Empty;
    public string RowVersionText { get; set; } = string.Empty;
    public List<AiActionApprovalDto> Approvals { get; set; } = new();
}

public sealed class AiActionApprovalDto
{
    public Guid Id { get; set; }
    public AiActionApprovalDecision Decision { get; set; }
    public Guid DecidedByUserId { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class InternalFollowUpTasksPageDto
{
    public Guid? BusinessId { get; set; }
    public string Query { get; set; } = string.Empty;
    public InternalFollowUpTaskStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int OverdueCount { get; set; }
    public List<InternalFollowUpTaskListItemDto> Items { get; set; } = new();
}

public class InternalFollowUpTaskListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? BusinessId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public InternalFollowUpTaskStatus Status { get; set; }
    public InternalFollowUpTaskPriority Priority { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? SourceAiActionDraftId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class InternalFollowUpTaskDetailDto : InternalFollowUpTaskListItemDto
{
    public string? Description { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CompletionNotes { get; set; }
    public string? CancellationReason { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string RowVersionText { get; set; } = string.Empty;
}
