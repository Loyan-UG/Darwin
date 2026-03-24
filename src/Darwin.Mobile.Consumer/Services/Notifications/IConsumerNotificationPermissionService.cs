using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Coordinates just-in-time disclosure and platform-specific notification permission requests for the Consumer app.
/// </summary>
public interface IConsumerNotificationPermissionService
{
    /// <summary>
    /// Ensures that notification permission has been requested with a prior in-app disclosure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated from the caller.</param>
    /// <returns>
    /// A successful result whose value indicates whether notification permission is now granted.
    /// The result fails only when the permission workflow itself encounters an unexpected runtime error.
    /// </returns>
    Task<Result<bool>> EnsurePermissionAsync(CancellationToken cancellationToken);
}
