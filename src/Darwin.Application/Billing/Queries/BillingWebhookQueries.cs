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

        var subscriptions = _db.Set<WebhookSubscription>().AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            subscriptions = subscriptions.Where(x => x.EventType.Contains(term) || x.CallbackUrl.Contains(term));
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

        var deliveries = from delivery in _db.Set<WebhookDelivery>().AsNoTracking()
                         join subscription in _db.Set<WebhookSubscription>().AsNoTracking()
                             on delivery.SubscriptionId equals subscription.Id
                         where !delivery.IsDeleted && !subscription.IsDeleted
                         select new BillingWebhookDeliveryListItemDto
                         {
                             Id = delivery.Id,
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

        deliveries = filter switch
        {
            BillingWebhookDeliveryQueueFilter.Pending => deliveries.Where(x => x.Status == "Pending"),
            BillingWebhookDeliveryQueueFilter.Failed => deliveries.Where(x => x.Status == "Failed"),
            BillingWebhookDeliveryQueueFilter.Succeeded => deliveries.Where(x => x.Status == "Succeeded"),
            BillingWebhookDeliveryQueueFilter.RetryPending => deliveries.Where(x => x.Status != "Succeeded" && x.RetryCount > 0),
            _ => deliveries
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            deliveries = deliveries.Where(x =>
                x.EventType.Contains(term) ||
                x.CallbackUrl.Contains(term) ||
                x.Status.Contains(term) ||
                (x.IdempotencyKey != null && x.IdempotencyKey.Contains(term)));
        }

        var total = await deliveries.CountAsync(ct).ConfigureAwait(false);
        var items = await deliveries
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetBillingWebhookDeliveriesPageDto
        {
            Items = items,
            Total = total
        };
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

        return new BillingWebhookOpsSummaryDto
        {
            ActiveSubscriptionCount = await subscriptions.CountAsync(x => x.IsActive, ct).ConfigureAwait(false),
            PendingDeliveryCount = await deliveries.CountAsync(x => x.Status == "Pending", ct).ConfigureAwait(false),
            FailedDeliveryCount = await deliveries.CountAsync(x => x.Status == "Failed", ct).ConfigureAwait(false),
            SucceededDeliveryCount = await deliveries.CountAsync(x => x.Status == "Succeeded", ct).ConfigureAwait(false),
            RetryPendingCount = await deliveries.CountAsync(x => x.Status != "Succeeded" && x.RetryCount > 0, ct).ConfigureAwait(false)
        };
    }
}
