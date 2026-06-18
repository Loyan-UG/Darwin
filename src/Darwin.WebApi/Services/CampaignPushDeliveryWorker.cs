using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Darwin.WebApi.Services;

public sealed class CampaignPushDeliveryWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ActiveDelay = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignPushDeliveryWorker> _logger;

    public CampaignPushDeliveryWorker(IServiceScopeFactory scopeFactory, ILogger<CampaignPushDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessPendingPushDeliveriesHandler>();
                var result = await handler.HandleAsync(stoppingToken).ConfigureAwait(false);
                processed = result.Succeeded ? result.Value : 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Campaign push delivery worker failed.");
            }

            await Task.Delay(processed > 0 ? ActiveDelay : IdleDelay, stoppingToken).ConfigureAwait(false);
        }
    }
}
