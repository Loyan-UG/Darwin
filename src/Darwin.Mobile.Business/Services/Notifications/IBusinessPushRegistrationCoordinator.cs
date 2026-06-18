using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Coordinates best-effort push-device registration for the Business app lifecycle.
/// </summary>
public interface IBusinessPushRegistrationCoordinator
{
    Task<Result> TryRegisterCurrentDeviceAsync(CancellationToken cancellationToken);
    void ResetCachedRegistrationState();
}

public interface IBusinessPushRuntimeInfo
{
    MobileDevicePlatform Platform { get; }
    string? AppVersion { get; }
    string? DeviceModel { get; }
}

public interface IBusinessPushRegistrationStateStore
{
    string GetLastRegistrationSignature();
    void SetLastRegistrationSignature(string signature);
    void Reset();
}
