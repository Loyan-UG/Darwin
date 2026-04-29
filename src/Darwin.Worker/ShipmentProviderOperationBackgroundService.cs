using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class ShipmentProviderOperationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ShipmentProviderOperationWorkerOptions> _options;
    private readonly ILogger<ShipmentProviderOperationBackgroundService> _logger;

    public ShipmentProviderOperationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ShipmentProviderOperationWorkerOptions> options,
        ILogger<ShipmentProviderOperationBackgroundService> logger)
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
                _logger.LogError(ex, "Shipment provider operation iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(ShipmentProviderOperationWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var nowUtc = DateTime.UtcNow;
        var retryCutoffUtc = nowUtc.AddSeconds(-options.RetryCooldownSeconds);

        var items = await db.Set<ShipmentProviderOperation>()
            .Where(x => !x.IsDeleted)
            .Where(x => db.Set<Shipment>().Any(s => s.Id == x.ShipmentId && !s.IsDeleted && db.Set<Order>().Any(o => o.Id == s.OrderId && !o.IsDeleted)))
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
            item.LastAttemptAtUtc = nowUtc;
            if (!await QueueSaveResilience.TrySaveClaimAsync(db, _logger, "shipment provider operation", item.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, item, ct).ConfigureAwait(false);
                item.Status = "Processed";
                item.ProcessedAtUtc = nowUtc;
                item.FailureReason = null;
            }
            catch (ValidationException ex)
            {
                item.Status = "Failed";
                item.FailureReason = WorkerFailureText.Truncate(ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                item.Status = "Failed";
                item.FailureReason = WorkerFailureText.Truncate(ex.Message);
                _logger.LogWarning(ex, "Shipment provider operation {OperationId} failed.", item.Id);
            }

            if (!await QueueSaveResilience.TrySaveCompletionAsync(db, _logger, "shipment provider operation", item.Id, ct).ConfigureAwait(false))
            {
                await PersistShipmentProviderCompletionFallbackAsync(db, item, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task PersistShipmentProviderCompletionFallbackAsync(
        IAppDbContext db,
        ShipmentProviderOperation item,
        CancellationToken ct)
    {
        await db.Set<ShipmentProviderOperation>()
            .Where(x => x.Id == item.Id && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, item.Status)
                .SetProperty(x => x.AttemptCount, item.AttemptCount)
                .SetProperty(x => x.LastAttemptAtUtc, item.LastAttemptAtUtc)
                .SetProperty(x => x.ProcessedAtUtc, item.ProcessedAtUtc)
                .SetProperty(x => x.FailureReason, item.FailureReason),
                ct)
            .ConfigureAwait(false);
    }

    private static async Task ProcessOneAsync(IServiceProvider services, ShipmentProviderOperation item, CancellationToken ct)
    {
        if (string.Equals(item.Provider, "DHL", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.OperationType, "CreateShipment", StringComparison.OrdinalIgnoreCase))
        {
            var handler = services.GetRequiredService<ApplyDhlShipmentCreateOperationHandler>();
            await handler.HandleAsync(item.ShipmentId, ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(item.Provider, "DHL", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.OperationType, "GenerateLabel", StringComparison.OrdinalIgnoreCase))
        {
            var handler = services.GetRequiredService<ApplyDhlShipmentLabelOperationHandler>();
            await handler.HandleAsync(item.ShipmentId, ct).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unsupported shipment provider operation '{item.Provider}:{item.OperationType}'.");
    }

    private static ShipmentProviderOperationWorkerOptions Normalize(ShipmentProviderOperationWorkerOptions options)
    {
        return new ShipmentProviderOperationWorkerOptions
        {
            Enabled = options.Enabled,
            PollIntervalSeconds = Math.Max(5, options.PollIntervalSeconds),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            RetryCooldownSeconds = Math.Clamp(options.RetryCooldownSeconds, 5, 3600),
            MaxAttempts = Math.Clamp(options.MaxAttempts, 1, 25)
        };
    }
}
