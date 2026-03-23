namespace Darwin.Mobile.Shared.Services.Privacy;

/// <summary>
/// Stores optional privacy-related choices that are intentionally independent from required legal acknowledgements.
/// </summary>
public sealed class OptionalPrivacyPreferences
{
    /// <summary>
    /// Gets or sets a value indicating whether the user opted in to promotional push notifications.
    /// </summary>
    public bool AllowPromotionalPushNotifications { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user opted in to optional analytics/tracking.
    /// </summary>
    public bool AllowOptionalAnalyticsTracking { get; set; }
}
