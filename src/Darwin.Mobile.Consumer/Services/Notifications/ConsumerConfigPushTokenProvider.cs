using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;
using Microsoft.Extensions.Configuration;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Configuration-backed push token provider for local/dev validation.
/// </summary>
/// <remarks>
/// Intended for environments where native FCM/APNs bridge is not yet wired.
/// Reads values from configuration section <c>PushNotifications</c>:
/// - <c>TestPushToken</c>
/// - <c>NotificationsEnabled</c>
/// </remarks>
public sealed class ConsumerConfigPushTokenProvider : IConsumerPushTokenProvider
{
    private readonly IConfiguration _configuration;

    public ConsumerConfigPushTokenProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public Task<Result<ConsumerPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var section = _configuration.GetSection("PushNotifications");
        var pushToken = section["TestPushToken"];

        var notificationsEnabled = true;
        if (bool.TryParse(section["NotificationsEnabled"], out var parsed))
        {
            notificationsEnabled = parsed;
        }

        return Task.FromResult(Result<ConsumerPushTokenState>.Ok(new ConsumerPushTokenState
        {
            PushToken = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim(),
            NotificationsEnabled = notificationsEnabled
        }));
    }
}
