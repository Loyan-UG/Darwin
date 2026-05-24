using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Coordinates best-effort push-device registration for the Consumer app lifecycle.
/// </summary>
public interface IConsumerPushRegistrationCoordinator
{
    /// <summary>
    /// Attempts to register/update the current installation in backend device registry.
    /// </summary>
    Task<Result> TryRegisterCurrentDeviceAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Clears local registration cache so next successful login/refresh forces re-registration.
    /// </summary>
    void ResetCachedRegistrationState();
}

/// <summary>
/// Provides runtime device/app metadata without forcing tests through MAUI static singletons.
/// </summary>
public interface IConsumerPushRuntimeInfo
{
    /// <summary>
    /// Gets the backend platform value for this installation.
    /// </summary>
    MobileDevicePlatform Platform { get; }

    /// <summary>
    /// Gets the installed application version, when available.
    /// </summary>
    string? AppVersion { get; }

    /// <summary>
    /// Gets the device model, when available.
    /// </summary>
    string? DeviceModel { get; }
}

/// <summary>
/// Persists the local duplicate-registration signature.
/// </summary>
public interface IConsumerPushRegistrationStateStore
{
    /// <summary>
    /// Gets the last successfully registered signature.
    /// </summary>
    string GetLastRegistrationSignature();

    /// <summary>
    /// Saves the last successfully registered signature.
    /// </summary>
    void SetLastRegistrationSignature(string signature);

    /// <summary>
    /// Clears cached registration state so the next login/startup registers again.
    /// </summary>
    void Reset();
}
