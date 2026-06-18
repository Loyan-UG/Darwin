using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Integration;

public sealed class SyncConflict : BaseEntity
{
    public Guid SyncStateId { get; set; }
    public SyncState? SyncState { get; set; }
    public Guid ExternalSystemId { get; set; }
    public ExternalSystem? ExternalSystem { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncDirection Direction { get; set; } = SyncDirection.Unknown;
    public SyncConflictStatus Status { get; set; } = SyncConflictStatus.Open;
    public SyncConflictResolution Resolution { get; set; } = SyncConflictResolution.None;
    public string ConflictKey { get; set; } = string.Empty;
    public string? FieldPath { get; set; }
    public string? DarwinValueSummary { get; set; }
    public string? ExternalValueSummary { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
