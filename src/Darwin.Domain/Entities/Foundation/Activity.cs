using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class Activity : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
    public string MetadataJson { get; set; } = "{}";
}
