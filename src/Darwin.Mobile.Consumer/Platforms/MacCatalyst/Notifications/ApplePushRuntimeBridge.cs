using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UserNotifications;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// iOS/MacCatalyst runtime bridge for APNs token + notification authorization state.
/// </summary>
internal static class ApplePushRuntimeBridge
{
    private static readonly object Sync = new();
    private static string? _cachedToken;

    public static void SetDeviceToken(NSData deviceToken)
    {
        if (deviceToken is null)
        {
            return;
        }

        var bytes = deviceToken.ToArray();
        var token = BitConverter.ToString(bytes).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

        lock (Sync)
        {
            _cachedToken = token;
        }

        PushTokenRuntimeState.SetPushToken(token);
    }

    public static Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (Sync)
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken))
            {
                return Task.FromResult<string?>(_cachedToken);
            }
        }

        return Task.FromResult(PushTokenRuntimeState.GetPushToken());
    }

    public static async Task<bool> AreNotificationsEnabledAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync().ConfigureAwait(false);
        return settings.AuthorizationStatus is UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional or UNAuthorizationStatus.Ephemeral;
    }

    public static async Task RequestAuthorizationAndRegisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
        var (approved, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options).ConfigureAwait(false);
        if (!approved)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            UIKit.UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }).ConfigureAwait(false);
    }
}
