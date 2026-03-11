using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.WebApi.Services;

/// <summary>
/// Periodic background worker that runs inactive-reminder orchestration batches.
/// </summary>
public sealed class InactiveReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<InactiveReminderWorkerOptions> _optionsMonitor;
    private readonly ILogger<InactiveReminderBackgroundService> _logger;

    public InactiveReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<InactiveReminderWorkerOptions> optionsMonitor,
        ILogger<InactiveReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inactive reminder worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            if (!options.Enabled)
            {
                await DelaySafeAsync(options.Interval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessInactiveReminderBatchHandler>();

                var result = await handler.HandleAsync(new ProcessInactiveReminderBatchDto
                {
                    InactiveThresholdDays = options.InactiveThresholdDays,
                    CooldownHours = options.CooldownHours,
                    MaxItems = options.MaxItemsPerRun
                }, stoppingToken).ConfigureAwait(false);

                if (!result.Succeeded || result.Value is null)
                {
                    _logger.LogWarning("Inactive reminder batch failed: {Error}", result.Error ?? "Unknown error");
                }
                else
                {
                    _logger.LogInformation(
                        "Inactive reminder batch completed. Evaluated={Evaluated}, Dispatched={Dispatched}, Suppressed={Suppressed}, Failed={Failed}",
                        result.Value.CandidatesEvaluated,
                        result.Value.DispatchedCount,
                        result.Value.SuppressedCount,
                        result.Value.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inactive reminder worker iteration failed unexpectedly.");
            }

            await DelaySafeAsync(options.Interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Inactive reminder worker stopped.");
    }

    /// <summary>
    /// Delays using validated interval bounds to avoid accidental tight loops.
    /// </summary>
    private static Task DelaySafeAsync(TimeSpan requestedInterval, CancellationToken ct)
    {
        var interval = requestedInterval < TimeSpan.FromMinutes(1)
            ? TimeSpan.FromMinutes(1)
            : requestedInterval;

        return Task.Delay(interval, ct);
    }
}
