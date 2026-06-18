using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class Note : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Body { get; set; } = string.Empty;
    public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
    public bool IsPinned { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
