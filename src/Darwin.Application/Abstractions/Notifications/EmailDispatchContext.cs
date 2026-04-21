using System;

namespace Darwin.Application.Abstractions.Notifications
{
    /// <summary>
    /// Optional metadata attached to phase-1 email sends so operational audits can be correlated.
    /// </summary>
    public sealed class EmailDispatchContext
    {
        public string? FlowKey { get; set; }
        public string? TemplateKey { get; set; }
        public string? CorrelationKey { get; set; }
        public Guid? BusinessId { get; set; }
        public string? IntendedRecipientEmail { get; set; }
    }
}
