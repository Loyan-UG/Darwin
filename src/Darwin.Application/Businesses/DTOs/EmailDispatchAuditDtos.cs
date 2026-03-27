using System;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Lightweight row for phase-1 email delivery audit visibility.
    /// </summary>
    public sealed class EmailDispatchAuditListItemDto
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? FlowKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
    }
}
