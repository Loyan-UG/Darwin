using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Integration
{
    /// <summary>
    /// Append-only event stream entry for analytics and future CRM automations.
    /// </summary>
    public sealed class EventLog : BaseEntity
    {
        /// <summary>Event type name (e.g., PageView, AddToCart, Purchase, EmailOpened).</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>UTC timestamp when the event occurred on the client/server.</summary>
        public DateTime OccurredAtUtc { get; set; }
        /// <summary>Optional registered user id associated with the event.</summary>
        public Guid? UserId { get; set; }
        /// <summary>Optional anonymous id (cookie-based) used before authentication.</summary>
        public string? AnonymousId { get; set; }
        /// <summary>Optional session id for correlating events within a browsing session.</summary>
        public string? SessionId { get; set; }
        /// <summary>Additional event-specific properties serialized as JSON (e.g., {"query":"milk"}).</summary>
        public string PropertiesJson { get; set; } = "{}";
        /// <summary>Snapshot of UTM attributes at the time of the event, serialized as JSON.</summary>
        public string UtmSnapshotJson { get; set; } = "{}";
        /// <summary>Optional idempotency key used to de-duplicate events from retries.</summary>
        public string? IdempotencyKey { get; set; }
    }
}