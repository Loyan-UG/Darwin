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
/// - Pulls push token state from <see cref="IConsumerPushTokenProvider"/>.
/// - Skips duplicate registrations when payload has not changed.
/// - Does not block login/startup on failures.
/// </remarks>
public sealed class ConsumerPushRegistrationCoordinator : IConsumerPushRegistrationCoordinator
{
    private const string DeviceIdStorageKey = "consumer.push.device-id.v1";
    private const string LastRegistrationSignatureStorageKey = "consumer.push.last-registration-signature.v1";

    private readonly IPushRegistrationService _pushRegistrationService;
    private readonly ITokenStore _tokenStore;
    private readonly IConsumerPushTokenProvider _tokenProvider;

    public ConsumerPushRegistrationCoordinator(
        IPushRegistrationService pushRegistrationService,
        ITokenStore tokenStore,
        IConsumerPushTokenProvider tokenProvider)
    {
        _pushRegistrationService = pushRegistrationService ?? throw new ArgumentNullException(nameof(pushRegistrationService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public async Task<Result> TryRegisterCurrentDeviceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result.Fail("No access token is available for push-device registration.");
        }

        var pushTokenStateResult = await _tokenProvider.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!pushTokenStateResult.Succeeded || pushTokenStateResult.Value is null)
        {
            return Result.Fail(pushTokenStateResult.Error ?? "Could not resolve push-token state.");
        }

        var pushTokenState = pushTokenStateResult.Value;
        var deviceId = GetOrCreateDeviceId();
        var platform = MapPlatform(DeviceInfo.Current.Platform);
        var appVersion = AppInfo.Current?.VersionString;
        var deviceModel = DeviceInfo.Current?.Model;

        var signature = BuildRegistrationSignature(
            deviceId,
            platform,
            pushTokenState.PushToken,
            pushTokenState.NotificationsEnabled,
            appVersion,
            deviceModel);

        var previousSignature = Preferences.Default.Get(LastRegistrationSignatureStorageKey, string.Empty);
        if (string.Equals(previousSignature, signature, StringComparison.Ordinal))
        {
            return Result.Ok();
        }

        var result = await _pushRegistrationService
            .RegisterDeviceAsync(
                deviceId: deviceId,
                platform: platform,
                pushToken: pushTokenState.PushToken,
                notificationsEnabled: pushTokenState.NotificationsEnabled,
                appVersion: appVersion,
                deviceModel: deviceModel,
                ct: cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return Result.Fail(result.Error ?? "Push-device registration request failed.");
        }

        Preferences.Default.Set(LastRegistrationSignatureStorageKey, signature);
        return Result.Ok();
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

    private static string BuildRegistrationSignature(
        string deviceId,
        MobileDevicePlatform platform,
        string? pushToken,
        bool notificationsEnabled,
        string? appVersion,
        string? deviceModel)
    {
        return string.Join("|",
            deviceId,
            platform.ToString(),
            pushToken ?? string.Empty,
            notificationsEnabled ? "1" : "0",
            appVersion ?? string.Empty,
            deviceModel ?? string.Empty);
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
