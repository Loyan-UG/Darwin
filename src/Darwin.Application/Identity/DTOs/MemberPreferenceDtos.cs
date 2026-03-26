namespace Darwin.Application.Identity.DTOs;

/// <summary>
/// Application-facing projection of the current user's privacy and communication preferences.
/// </summary>
public sealed class MemberPreferencesDto
{
    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets a value indicating whether aggregate marketing consent is granted.</summary>
    public bool MarketingConsent { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional email is allowed.</summary>
    public bool AllowEmailMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional SMS is allowed.</summary>
    public bool AllowSmsMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional WhatsApp messages are allowed.</summary>
    public bool AllowWhatsAppMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional push notifications are allowed.</summary>
    public bool AllowPromotionalPushNotifications { get; set; }

    /// <summary>Gets or sets a value indicating whether optional analytics tracking is allowed.</summary>
    public bool AllowOptionalAnalyticsTracking { get; set; }

    /// <summary>Gets or sets the UTC timestamp when terms were accepted, if recorded.</summary>
    public DateTime? AcceptsTermsAtUtc { get; set; }
}

/// <summary>
/// Application command payload for updating the current user's privacy and communication preferences.
/// </summary>
public sealed class UpdateMemberPreferencesDto
{
    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets a value indicating whether aggregate marketing consent is granted.</summary>
    public bool MarketingConsent { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional email is allowed.</summary>
    public bool AllowEmailMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional SMS is allowed.</summary>
    public bool AllowSmsMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional WhatsApp messages are allowed.</summary>
    public bool AllowWhatsAppMarketing { get; set; }

    /// <summary>Gets or sets a value indicating whether promotional push notifications are allowed.</summary>
    public bool AllowPromotionalPushNotifications { get; set; }

    /// <summary>Gets or sets a value indicating whether optional analytics tracking is allowed.</summary>
    public bool AllowOptionalAnalyticsTracking { get; set; }
}
