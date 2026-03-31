using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration
{
    /// <summary>
    /// Append-only audit entry for SMS and WhatsApp delivery attempts that are already live.
    /// This stays flow-scoped until a broader Communication Core outbox exists.
    /// </summary>
    public sealed class ChannelDispatchAudit : BaseEntity
    {
        public string Channel { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string RecipientAddress { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
    }
}
