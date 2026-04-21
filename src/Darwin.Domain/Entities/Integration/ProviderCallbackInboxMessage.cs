using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration;

/// <summary>
/// Persisted inbound provider callback awaiting async processing.
/// </summary>
public sealed class ProviderCallbackInboxMessage : BaseEntity
{
    public string Provider { get; set; } = string.Empty;
    public string CallbackType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
