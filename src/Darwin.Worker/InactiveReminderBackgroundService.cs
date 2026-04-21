using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

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
                    var evaluated = Math.Max(1, result.Value.CandidatesEvaluated);
                    var failedRatePercent = (result.Value.FailedCount * 100d) / evaluated;
                    var cooldownSuppressionRatePercent = (result.Value.SuppressedByCooldownCount * 100d) / evaluated;

                    _logger.LogInformation(
                        "Inactive reminder batch completed. Evaluated={Evaluated}, Dispatched={Dispatched}, Suppressed={Suppressed}, SuppressedByCooldown={SuppressedByCooldown}, SuppressedByMissingDestination={SuppressedByMissingDestination}, Failed={Failed}, FailedRatePercent={FailedRatePercent:F1}, CooldownSuppressionRatePercent={CooldownSuppressionRatePercent:F1}, FailureBreakdown={FailureBreakdown}, SuppressionBreakdown={SuppressionBreakdown}",
                        result.Value.CandidatesEvaluated,
                        result.Value.DispatchedCount,
                        result.Value.SuppressedCount,
                        result.Value.SuppressedByCooldownCount,
                        result.Value.SuppressedByMissingDestinationCount,
                        result.Value.FailedCount,
                        failedRatePercent,
                        cooldownSuppressionRatePercent,
                        FormatBreakdown(result.Value.FailureCodeCounts),
                        FormatBreakdown(result.Value.SuppressionCodeCounts));

                    if (failedRatePercent >= Math.Clamp(options.HighFailureRateWarningThresholdPercent, 0, 100))
                    {
                        _logger.LogWarning(
                            "Inactive reminder batch failure rate exceeded warning threshold. FailedRatePercent={FailedRatePercent:F1}, ThresholdPercent={ThresholdPercent}, Evaluated={Evaluated}, Failed={Failed}",
                            failedRatePercent,
                            options.HighFailureRateWarningThresholdPercent,
                            result.Value.CandidatesEvaluated,
                            result.Value.FailedCount);
                    }

                    if (cooldownSuppressionRatePercent >= Math.Clamp(options.HighCooldownSuppressionWarningThresholdPercent, 0, 100))
                    {
                        _logger.LogWarning(
                            "Inactive reminder batch cooldown suppression rate exceeded warning threshold. CooldownSuppressionRatePercent={CooldownSuppressionRatePercent:F1}, ThresholdPercent={ThresholdPercent}, Evaluated={Evaluated}, SuppressedByCooldown={SuppressedByCooldown}",
                            cooldownSuppressionRatePercent,
                            options.HighCooldownSuppressionWarningThresholdPercent,
                            result.Value.CandidatesEvaluated,
                            result.Value.SuppressedByCooldownCount);
                    }
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

    /// <summary>
    /// Formats per-code counters into a compact stable log string for remediation workflows.
    /// </summary>
    private static string FormatBreakdown(IReadOnlyDictionary<string, int> counters)
    {
        if (counters.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ", ",
            counters
                .OrderByDescending(static kvp => kvp.Value)
                .ThenBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
