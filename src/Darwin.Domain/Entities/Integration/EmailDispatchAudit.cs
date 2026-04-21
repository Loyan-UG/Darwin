using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Integration
{
    /// <summary>
    /// Append-only audit entry for phase-1 email delivery attempts.
    /// Used for operational visibility until the full Communication Core logging pipeline exists.
    /// </summary>
    public sealed class EmailDispatchAudit : BaseEntity
    {
        public string Provider { get; set; } = "SMTP";
        public string? FlowKey { get; set; }
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string? IntendedRecipientEmail { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? ProviderMessageId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
    }
}
