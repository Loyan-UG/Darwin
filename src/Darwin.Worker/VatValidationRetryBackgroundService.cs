using Darwin.Application.CRM.Commands;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class VatValidationRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<VatValidationRetryWorkerOptions> _optionsMonitor;
    private readonly ILogger<VatValidationRetryBackgroundService> _logger;

    public VatValidationRetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<VatValidationRetryWorkerOptions> optionsMonitor,
        ILogger<VatValidationRetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loggedDisabled = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(_optionsMonitor.CurrentValue);
            if (!options.Enabled)
            {
                if (!loggedDisabled)
                {
                    _logger.LogInformation("VAT validation retry worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("VAT validation retry worker enabled.");
                loggedDisabled = false;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<RetryUnknownCustomerVatValidationBatchHandler>();
                var result = await handler.HandleAsync(options.BatchSize, options.MinRetryAgeMinutes, stoppingToken).ConfigureAwait(false);

                if (result.RetriedCount > 0)
                {
                    _logger.LogInformation(
                        "VAT validation retry processed {RetriedCount} customers: {ValidCount} valid, {InvalidCount} invalid, {UnknownCount} still unknown.",
                        result.RetriedCount,
                        result.ValidCount,
                        result.InvalidCount,
                        result.UnknownCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VAT validation retry iteration failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }

    private static VatValidationRetryWorkerOptions Normalize(VatValidationRetryWorkerOptions options)
        => new()
        {
            Enabled = options.Enabled,
            PollIntervalMinutes = Math.Clamp(options.PollIntervalMinutes, 15, 10080),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            MinRetryAgeMinutes = Math.Clamp(options.MinRetryAgeMinutes, 1, 10080)
        };
}
