using System;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Shared.Services.Privacy;

/// <summary>
/// Preference-backed storage for optional privacy choices that remain revocable inside the app.
/// </summary>
public sealed class OptionalPrivacyPreferencesStore : IOptionalPrivacyPreferencesStore
{
    private const string PromotionalPushKey = "privacy.optional.promotional-push.v1";
    private const string OptionalAnalyticsKey = "privacy.optional.analytics.v1";

    /// <inheritdoc />
    public OptionalPrivacyPreferences GetCurrent()
    {
        return new OptionalPrivacyPreferences
        {
            AllowPromotionalPushNotifications = Preferences.Default.Get(PromotionalPushKey, false),
            AllowOptionalAnalyticsTracking = Preferences.Default.Get(OptionalAnalyticsKey, false)
        };
    }

    /// <inheritdoc />
    public void Save(OptionalPrivacyPreferences preferences)
    {
        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        Preferences.Default.Set(PromotionalPushKey, preferences.AllowPromotionalPushNotifications);
        Preferences.Default.Set(OptionalAnalyticsKey, preferences.AllowOptionalAnalyticsTracking);
    }
}
