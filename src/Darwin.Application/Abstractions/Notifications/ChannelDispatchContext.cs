using System;

namespace Darwin.Application.Abstractions.Notifications
{
    /// <summary>
    /// Carries minimal flow metadata so non-email transports can emit operational audit rows.
    /// </summary>
    public sealed class ChannelDispatchContext
    {
        public string? FlowKey { get; init; }
        public string? TemplateKey { get; init; }
        public string? CorrelationKey { get; init; }
        public Guid? BusinessId { get; init; }
        public string? IntendedRecipientAddress { get; init; }
    }
}
