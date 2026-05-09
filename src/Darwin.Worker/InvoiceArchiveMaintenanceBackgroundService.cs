using Darwin.Application.CRM.Commands;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class InvoiceArchiveMaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<InvoiceArchiveMaintenanceWorkerOptions> _optionsMonitor;
    private readonly ILogger<InvoiceArchiveMaintenanceBackgroundService> _logger;

    public InvoiceArchiveMaintenanceBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<InvoiceArchiveMaintenanceWorkerOptions> optionsMonitor,
        ILogger<InvoiceArchiveMaintenanceBackgroundService> logger)
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
                    _logger.LogInformation("Invoice archive maintenance worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("Invoice archive maintenance worker enabled.");
                loggedDisabled = false;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredInvoiceArchivesHandler>();
                var result = await handler.HandleAsync(options.BatchSize, stoppingToken).ConfigureAwait(false);

                if (result.PurgedCount > 0)
                {
                    _logger.LogInformation(
                        "Invoice archive maintenance purged {PurgedCount} expired archive payloads from {EvaluatedCount} evaluated invoices.",
                        result.PurgedCount,
                        result.EvaluatedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice archive maintenance iteration failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }

    private static InvoiceArchiveMaintenanceWorkerOptions Normalize(InvoiceArchiveMaintenanceWorkerOptions options)
        => new()
        {
            Enabled = options.Enabled,
            PollIntervalMinutes = Math.Clamp(options.PollIntervalMinutes, 15, 10080),
            BatchSize = Math.Clamp(options.BatchSize, 1, 250)
        };
}
