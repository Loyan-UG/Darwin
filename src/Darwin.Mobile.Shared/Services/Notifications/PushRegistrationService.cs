using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Models.Notifications;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Notifications;

/// <summary>
/// Default implementation of push-device registration service.
/// </summary>
public sealed class PushRegistrationService : IPushRegistrationService
{
    private readonly IApiClient _api;

    public PushRegistrationService(IApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<Result<PushDeviceRegistrationClientModel>> RegisterDeviceAsync(
        string deviceId,
        MobileDevicePlatform platform,
        string? pushToken,
        bool notificationsEnabled,
        string? appVersion,
        string? deviceModel,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Result<PushDeviceRegistrationClientModel>.Fail("DeviceId is required.");
        }

        var request = new RegisterPushDeviceRequest
        {
            DeviceId = deviceId.Trim(),
            Platform = platform,
            PushToken = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim(),
            NotificationsEnabled = notificationsEnabled,
            AppVersion = string.IsNullOrWhiteSpace(appVersion) ? null : appVersion.Trim(),
            DeviceModel = string.IsNullOrWhiteSpace(deviceModel) ? null : deviceModel.Trim()
        };

        var response = await _api.PostResultAsync<RegisterPushDeviceRequest, RegisterPushDeviceResponse>(
            ApiRoutes.Notifications.RegisterDevice,
            request,
            ct).ConfigureAwait(false);

        if (!response.Succeeded || response.Value is null)
        {
            return Result<PushDeviceRegistrationClientModel>.Fail(response.Error ?? "Failed to register push device.");
        }

        return Result<PushDeviceRegistrationClientModel>.Ok(new PushDeviceRegistrationClientModel
        {
            DeviceId = response.Value.DeviceId,
            RegisteredAtUtc = response.Value.RegisteredAtUtc
        });
    }
}
