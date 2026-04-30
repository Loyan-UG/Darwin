using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class WebhookDeliveryBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<WebhookDeliveryWorkerOptions> _options;
    private readonly ILogger<WebhookDeliveryBackgroundService> _logger;

    public WebhookDeliveryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookDeliveryWorkerOptions> options,
        ILogger<WebhookDeliveryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
                    _logger.LogInformation("Webhook delivery worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("Webhook delivery worker enabled.");
                loggedDisabled = false;
            }

            try
            {
                await ProcessPendingDeliveriesAsync(options, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook delivery dispatcher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessPendingDeliveriesAsync(WebhookDeliveryWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var nowUtc = DateTime.UtcNow;
        var retryCutoffUtc = nowUtc.AddSeconds(-options.RetryCooldownSeconds);
        var deliveries = await db.Set<WebhookDelivery>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Status == "Pending" || x.Status == "Failed")
            .Where(x => x.RetryCount < options.MaxAttempts)
            .Where(x => !x.LastAttemptAtUtc.HasValue || x.LastAttemptAtUtc <= retryCutoffUtc)
            .OrderBy(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Take(options.BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (deliveries.Count == 0)
        {
            return;
        }

        var subscriptionIds = deliveries.Select(x => x.SubscriptionId).Distinct().ToList();
        var subscriptions = await db.Set<WebhookSubscription>()
            .Where(x => !x.IsDeleted && subscriptionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        var eventIds = deliveries.Where(x => x.EventRefId.HasValue).Select(x => x.EventRefId!.Value).Distinct().ToList();
        var events = eventIds.Count == 0
            ? new Dictionary<Guid, EventLog>()
            : await db.Set<EventLog>()
                .Where(x => !x.IsDeleted && eventIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct)
                .ConfigureAwait(false);

        var inactiveSubscriptionDeliveryIds = new List<Guid>();
        foreach (var delivery in deliveries)
        {
            if (!subscriptions.TryGetValue(delivery.SubscriptionId, out var subscription) || !subscription.IsActive)
            {
                delivery.LastAttemptAtUtc = nowUtc;
                delivery.RetryCount = options.MaxAttempts;
                delivery.Status = "Failed";
                inactiveSubscriptionDeliveryIds.Add(delivery.Id);
                continue;
            }

            events.TryGetValue(delivery.EventRefId ?? Guid.Empty, out var eventLog);
            await DispatchAsync(db, delivery, subscription, eventLog, options, nowUtc, ct).ConfigureAwait(false);
        }

        if (inactiveSubscriptionDeliveryIds.Count > 0 &&
            !await QueueSaveResilience.TrySaveBatchAsync(db, _logger, "webhook delivery inactive subscription", ct).ConfigureAwait(false))
        {
            await PersistInactiveSubscriptionFallbackAsync(db, inactiveSubscriptionDeliveryIds, nowUtc, options.MaxAttempts, ct).ConfigureAwait(false);
        }
    }

    private static async Task PersistInactiveSubscriptionFallbackAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> deliveryIds,
        DateTime nowUtc,
        int maxAttempts,
        CancellationToken ct)
    {
        await db.Set<WebhookDelivery>()
            .Where(x => deliveryIds.Contains(x.Id) && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LastAttemptAtUtc, nowUtc)
                .SetProperty(x => x.RetryCount, maxAttempts)
                .SetProperty(x => x.Status, "Failed"),
                ct)
            .ConfigureAwait(false);
    }

    private async Task DispatchAsync(
        IAppDbContext db,
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        EventLog? eventLog,
        WebhookDeliveryWorkerOptions options,
        DateTime nowUtc,
        CancellationToken ct)
    {
        delivery.LastAttemptAtUtc = nowUtc;
        delivery.RetryCount += 1;
        if (!await QueueSaveResilience.TrySaveClaimAsync(db, _logger, "webhook delivery", delivery.Id, ct).ConfigureAwait(false))
        {
            return;
        }

        var payload = BuildEnvelope(delivery, subscription, eventLog, nowUtc);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        delivery.PayloadHash = ComputePayloadHash(payloadJson);
        if (!await QueueSaveResilience.TrySaveCompletionAsync(db, _logger, "webhook delivery payload hash", delivery.Id, ct).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(WebhookDeliveryBackgroundService));
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("Darwin.WebhookDispatcher/1.0");
            request.Headers.TryAddWithoutValidation("Idempotency-Key", payload.IdempotencyKey);
            request.Headers.TryAddWithoutValidation("X-Darwin-Delivery-Id", delivery.Id.ToString("D"));
            request.Headers.TryAddWithoutValidation("X-Darwin-Event-Type", payload.EventType);
            request.Headers.TryAddWithoutValidation("X-Darwin-Signature", ComputeSignatureHeader(payloadJson, subscription.Secret));

            if (delivery.EventRefId.HasValue)
            {
                request.Headers.TryAddWithoutValidation("X-Darwin-Event-Ref-Id", delivery.EventRefId.Value.ToString("D"));
            }

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            delivery.ResponseCode = (int)response.StatusCode;
            delivery.Status = response.IsSuccessStatusCode ? "Succeeded" : "Failed";

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Delivered webhook {DeliveryId} ({EventType}) to {CallbackUrl} with status {StatusCode}.",
                    delivery.Id,
                    payload.EventType,
                    subscription.CallbackUrl,
                    (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook {DeliveryId} ({EventType}) callback {CallbackUrl} returned {StatusCode}.",
                    delivery.Id,
                    payload.EventType,
                    subscription.CallbackUrl,
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            delivery.ResponseCode = null;
            delivery.Status = "Failed";

            _logger.LogWarning(
                ex,
                "Webhook {DeliveryId} ({EventType}) callback {CallbackUrl} failed on attempt {RetryCount}.",
                delivery.Id,
                payload.EventType,
                subscription.CallbackUrl,
                delivery.RetryCount);
        }

        if (!await QueueSaveResilience.TrySaveCompletionAsync(db, _logger, "webhook delivery", delivery.Id, ct).ConfigureAwait(false))
        {
            await PersistWebhookCompletionFallbackAsync(db, delivery, ct).ConfigureAwait(false);
        }
    }

    private static async Task PersistWebhookCompletionFallbackAsync(
        IAppDbContext db,
        WebhookDelivery delivery,
        CancellationToken ct)
    {
        await db.Set<WebhookDelivery>()
            .Where(x => x.Id == delivery.Id && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, delivery.Status)
                .SetProperty(x => x.ResponseCode, delivery.ResponseCode)
                .SetProperty(x => x.PayloadHash, delivery.PayloadHash)
                .SetProperty(x => x.LastAttemptAtUtc, delivery.LastAttemptAtUtc)
                .SetProperty(x => x.RetryCount, delivery.RetryCount),
                ct)
            .ConfigureAwait(false);
    }

    private static WebhookEnvelope BuildEnvelope(
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        EventLog? eventLog,
        DateTime attemptedAtUtc)
    {
        return new WebhookEnvelope
        {
            DeliveryId = delivery.Id,
            SubscriptionId = subscription.Id,
            EventType = subscription.EventType,
            EventRefId = delivery.EventRefId,
            AttemptedAtUtc = attemptedAtUtc,
            IdempotencyKey = string.IsNullOrWhiteSpace(delivery.IdempotencyKey)
                ? $"webhook-delivery-{delivery.Id:N}"
                : delivery.IdempotencyKey!,
            PayloadHash = null,
            Event = eventLog is null
                ? null
                : new EventEnvelope
                {
                    Id = eventLog.Id,
                    Type = eventLog.Type,
                    OccurredAtUtc = eventLog.OccurredAtUtc,
                    UserId = eventLog.UserId,
                    AnonymousId = eventLog.AnonymousId,
                    SessionId = eventLog.SessionId,
                    PropertiesJson = eventLog.PropertiesJson,
                    UtmSnapshotJson = eventLog.UtmSnapshotJson,
                    IdempotencyKey = eventLog.IdempotencyKey
                }
        };
    }

    private static string ComputePayloadHash(string payloadJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string ComputeSignatureHeader(string payloadJson, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var payload = Encoding.UTF8.GetBytes(payloadJson);
        using var hmac = new HMACSHA256(key);
        var signature = hmac.ComputeHash(payload);
        return $"sha256={Convert.ToHexString(signature).ToLowerInvariant()}";
    }

    private static WebhookDeliveryWorkerOptions Normalize(WebhookDeliveryWorkerOptions options)
    {
        return new WebhookDeliveryWorkerOptions
        {
            Enabled = options.Enabled,
            PollIntervalSeconds = Math.Max(5, options.PollIntervalSeconds),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            RequestTimeoutSeconds = Math.Clamp(options.RequestTimeoutSeconds, 5, 120),
            RetryCooldownSeconds = Math.Clamp(options.RetryCooldownSeconds, 5, 3600),
            MaxAttempts = Math.Clamp(options.MaxAttempts, 1, 20)
        };
    }

    private sealed class WebhookEnvelope
    {
        public Guid DeliveryId { get; init; }

        public Guid SubscriptionId { get; init; }

        public string EventType { get; init; } = string.Empty;

        public Guid? EventRefId { get; init; }

        public DateTime AttemptedAtUtc { get; init; }

        public string IdempotencyKey { get; init; } = string.Empty;

        public string? PayloadHash { get; init; }

        public EventEnvelope? Event { get; init; }
    }

    private sealed class EventEnvelope
    {
        public Guid Id { get; init; }

        public string Type { get; init; } = string.Empty;

        public DateTime OccurredAtUtc { get; init; }

        public Guid? UserId { get; init; }

        public string? AnonymousId { get; init; }

        public string? SessionId { get; init; }

        public string PropertiesJson { get; init; } = "{}";

        public string UtmSnapshotJson { get; init; } = "{}";

        public string? IdempotencyKey { get; init; }
    }
}
