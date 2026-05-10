using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
    private readonly IClock _clock;
    private readonly ILogger<ShipmentProviderOperationBackgroundService> _logger;

    public ShipmentProviderOperationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ShipmentProviderOperationWorkerOptions> options,
        IClock clock,
        ILogger<ShipmentProviderOperationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loggedDisabled = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(_options.Value);
            if (!options.Enabled)
            {
                if (!loggedDisabled)
                {
                    _logger.LogInformation("Shipment provider operation worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("Shipment provider operation worker enabled.");
                loggedDisabled = false;
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
        var nowUtc = _clock.UtcNow;
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
                await MarkShipmentProviderOperationFailedAsync(db, item, nowUtc, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                item.Status = "Failed";
                item.FailureReason = WorkerFailureText.Truncate(ex.Message);
                await MarkShipmentProviderOperationFailedAsync(db, item, nowUtc, ct).ConfigureAwait(false);
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

    private static async Task MarkShipmentProviderOperationFailedAsync(
        IAppDbContext db,
        ShipmentProviderOperation item,
        DateTime nowUtc,
        CancellationToken ct)
    {
        if (!string.Equals(item.Provider, "DHL", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var failureEventKey = ResolveDhlFailureEventKey(item.OperationType);
        if (failureEventKey is null)
        {
            return;
        }

        var shipment = await db.Set<Shipment>()
            .FirstOrDefaultAsync(x => x.Id == item.ShipmentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (shipment is null)
        {
            return;
        }

        shipment.LastCarrierEventKey = failureEventKey;
        var alreadyRecorded = await db.Set<ShipmentCarrierEvent>()
            .AnyAsync(
                x => x.ShipmentId == shipment.Id &&
                     !x.IsDeleted &&
                     x.CarrierEventKey == failureEventKey,
                ct)
            .ConfigureAwait(false);
        if (alreadyRecorded)
        {
            return;
        }

        db.Set<ShipmentCarrierEvent>().Add(new ShipmentCarrierEvent
        {
            ShipmentId = shipment.Id,
            Carrier = shipment.Carrier,
            ProviderShipmentReference = shipment.ProviderShipmentReference ?? string.Empty,
            CarrierEventKey = failureEventKey,
            ProviderStatus = "Failed",
            ExceptionCode = item.OperationType,
            ExceptionMessage = item.FailureReason,
            TrackingNumber = shipment.TrackingNumber,
            LabelUrl = shipment.LabelUrl,
            Service = shipment.Service,
            OccurredAtUtc = nowUtc
        });
    }

    private static string? ResolveDhlFailureEventKey(string operationType)
    {
        return operationType switch
        {
            "CreateShipment" => "shipment.provider_create_failed",
            "GenerateLabel" => "shipment.label_failed",
            "CreateReturnShipment" => "return.provider_create_failed",
            _ => null
        };
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

        if (string.Equals(item.Provider, "DHL", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.OperationType, "CreateReturnShipment", StringComparison.OrdinalIgnoreCase))
        {
            var handler = services.GetRequiredService<ApplyDhlReturnShipmentCreateOperationHandler>();
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
