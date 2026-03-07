using System;
using System.Threading;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Stores latest platform push token in memory + preferences for cross-component access.
/// </summary>
internal static class PushTokenRuntimeState
{
    private const string PushTokenStorageKey = "consumer.push.current-token.v1";
    private static readonly object Sync = new();
    private static string? _currentPushToken;

    public static void SetPushToken(string? pushToken)
    {
        var normalized = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim();

        lock (Sync)
        {
            _currentPushToken = normalized;

            if (normalized is null)
            {
                Preferences.Default.Remove(PushTokenStorageKey);
            }
            else
            {
                Preferences.Default.Set(PushTokenStorageKey, normalized);
            }
        }
    }

    public static string? GetPushToken()
    {
        lock (Sync)
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
}
