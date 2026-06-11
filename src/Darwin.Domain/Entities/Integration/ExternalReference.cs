using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Integration;

public sealed class ExternalReference : BaseEntity
{
    public Guid ExternalSystemId { get; set; }
    public ExternalSystem? ExternalSystem { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public ExternalReferenceKind ReferenceKind { get; set; } = ExternalReferenceKind.Primary;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalDisplayId { get; set; }
    public SourceOfTruth SourceOfTruth { get; set; } = SourceOfTruth.Unknown;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
