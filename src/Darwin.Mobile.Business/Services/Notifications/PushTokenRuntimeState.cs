using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.Services.Notifications;

internal static class PushTokenRuntimeState
{
    private const string PushTokenStorageKey = "business.push.current-token.v1";

    private static string? _currentPushToken;

    public static void SetPushToken(string? pushToken)
    {
        var normalized = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim();
        _currentPushToken = normalized;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            Preferences.Default.Remove(PushTokenStorageKey);
            return;
        }

        Preferences.Default.Set(PushTokenStorageKey, normalized);
    }

    public static string? GetPushToken()
    {
        if (!string.IsNullOrWhiteSpace(_currentPushToken))
        {
            return _currentPushToken;
        }

        var persisted = Preferences.Default.Get(PushTokenStorageKey, string.Empty);
        _currentPushToken = string.IsNullOrWhiteSpace(persisted) ? null : persisted;
        return _currentPushToken;
    }
}
