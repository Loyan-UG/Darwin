using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Notifications;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Best-effort push-device registration coordinator for Consumer app.
/// </summary>
/// <remarks>
/// Current scope:
/// - Registers a stable device id + platform + app/device metadata.
/// - Uses null push token until native push-token provider is integrated.
/// - Does not block login/startup on failures.
/// </remarks>
public sealed class ConsumerPushRegistrationCoordinator : IConsumerPushRegistrationCoordinator
{
    private const string DeviceIdStorageKey = "consumer.push.device-id.v1";

    private readonly IPushRegistrationService _pushRegistrationService;
    private readonly ITokenStore _tokenStore;

    public ConsumerPushRegistrationCoordinator(
        IPushRegistrationService pushRegistrationService,
        ITokenStore tokenStore)
    {
        _pushRegistrationService = pushRegistrationService ?? throw new ArgumentNullException(nameof(pushRegistrationService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public async Task<Result> TryRegisterCurrentDeviceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result.Fail("No access token is available for push-device registration.");
        }

        var deviceId = GetOrCreateDeviceId();
        var platform = MapPlatform(DeviceInfo.Current.Platform);
        var appVersion = AppInfo.Current?.VersionString;
        var deviceModel = DeviceInfo.Current?.Model;

        var result = await _pushRegistrationService
            .RegisterDeviceAsync(
                deviceId: deviceId,
                platform: platform,
                pushToken: null,
                notificationsEnabled: true,
                appVersion: appVersion,
                deviceModel: deviceModel,
                ct: cancellationToken)
            .ConfigureAwait(false);

        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Error ?? "Push-device registration request failed.");
    }

    private static string GetOrCreateDeviceId()
    {
        var existing = Preferences.Default.Get(DeviceIdStorageKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var created = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdStorageKey, created);
        return created;
    }

    private static MobileDevicePlatform MapPlatform(DevicePlatform platform)
    {
        return platform switch
        {
            DevicePlatform.Android => MobileDevicePlatform.Android,
            DevicePlatform.iOS => MobileDevicePlatform.Ios,
            DevicePlatform.MacCatalyst => MobileDevicePlatform.Ios,
            _ => MobileDevicePlatform.Unknown
        };
    }
}
