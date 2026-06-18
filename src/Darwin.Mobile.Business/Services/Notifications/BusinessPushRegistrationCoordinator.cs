using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services.Notifications;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Best-effort push-device registration coordinator for the Business app.
/// </summary>
public sealed class BusinessPushRegistrationCoordinator : IBusinessPushRegistrationCoordinator
{
    private const string LastRegistrationSignatureStorageKey = "business.push.last-registration-signature.v1";

    private readonly IPushRegistrationService _pushRegistrationService;
    private readonly ITokenStore _tokenStore;
    private readonly IBusinessNotificationPermissionService _notificationPermissionService;
    private readonly IBusinessPushTokenProvider _tokenProvider;
    private readonly IDeviceIdProvider _deviceIdProvider;
    private readonly IBusinessPushRuntimeInfo _runtimeInfo;
    private readonly IBusinessPushRegistrationStateStore _registrationStateStore;
    private int _notificationPermissionRequestAttempted;

    public BusinessPushRegistrationCoordinator(
        IPushRegistrationService pushRegistrationService,
        ITokenStore tokenStore,
        IBusinessNotificationPermissionService notificationPermissionService,
        IBusinessPushTokenProvider tokenProvider,
        IDeviceIdProvider deviceIdProvider,
        IBusinessPushRuntimeInfo runtimeInfo,
        IBusinessPushRegistrationStateStore registrationStateStore)
    {
        _pushRegistrationService = pushRegistrationService ?? throw new ArgumentNullException(nameof(pushRegistrationService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _notificationPermissionService = notificationPermissionService ?? throw new ArgumentNullException(nameof(notificationPermissionService));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _deviceIdProvider = deviceIdProvider ?? throw new ArgumentNullException(nameof(deviceIdProvider));
        _runtimeInfo = runtimeInfo ?? throw new ArgumentNullException(nameof(runtimeInfo));
        _registrationStateStore = registrationStateStore ?? throw new ArgumentNullException(nameof(registrationStateStore));
    }

    public async Task<Result> TryRegisterCurrentDeviceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenSnapshot = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(tokenSnapshot.AccessToken))
        {
            return Result.Fail("No access token is available for push-device registration.");
        }

        if (Interlocked.Exchange(ref _notificationPermissionRequestAttempted, 1) == 0)
        {
            _ = await _notificationPermissionService.EnsurePermissionAsync(cancellationToken).ConfigureAwait(false);
        }

        var pushTokenStateResult = await _tokenProvider.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!pushTokenStateResult.Succeeded || pushTokenStateResult.Value is null)
        {
            return Result.Fail(pushTokenStateResult.Error ?? "Could not resolve push-token state.");
        }

        var pushTokenState = pushTokenStateResult.Value;
        var deviceId = await _deviceIdProvider.GetDeviceIdAsync().ConfigureAwait(false);
        var platform = _runtimeInfo.Platform;
        var appVersion = _runtimeInfo.AppVersion;
        var deviceModel = _runtimeInfo.DeviceModel;

        var signature = BuildRegistrationSignature(
            deviceId,
            platform,
            pushTokenState.PushToken,
            pushTokenState.NotificationsEnabled,
            appVersion,
            deviceModel);

        if (string.Equals(_registrationStateStore.GetLastRegistrationSignature(), signature, StringComparison.Ordinal))
        {
            return Result.Ok();
        }

        var result = await _pushRegistrationService
            .RegisterDeviceAsync(
                deviceId,
                platform,
                pushTokenState.PushToken,
                pushTokenState.NotificationsEnabled,
                appVersion,
                deviceModel,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return Result.Fail(result.Error ?? "Push-device registration request failed.");
        }

        _registrationStateStore.SetLastRegistrationSignature(signature);
        return Result.Ok();
    }

    public void ResetCachedRegistrationState()
    {
        _registrationStateStore.Reset();
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
        if (platform == DevicePlatform.Android)
        {
            return MobileDevicePlatform.Android;
        }

        if (platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst)
        {
            return MobileDevicePlatform.iOS;
        }

        return MobileDevicePlatform.Unknown;
    }

    public sealed class MauiBusinessPushRuntimeInfo : IBusinessPushRuntimeInfo
    {
        public MobileDevicePlatform Platform => MapPlatform(DeviceInfo.Current.Platform);

        public string? AppVersion => AppInfo.Current?.VersionString;

        public string? DeviceModel => DeviceInfo.Current?.Model;
    }

    public sealed class PreferencesBusinessPushRegistrationStateStore : IBusinessPushRegistrationStateStore
    {
        public string GetLastRegistrationSignature()
            => Preferences.Default.Get(LastRegistrationSignatureStorageKey, string.Empty);

        public void SetLastRegistrationSignature(string signature)
            => Preferences.Default.Set(LastRegistrationSignatureStorageKey, signature);

        public void Reset()
            => Preferences.Default.Remove(LastRegistrationSignatureStorageKey);
    }
}
