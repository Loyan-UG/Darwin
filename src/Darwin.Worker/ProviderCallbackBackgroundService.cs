using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing;
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
    private readonly ILogger<ProviderCallbackBackgroundService> _logger;

    public ProviderCallbackBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ProviderCallbackWorkerOptions> options,
        ILogger<ProviderCallbackBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
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
                _logger.LogError(ex, "Provider callback dispatcher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(ProviderCallbackWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var retryCutoffUtc = DateTime.UtcNow.AddSeconds(-options.RetryCooldownSeconds);

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
            item.LastAttemptAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, item, ct).ConfigureAwait(false);
                item.Status = "Succeeded";
                item.ProcessedAtUtc = DateTime.UtcNow;
                item.FailureReason = null;
            }
            catch (ValidationException ex)
            {
                item.Status = "Failed";
                item.FailureReason = ex.Message;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                item.Status = "Failed";
                item.FailureReason = ex.Message.Length > 1024 ? ex.Message[..1024] : ex.Message;
                _logger.LogWarning(ex, "Provider callback inbox message {MessageId} failed.", item.Id);
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
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
