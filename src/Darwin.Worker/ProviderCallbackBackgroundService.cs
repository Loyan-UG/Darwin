using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing;
using Darwin.Application.Notifications;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Contracts.Shipping;
using Darwin.Domain.Entities.Integration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class ProviderCallbackBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ProviderCallbackWorkerOptions> _options;
    private readonly IClock _clock;
    private readonly ILogger<ProviderCallbackBackgroundService> _logger;

    public ProviderCallbackBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ProviderCallbackWorkerOptions> options,
        IClock clock,
        ILogger<ProviderCallbackBackgroundService> logger)
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
                    _logger.LogInformation("Provider callback worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("Provider callback worker enabled.");
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
                _logger.LogError(ex, "Provider callback dispatcher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(ProviderCallbackWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var nowUtc = _clock.UtcNow;
        var retryCutoffUtc = nowUtc.AddSeconds(-options.RetryCooldownSeconds);

        var items = await db.Set<ProviderCallbackInboxMessage>()
            .Where(x => !x.IsDeleted)
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
            if (!await QueueSaveResilience.TrySaveClaimAsync(db, _logger, "provider callback inbox message", item.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, item, ct).ConfigureAwait(false);
                item.Status = "Processed";
                item.ProcessedAtUtc = _clock.UtcNow;
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
                _logger.LogWarning(ex, "Provider callback inbox message {MessageId} failed.", item.Id);
            }

            if (!await QueueSaveResilience.TrySaveCompletionAsync(db, _logger, "provider callback inbox message", item.Id, ct).ConfigureAwait(false))
            {
                await PersistProviderCallbackCompletionFallbackAsync(db, item, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task PersistProviderCallbackCompletionFallbackAsync(
        IAppDbContext db,
        ProviderCallbackInboxMessage item,
        CancellationToken ct)
    {
        await db.Set<ProviderCallbackInboxMessage>()
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

    private static async Task ProcessOneAsync(IServiceProvider services, ProviderCallbackInboxMessage item, CancellationToken ct)
    {
        if (string.Equals(item.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            var handler = services.GetRequiredService<ProcessStripeWebhookHandler>();
            var result = await handler.HandleAsync(item.PayloadJson, ct).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Error ?? "Stripe callback processing failed.");
            }

            return;
        }

        if (string.Equals(item.Provider, "DHL", StringComparison.OrdinalIgnoreCase))
        {
            var callback = JsonSerializer.Deserialize<DhlShipmentCallbackRequest>(item.PayloadJson, SerializerOptions)
                ?? throw new InvalidOperationException("DHL callback payload was invalid.");
            var handler = services.GetRequiredService<ApplyShipmentCarrierEventHandler>();
            await handler.HandleAsync(new ApplyShipmentCarrierEventDto
            {
                Carrier = "DHL",
                ProviderShipmentReference = callback.ProviderShipmentReference,
                TrackingNumber = callback.TrackingNumber,
                LabelUrl = callback.LabelUrl,
                Service = callback.Service,
                CarrierEventKey = callback.CarrierEventKey,
                OccurredAtUtc = callback.OccurredAtUtc,
                ProviderStatus = callback.ProviderStatus,
                ExceptionCode = callback.ExceptionCode,
                ExceptionMessage = callback.ExceptionMessage
            }, ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(item.Provider, "Brevo", StringComparison.OrdinalIgnoreCase))
        {
            var handler = services.GetRequiredService<ProcessBrevoTransactionalEmailWebhookHandler>();
            var result = await handler.HandleAsync(item.PayloadJson, ct).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Error ?? "Brevo callback processing failed.");
            }

            return;
        }

        throw new InvalidOperationException($"Unsupported provider callback provider '{item.Provider}'.");
    }

    private static ProviderCallbackWorkerOptions Normalize(ProviderCallbackWorkerOptions options)
    {
        return new ProviderCallbackWorkerOptions
        {
            Enabled = options.Enabled,
            PollIntervalSeconds = Math.Max(5, options.PollIntervalSeconds),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            RetryCooldownSeconds = Math.Clamp(options.RetryCooldownSeconds, 5, 3600),
            MaxAttempts = Math.Clamp(options.MaxAttempts, 1, 25)
        };
    }
}
