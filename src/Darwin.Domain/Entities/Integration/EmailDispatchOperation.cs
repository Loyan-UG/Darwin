using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration;

/// <summary>
/// Persisted outbound email-dispatch operation awaiting async provider processing.
/// </summary>
public sealed class EmailDispatchOperation : BaseEntity
{
    public string Provider { get; set; } = "SMTP";
    public string RecipientEmail { get; set; } = string.Empty;
    public string? IntendedRecipientEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
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
