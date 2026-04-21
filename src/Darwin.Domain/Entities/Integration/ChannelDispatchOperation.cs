using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration;

/// <summary>
/// Persisted outbound channel-dispatch operation awaiting async provider processing.
/// </summary>
public sealed class ChannelDispatchOperation : BaseEntity
{
    public string Channel { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string? IntendedRecipientAddress { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string? FlowKey { get; set; }
    public string? TemplateKey { get; set; }
    public string? CorrelationKey { get; set; }
    public Guid? BusinessId { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
