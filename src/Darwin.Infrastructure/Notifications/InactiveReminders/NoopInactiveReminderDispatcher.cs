using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Notifications.InactiveReminders;

/// <summary>
/// Default inactive reminder dispatcher.
/// This implementation is intentionally non-delivering and only logs attempts
/// until a concrete push provider sender is integrated on the server side.
/// </summary>
public sealed class NoopInactiveReminderDispatcher : IInactiveReminderDispatcher
{
    private readonly ILogger<NoopInactiveReminderDispatcher> _logger;

    public NoopInactiveReminderDispatcher(ILogger<NoopInactiveReminderDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result> DispatchAsync(
        Guid userId,
        string destinationDeviceId,
        string pushToken,
        string platform,
        int inactiveDays,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Inactive reminder dispatch is not configured yet. UserId={UserId}, DeviceId={DeviceId}, Platform={Platform}, InactiveDays={InactiveDays}.",
            userId,
            destinationDeviceId,
            platform,
            inactiveDays);

        return Task.FromResult(Result.Fail("Inactive reminder dispatch provider is not configured."));
    }
}
