using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Integration;

public sealed class SyncState : BaseEntity
{
    public Guid ExternalSystemId { get; set; }
    public ExternalSystem? ExternalSystem { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncDirection Direction { get; set; } = SyncDirection.Unknown;
    public SyncStateStatus Status { get; set; } = SyncStateStatus.NotSynced;
    public string SyncScope { get; set; } = "default";
    public DateTime? LastSuccessfulSyncAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSummary { get; set; }
    public string? RemoteVersion { get; set; }
    public string? LocalVersion { get; set; }
    public string MetadataJson { get; set; } = "{}";

    public ICollection<SyncConflict> Conflicts { get; set; } = new List<SyncConflict>();
}
