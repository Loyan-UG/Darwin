using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Marketing
{
    /// <summary>
    /// Represents a marketing campaign used for mobile feed, promotions, and engagement automation.
    /// A campaign can be global (platform-wide) or scoped to a specific <see cref="BusinessId"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This entity is intentionally "content + targeting + schedule" only. Actual delivery attempts are tracked
    /// by <see cref="CampaignDelivery"/> to support retry, auditing and provider correlation.
    /// </para>
    /// <para>
    /// Content is kept simple (Title/Body/MediaUrl). Rich templating should be stored in JSON to avoid schema churn.
    /// </para>
    /// </remarks>
    public sealed class Campaign : BaseEntity
    {
        /// <summary>
        /// Optional business scope. When null, the campaign is considered platform-wide.
        /// </summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// Administrative internal name (not displayed to end users).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display title shown to users (feed card, push title, etc.).
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional short subtitle displayed under the title.
        /// </summary>
        public string? Subtitle { get; set; }

        /// <summary>
        /// Optional body content (plain text). Rich HTML should not be stored here.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Optional primary media URL (image/video preview). Must be absolute when provided.
        /// </summary>
        public string? MediaUrl { get; set; }

        /// <summary>
        /// Optional call-to-action URL. Must be absolute when provided.
        /// </summary>
        public string? LandingUrl { get; set; }

        /// <summary>
        /// Indicates which channels this campaign is allowed to use.
        /// Actual channel delivery attempts are stored in <see cref="CampaignDelivery"/>.
        /// </summary>
        public CampaignChannels Channels { get; set; } = CampaignChannels.InApp;

        /// <summary>
        /// Optional schedule window. When null, the campaign is always eligible if <see cref="IsActive"/> is true.
        /// </summary>
        public DateTime? StartsAtUtc { get; set; }

        /// <summary>
        /// Optional schedule end.
        /// </summary>
        public DateTime? EndsAtUtc { get; set; }

        /// <summary>
        /// Whether this campaign is enabled and eligible for selection.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON-based targeting rules (segments, proximity, favorites-only, inactive users, etc.).
        /// Keep it evolvable without schema changes.
        /// Examples: {"minVisits":3,"city":"Berlin","favoritesOnly":true}.
        /// </summary>
        public string TargetingJson { get; set; } = "{}";

        /// <summary>
        /// Optional JSON payload that can hold template keys, deep links, localization variants, etc.
        /// Examples: {"template":"promo-1","deeplink":"darwin://business/.."}.
        /// </summary>
        public string PayloadJson { get; set; } = "{}";
    }

    /// <summary>
    /// Tracks a single delivery attempt or planned delivery for a campaign.
    /// This entity supports retry, auditing and external provider correlation (e.g., push provider message id).
    /// </summary>
    public sealed class CampaignDelivery : BaseEntity
    {
        /// <summary>
        /// Campaign reference.
        /// </summary>
        public Guid CampaignId { get; set; }

        /// <summary>
        /// Optional recipient user. For anonymous deliveries (e.g., public feed), this may be null.
        /// </summary>
        public Guid? RecipientUserId { get; set; }

        /// <summary>
        /// Optional business scope for convenience indexing/reporting.
        /// </summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// Delivery channel used for this attempt (Push, InApp, Email, ...).
        /// </summary>
        public CampaignDeliveryChannel Channel { get; set; } = CampaignDeliveryChannel.InApp;

        /// <summary>
        /// Delivery status lifecycle.
        /// </summary>
        public CampaignDeliveryStatus Status { get; set; } = CampaignDeliveryStatus.Pending;

        /// <summary>
        /// Optional destination identifier (email address, device id, phone, etc.).
        /// For push, this could be a device installation id rather than the raw push token.
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// Number of attempts performed so far.
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// First attempt timestamp (UTC).
        /// </summary>
        public DateTime? FirstAttemptAtUtc { get; set; }

        /// <summary>
        /// Last attempt timestamp (UTC).
        /// </summary>
        public DateTime? LastAttemptAtUtc { get; set; }

        /// <summary>
        /// Optional response code returned by the downstream provider or HTTP gateway.
        /// </summary>
        public int? LastResponseCode { get; set; }

        /// <summary>
        /// Optional provider-specific identifier for troubleshooting and reconciliation.
        /// </summary>
        public string? ProviderMessageId { get; set; }

        /// <summary>
        /// Optional last error message (truncated in Application if needed).
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Idempotency key to prevent duplicate deliveries in case of retries or job restarts.
        /// </summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Optional payload hash (e.g., SHA-256) to support deduplication and audit.
        /// </summary>
        public string? PayloadHash { get; set; }
    }
}
