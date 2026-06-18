using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class AuditTrail : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditTrailAction Action { get; set; } = AuditTrailAction.Custom;
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? BusinessEventId { get; set; }
    public string? Reason { get; set; }
    public string? CorrelationId { get; set; }
    public string ChangeSetJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}
