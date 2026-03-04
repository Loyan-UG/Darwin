using System.Threading;
using System.Threading.Tasks;
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
}
