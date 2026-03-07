using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Application.Abstractions.Notifications;

/// <summary>
/// Sends an inactive-user reminder using the configured outbound notification channel.
/// </summary>
public interface IInactiveReminderDispatcher
{
    /// <summary>
    /// Dispatches one reminder message for the specified user/device context.
    /// </summary>
    Task<Result> DispatchAsync(
        Guid userId,
        string destinationDeviceId,
        int inactiveDays,
        CancellationToken ct);
}
