using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Notifications.Sms;
using Darwin.Infrastructure.Notifications.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class ChannelDispatchOperationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ChannelDispatchOperationWorkerOptions> _options;
    private readonly ILogger<ChannelDispatchOperationBackgroundService> _logger;

    public ChannelDispatchOperationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ChannelDispatchOperationWorkerOptions> options,
        ILogger<ChannelDispatchOperationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(_options.Value);
            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await ProcessAsync(options, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Channel dispatch operation iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(ChannelDispatchOperationWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var retryCutoffUtc = DateTime.UtcNow.AddSeconds(-options.RetryCooldownSeconds);

        var items = await db.Set<ChannelDispatchOperation>()
            .Where(x => !x.IsDeleted)
            .Where(x => !x.BusinessId.HasValue || db.Set<Business>().Any(b => b.Id == x.BusinessId.Value && !b.IsDeleted))
            .Where(x => x.Status == "Pending" || x.Status == "Failed")
            .Where(x => x.AttemptCount < options.MaxAttempts)
            .Where(x => !x.LastAttemptAtUtc.HasValue || x.LastAttemptAtUtc <= retryCutoffUtc)
            .OrderBy(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Take(options.BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            item.AttemptCount += 1;
            item.LastAttemptAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, item, ct).ConfigureAwait(false);
                item.Status = "Succeeded";
                item.ProcessedAtUtc = DateTime.UtcNow;
                item.FailureReason = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                item.Status = "Failed";
                item.FailureReason = WorkerFailureText.Truncate(ex.Message);
                _logger.LogWarning(ex, "Channel dispatch operation {OperationId} failed.", item.Id);
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task ProcessOneAsync(IServiceProvider services, ChannelDispatchOperation item, CancellationToken ct)
    {
        var context = new ChannelDispatchContext
        {
            FlowKey = item.FlowKey,
            TemplateKey = item.TemplateKey,
            CorrelationKey = item.CorrelationKey,
            BusinessId = item.BusinessId,
            IntendedRecipientAddress = item.IntendedRecipientAddress
        };

        if (string.Equals(item.Channel, "SMS", StringComparison.OrdinalIgnoreCase))
        {
            var sender = services.GetRequiredService<ProviderBackedSmsSender>();
            await sender.SendAsync(item.RecipientAddress, item.MessageText, ct, context).ConfigureAwait(false);
            return;
        }

        if (string.Equals(item.Channel, "WhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            var sender = services.GetRequiredService<MetaWhatsAppSender>();
            await sender.SendTextAsync(item.RecipientAddress, item.MessageText, ct, context).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unsupported channel '{item.Channel}'.");
    }

    private static ChannelDispatchOperationWorkerOptions Normalize(ChannelDispatchOperationWorkerOptions options)
    {
        return new ChannelDispatchOperationWorkerOptions
        {
            Enabled = options.Enabled,
            PollIntervalSeconds = Math.Max(5, options.PollIntervalSeconds),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            RetryCooldownSeconds = Math.Clamp(options.RetryCooldownSeconds, 5, 3600),
            MaxAttempts = Math.Clamp(options.MaxAttempts, 1, 25)
        };
    }
}
