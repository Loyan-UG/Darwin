using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing.Queries;

public sealed class GetBillingWebhookSubscriptionsPageHandler
{
    private const int MaxPageSize = 200;

    private readonly IAppDbContext _db;

    public GetBillingWebhookSubscriptionsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<GetBillingWebhookSubscriptionsPageDto> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var subscriptions = _db.Set<WebhookSubscription>().AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            subscriptions = subscriptions.Where(x => x.EventType.ToLower().Contains(term) || x.CallbackUrl.ToLower().Contains(term));
        }

        var total = await subscriptions.CountAsync(ct).ConfigureAwait(false);
        var items = await subscriptions
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BillingWebhookSubscriptionListItemDto
            {
                Id = x.Id,
                EventType = x.EventType,
                CallbackUrl = x.CallbackUrl,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetBillingWebhookSubscriptionsPageDto
        {
            Items = items,
            Total = total
        };
    }
}

public sealed class GetBillingWebhookDeliveriesPageHandler
{
    private const int MaxPageSize = 200;

    private readonly IAppDbContext _db;

    public GetBillingWebhookDeliveriesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<GetBillingWebhookDeliveriesPageDto> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        BillingWebhookDeliveryQueueFilter filter = BillingWebhookDeliveryQueueFilter.All,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var deliveries = from delivery in _db.Set<WebhookDelivery>().AsNoTracking()
                         join subscription in _db.Set<WebhookSubscription>().AsNoTracking()
                             on delivery.SubscriptionId equals subscription.Id
                         where !delivery.IsDeleted && !subscription.IsDeleted
                         select new BillingWebhookDeliveryListItemDto
                         {
                             Id = delivery.Id,
                             RowVersion = delivery.RowVersion,
                             SubscriptionId = delivery.SubscriptionId,
                             EventType = subscription.EventType,
                             CallbackUrl = subscription.CallbackUrl,
                             Status = delivery.Status,
                             RetryCount = delivery.RetryCount,
                             ResponseCode = delivery.ResponseCode,
                             CreatedAtUtc = delivery.CreatedAtUtc,
                             LastAttemptAtUtc = delivery.LastAttemptAtUtc,
                             IdempotencyKey = delivery.IdempotencyKey,
                             IsActiveSubscription = subscription.IsActive
                         };

