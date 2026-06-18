using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class AiSensitiveFieldPolicy : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string PurposeKey { get; set; } = string.Empty;
    public AiSensitiveDataCategory DataCategory { get; set; } = AiSensitiveDataCategory.General;
    public AiSensitivityLevel SensitivityLevel { get; set; } = AiSensitivityLevel.Confidential;
    public AiAccessDecision Decision { get; set; } = AiAccessDecision.Deny;
    public bool IsActive { get; set; } = true;
    public string? RedactionRule { get; set; }
    public string? Description { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AiRecommendation : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string RecommendationType { get; set; } = string.Empty;
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public AiRecommendationStatus Status { get; set; } = AiRecommendationStatus.Open;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewReason { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AiActionDraft : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public Guid? RecommendationId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid? TargetEntityId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public string CommandPayloadJson { get; set; } = "{}";
    public string Summary { get; set; } = string.Empty;
    public AiActionRiskLevel RiskLevel { get; set; } = AiActionRiskLevel.Medium;
    public AiActionDraftStatus Status { get; set; } = AiActionDraftStatus.Draft;
    public Guid? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public Guid? ExecutionEventId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AiActionApproval : BaseEntity
{
    public Guid AiActionDraftId { get; set; }
    public AiActionApprovalDecision Decision { get; set; }
    public Guid DecidedByUserId { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}
