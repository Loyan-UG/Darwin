using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Production push-token provider that reads real platform state from FCM/APNs runtime bridges.
/// </summary>
/// <remarks>
/// Thread-safety rules:
/// - This provider never touches UI-bound properties directly.
/// - It only reads platform notification settings and token cache managed by platform callbacks.
/// </remarks>
public sealed class ConsumerPlatformPushTokenProvider : IConsumerPushTokenProvider
{
    public async Task<Result<ConsumerPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var notificationsEnabled = await GetNotificationPermissionStateAsync(cancellationToken)
                .ConfigureAwait(false);

            var pushToken = await ResolvePlatformPushTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<ConsumerPushTokenState>.Ok(new ConsumerPushTokenState
            {
                PushToken = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim(),
                NotificationsEnabled = notificationsEnabled
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<ConsumerPushTokenState>.Fail("Could not resolve platform push token state.");
        }
    }

    private static async Task<bool> GetNotificationPermissionStateAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
        return status == PermissionStatus.Granted;
#elif IOS || MACCATALYST
        return await ApplePushRuntimeBridge.AreNotificationsEnabledAsync(cancellationToken).ConfigureAwait(false);
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return true;
#endif
    }

    private static async Task<string?> ResolvePlatformPushTokenAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        return await AndroidFcmRuntimeBridge.GetTokenAsync(cancellationToken).ConfigureAwait(false);
#elif IOS || MACCATALYST
        return await ApplePushRuntimeBridge.GetTokenAsync(cancellationToken).ConfigureAwait(false);
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return null;
#endif
    }
}
