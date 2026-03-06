using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Default provider used until native push SDK wiring is added.
/// </summary>
public sealed class ConsumerNoopPushTokenProvider : IConsumerPushTokenProvider
{
    public Task<Result<ConsumerPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<ConsumerPushTokenState>.Ok(new ConsumerPushTokenState
        {
            PushToken = null,
            NotificationsEnabled = true
        }));
    }
}
