namespace Darwin.Mobile.Shared.Services.Privacy;

/// <summary>
/// Persists optional privacy choices locally until a dedicated server-side privacy/preferences domain is available.
/// </summary>
public interface IOptionalPrivacyPreferencesStore
{
    /// <summary>
    /// Loads the current optional privacy preferences.
    /// </summary>
    /// <returns>The persisted preference snapshot, or default values when no preferences were saved yet.</returns>
    OptionalPrivacyPreferences GetCurrent();

    /// <summary>
    /// Persists the supplied optional privacy preferences.
    /// </summary>
    /// <param name="preferences">The preference snapshot to persist.</param>
    void Save(OptionalPrivacyPreferences preferences);
}
