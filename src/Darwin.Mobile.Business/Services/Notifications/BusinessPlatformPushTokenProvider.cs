using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Production push-token provider that reads real platform state from FCM runtime bridges.
/// </summary>
public sealed class BusinessPlatformPushTokenProvider : IBusinessPushTokenProvider
{
    public async Task<Result<BusinessPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var notificationsEnabled = await GetNotificationPermissionStateAsync(cancellationToken).ConfigureAwait(false);
            var pushToken = await ResolvePlatformPushTokenAsync(cancellationToken).ConfigureAwait(false);

            return Result<BusinessPushTokenState>.Ok(new BusinessPushTokenState
            {
                PushToken = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim(),
                NotificationsEnabled = notificationsEnabled
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Result<BusinessPushTokenState>.Ok(new BusinessPushTokenState
            {
                PushToken = null,
                NotificationsEnabled = false
            });
        }
    }

    private static async Task<bool> GetNotificationPermissionStateAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
        return status == PermissionStatus.Granted;
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return false;
#endif
    }

    private static async Task<string?> ResolvePlatformPushTokenAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        return await AndroidFcmRuntimeBridge.GetTokenAsync(cancellationToken).ConfigureAwait(false);
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return null;
#endif
    }
}
