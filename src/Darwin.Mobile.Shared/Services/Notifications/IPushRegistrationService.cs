using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Models.Notifications;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Notifications;

/// <summary>
/// Registers mobile device installations for push notification delivery.
/// </summary>
public interface IPushRegistrationService
{
    Task<Result<PushDeviceRegistrationClientModel>> RegisterDeviceAsync(
        string deviceId,
        MobileDevicePlatform platform,
        string? pushToken,
        bool notificationsEnabled,
        string? appVersion,
        string? deviceModel,
        CancellationToken ct);
}
