using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Integration
{
    /// <summary>
    /// Outgoing webhook subscription configured by administrators for specific event types.
    /// </summary>
    public sealed class WebhookSubscription : BaseEntity
    {
        /// <summary>Event type to deliver (e.g., "order.created").</summary>
        public string EventType { get; set; } = string.Empty;
        /// <summary>HTTPS endpoint to post payloads to.</summary>
        public string CallbackUrl { get; set; } = string.Empty;
        /// <summary>Shared secret used to sign payload (HMAC-SHA256).</summary>
        public string Secret { get; set; } = string.Empty;
        /// <summary>Whether the subscription is active.</summary>
        public bool IsActive { get; set; } = true;
    }


    /// <summary>
    /// Delivery log for webhook attempts. Retries are manual in phase 1 (no background daemon).
    /// </summary>
    public sealed class WebhookDelivery : BaseEntity
    {
        public Guid SubscriptionId { get; set; }
        /// <summary>Reference to originating domain event id (if recorded).</summary>
        public Guid? EventRefId { get; set; }
        /// <summary>HTTP status code returned by the receiver, if any.</summary>
        public int? ResponseCode { get; set; }
        /// <summary>Current delivery status, e.g., Succeeded/Failed/Pending.</summary>
        public string Status { get; set; } = "Pending";
        /// <summary>Number of attempts made so far.</summary>
        public int RetryCount { get; set; }
        /// <summary>Last attempt time (UTC).</summary>
        public DateTime? LastAttemptAtUtc { get; set; }
        /// <summary>Payload integrity hash to support idempotency.</summary>
        public string? PayloadHash { get; set; }
        /// <summary>Idempotency key to deduplicate deliveries.</summary>
        public string? IdempotencyKey { get; set; }
    }
}