        deliveries = ApplyQueueFilter(deliveries, filter);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            deliveries = deliveries.Where(x =>
                x.EventType.ToLower().Contains(term) ||
                x.CallbackUrl.ToLower().Contains(term) ||
                x.Status.ToLower().Contains(term) ||
                (x.IdempotencyKey != null && x.IdempotencyKey.ToLower().Contains(term)));
        }

        var total = await deliveries.CountAsync(ct).ConfigureAwait(false);
        var items = await deliveries
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            (item.SuggestedOperatorAction, item.SuggestedQueueTarget) = ResolveSuggestedQueueTarget(item);
            item.RetrySafetyState = ResolveRetrySafetyState(item);
            item.FailureDiagnostics = ResolveFailureDiagnostics(item);
            item.EscalationHint = ResolveEscalationHint(item);
        }

        return new GetBillingWebhookDeliveriesPageDto
        {
            Items = items,
            Total = total
        };
    }

    private static IQueryable<BillingWebhookDeliveryListItemDto> ApplyQueueFilter(
        IQueryable<BillingWebhookDeliveryListItemDto> deliveries,
        BillingWebhookDeliveryQueueFilter filter)
    {
        deliveries = filter switch
        {
            BillingWebhookDeliveryQueueFilter.Pending => deliveries.Where(x => x.Status == "Pending"),
            BillingWebhookDeliveryQueueFilter.Failed => deliveries.Where(x => x.Status == "Failed"),
            BillingWebhookDeliveryQueueFilter.Succeeded => deliveries.Where(x => x.Status == "Succeeded"),
            BillingWebhookDeliveryQueueFilter.RetryPending => deliveries.Where(x => x.Status != "Succeeded" && x.RetryCount > 0),
            BillingWebhookDeliveryQueueFilter.PaymentExceptions => deliveries.Where(x =>
                (x.EventType.Contains("payment_intent") || x.EventType.Contains("charge") || x.EventType.Contains("refund")) &&
                (x.Status != "Succeeded" || x.RetryCount > 0)),
            BillingWebhookDeliveryQueueFilter.DisputeSignals => deliveries.Where(x =>
                x.EventType.Contains("dispute") || x.EventType.Contains("charge.dispute")),
            _ => deliveries
        };

        return deliveries;
    }

    private static (string Action, string QueueTarget) ResolveSuggestedQueueTarget(BillingWebhookDeliveryListItemDto item)
    {
        if (item.EventType.Contains("dispute", StringComparison.OrdinalIgnoreCase) ||
            item.EventType.Contains("charge.dispute", StringComparison.OrdinalIgnoreCase))
        {
            return ("Review payment anomalies", "DisputeSignals");
        }

        if (item.EventType.Contains("refund", StringComparison.OrdinalIgnoreCase))
        {
            return ("Review refunds", "Refunds");
        }

        if (item.EventType.Contains("payment_intent", StringComparison.OrdinalIgnoreCase) ||
            item.EventType.Contains("charge", StringComparison.OrdinalIgnoreCase))
        {
            return ("Review payments", "Payments");
        }

        return ("Review callback history", string.Empty);
    }

    private static string ResolveRetrySafetyState(BillingWebhookDeliveryListItemDto item)
    {
        if (!item.IsActiveSubscription)
        {
            return "WebhookRetrySafetySubscriptionInactive";
        }

        if (string.Equals(item.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return item.RetryCount > 0
                ? "WebhookRetrySafetyRecoveredAfterRetry"
                : "WebhookRetrySafetyDeliveredWithoutRetry";
        }

        if (string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return item.RetryCount > 0
                ? "WebhookRetrySafetyRetryInFlight"
                : "WebhookRetrySafetyAwaitingFirstAttempt";
        }

        if (item.ResponseCode is >= 500)
        {
            return "WebhookRetrySafetyReceiverUnavailable";
        }

        if (item.ResponseCode is >= 400)
        {
            return "WebhookRetrySafetyReceiverRejected";
        }

        return "WebhookRetrySafetyNoReceiverResponse";
    }

    private static string ResolveFailureDiagnostics(BillingWebhookDeliveryListItemDto item)
    {
        if (!item.IsActiveSubscription)
        {
            return "WebhookFailureDiagnosticSubscriptionInactive";
        }

        if (string.Equals(item.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return item.RetryCount > 0
                ? "WebhookFailureDiagnosticRecoveredAfterRetry"
                : "WebhookFailureDiagnosticDeliveredWithoutRetry";
        }

        if (string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return item.RetryCount > 0
                ? "WebhookFailureDiagnosticRetryPending"
                : "WebhookFailureDiagnosticPendingFirstAttempt";
        }

        if (item.ResponseCode is >= 500)
        {
            return "WebhookFailureDiagnosticReceiver5xx";
        }

        if (item.ResponseCode is >= 400)
        {
            return "WebhookFailureDiagnosticReceiver4xx";
        }

        return "WebhookFailureDiagnosticNoHttpResponse";
    }

    private static string ResolveEscalationHint(BillingWebhookDeliveryListItemDto item)
    {
        if (!item.IsActiveSubscription)
        {
            return "WebhookEscalationHintSubscriptionInactive";
        }

        if (string.Equals(item.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return item.RetryCount > 0
                ? "WebhookEscalationHintRecovered"
                : "WebhookEscalationHintStable";
        }

        if (item.ResponseCode is >= 500)
        {
            return "WebhookEscalationHintReceiver5xx";
        }

        if (item.ResponseCode is >= 400)
        {
            return "WebhookEscalationHintReceiver4xx";
        }

        if (string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase) && item.RetryCount > 0)
        {
            return "WebhookEscalationHintRetryInFlight";
        }

        return "WebhookEscalationHintNoResponse";
    }
}

public sealed class GetBillingWebhookOpsSummaryHandler
{
    private readonly IAppDbContext _db;

    public GetBillingWebhookOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BillingWebhookOpsSummaryDto> HandleAsync(CancellationToken ct = default)
    {
        var subscriptions = _db.Set<WebhookSubscription>().AsNoTracking().Where(x => !x.IsDeleted);
        var deliveries = _db.Set<WebhookDelivery>().AsNoTracking().Where(x => !x.IsDeleted);
        var deliveryEvents = from delivery in _db.Set<WebhookDelivery>().AsNoTracking()
                             join subscription in _db.Set<WebhookSubscription>().AsNoTracking()
                                 on delivery.SubscriptionId equals subscription.Id
                             where !delivery.IsDeleted && !subscription.IsDeleted
                             select new
                             {
                                 delivery.Status,
                                 delivery.RetryCount,
                                 subscription.EventType
                             };

        return new BillingWebhookOpsSummaryDto
        {
            ActiveSubscriptionCount = await subscriptions.CountAsync(x => x.IsActive, ct).ConfigureAwait(false),
            PendingDeliveryCount = await deliveries.CountAsync(x => x.Status == "Pending", ct).ConfigureAwait(false),
            FailedDeliveryCount = await deliveries.CountAsync(x => x.Status == "Failed", ct).ConfigureAwait(false),
            SucceededDeliveryCount = await deliveries.CountAsync(x => x.Status == "Succeeded", ct).ConfigureAwait(false),
            RetryPendingCount = await deliveries.CountAsync(x => x.Status != "Succeeded" && x.RetryCount > 0, ct).ConfigureAwait(false),
            PaymentExceptionCount = await deliveryEvents.CountAsync(x =>
                (x.EventType.ToLower().Contains("payment_intent") || x.EventType.ToLower().Contains("charge") || x.EventType.ToLower().Contains("refund")) &&
                (x.Status != "Succeeded" || x.RetryCount > 0), ct).ConfigureAwait(false),
            DisputeSignalCount = await deliveryEvents.CountAsync(x =>
                x.EventType.ToLower().Contains("dispute") || x.EventType.ToLower().Contains("charge.dispute"), ct).ConfigureAwait(false)
        };
    }
}
