using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Resolves current push-token state for the running Consumer installation.
/// </summary>
public interface IConsumerPushTokenProvider
{
    /// <summary>
    /// Gets current token + notification-permission state.
    /// </summary>
    Task<Result<ConsumerPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Immutable snapshot returned by <see cref="IConsumerPushTokenProvider"/>.
/// </summary>
public sealed class ConsumerPushTokenState
{
    /// <summary>
    /// Gets or sets platform push token (FCM/APNs).
    /// Can be null temporarily when provider initialization failed or permission is denied.
    /// </summary>
    public string? PushToken { get; init; }

    /// <summary>
    /// Gets or sets whether user enabled app notifications.
    /// </summary>
    public bool NotificationsEnabled { get; init; } = true;
}
