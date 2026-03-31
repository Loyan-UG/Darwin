using System;
using System.Collections.Generic;

namespace Darwin.Application.Billing.DTOs;

public enum BillingWebhookDeliveryQueueFilter
{
    All = 0,
    Pending = 1,
    Failed = 2,
    Succeeded = 3,
    RetryPending = 4,
    PaymentExceptions = 5,
    DisputeSignals = 6
}

public sealed class BillingWebhookSubscriptionListItemDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class BillingWebhookDeliveryListItemDto
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public int? ResponseCode { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastAttemptAtUtc { get; init; }
    public string? IdempotencyKey { get; init; }
    public bool IsActiveSubscription { get; init; }
    public string SuggestedOperatorAction { get; set; } = string.Empty;
    public string SuggestedQueueTarget { get; set; } = string.Empty;
}

public sealed class BillingWebhookOpsSummaryDto
{
    public int ActiveSubscriptionCount { get; init; }
    public int PendingDeliveryCount { get; init; }
    public int FailedDeliveryCount { get; init; }
    public int SucceededDeliveryCount { get; init; }
    public int RetryPendingCount { get; init; }
    public int PaymentExceptionCount { get; init; }
    public int DisputeSignalCount { get; init; }
}

public sealed class GetBillingWebhookSubscriptionsPageDto
{
    public IReadOnlyList<BillingWebhookSubscriptionListItemDto> Items { get; init; } = new List<BillingWebhookSubscriptionListItemDto>();
    public int Total { get; init; }
}

public sealed class GetBillingWebhookDeliveriesPageDto
{
    public IReadOnlyList<BillingWebhookDeliveryListItemDto> Items { get; init; } = new List<BillingWebhookDeliveryListItemDto>();
    public int Total { get; init; }
}
