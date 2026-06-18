using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class BusinessEvent : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EventKey { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public BusinessEventSource Source { get; set; } = BusinessEventSource.System;
    public BusinessEventSeverity Severity { get; set; } = BusinessEventSeverity.Info;
    public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}
