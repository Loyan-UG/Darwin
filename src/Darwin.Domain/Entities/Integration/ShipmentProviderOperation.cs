using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration;

/// <summary>
/// Persisted outbound shipment-provider operation awaiting async processing.
/// </summary>
public sealed class ShipmentProviderOperation : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
