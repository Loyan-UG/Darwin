using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class InternalFollowUpTask : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string FeatureAreaCode { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InternalFollowUpTaskStatus Status { get; set; } = InternalFollowUpTaskStatus.Open;
    public InternalFollowUpTaskPriority Priority { get; set; } = InternalFollowUpTaskPriority.Normal;
    public DateTime? DueAtUtc { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? SourceAiActionDraftId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CompletionNotes { get; set; }
    public string? CancellationReason { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
