using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Billing;

/// <summary>
/// Processes verified Stripe webhook payloads and applies bounded payment/subscription lifecycle updates.
/// </summary>
public sealed class ProcessStripeWebhookHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ProcessStripeWebhookHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<StripeWebhookProcessingResultDto>> HandleAsync(string rawPayloadJson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawPayloadJson))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookPayloadInvalid"]);
        }

        if (!TryParsePayload(rawPayloadJson, out var parsedDocument))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookPayloadInvalid"]);
        }

        using var document = parsedDocument!;

        var root = document.RootElement;
        var eventId = GetString(root, "id");
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookEventIdRequired"]);
        }

        var eventType = GetString(root, "type");
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookEventTypeRequired"]);
        }

        var existing = await _db.Set<EventLog>()
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == eventId, ct)
            .ConfigureAwait(false);

        if (existing)
        {
            return Result<StripeWebhookProcessingResultDto>.Ok(new StripeWebhookProcessingResultDto
            {
                EventId = eventId,
                EventType = eventType,
                IsDuplicate = true
            });
        }

        if (!TryGetNested(root, out var stripeObject, "data", "object"))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookPayloadInvalid"]);
        }

        var occurredAtUtc = GetUnixDateTimeUtc(root, "created") ?? DateTime.UtcNow;
        var result = new StripeWebhookProcessingResultDto
        {
            EventId = eventId,
            EventType = eventType,
            IsDuplicate = false
        };

        _db.Set<EventLog>().Add(new EventLog
        {
            Type = BuildEventLogType(eventType),
            OccurredAtUtc = occurredAtUtc,
            PropertiesJson = rawPayloadJson,
            UtmSnapshotJson = "{}",
            IdempotencyKey = eventId
        });

        switch (eventType)
        {
            case "checkout.session.completed":
                await ApplyCheckoutSessionCompletedAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
                break;
            case "payment_intent.succeeded":
                await ApplyPaymentIntentSucceededAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
                break;
            case "payment_intent.payment_failed":
                await ApplyPaymentIntentFailedAsync(stripeObject, result, ct).ConfigureAwait(false);
                break;
            case "payment_intent.canceled":
                await ApplyPaymentIntentCanceledAsync(stripeObject, result, ct).ConfigureAwait(false);
                break;
            case "charge.refunded":
                await ApplyChargeRefundedAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
                break;
            case "invoice.paid":
                await ApplyInvoicePaidAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
                break;
            case "invoice.payment_failed":
                await ApplyInvoicePaymentFailedAsync(stripeObject, result, ct).ConfigureAwait(false);
                break;
            case "customer.subscription.updated":
                await ApplySubscriptionUpdatedAsync(stripeObject, result, ct).ConfigureAwait(false);
                break;
            case "customer.subscription.deleted":
                await ApplySubscriptionDeletedAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
                break;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<StripeWebhookProcessingResultDto>.Ok(result);
    }

    private async Task ApplyCheckoutSessionCompletedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var sessionId = GetString(stripeObject, "id");
        var paymentIntentId = GetString(stripeObject, "payment_intent");
        var paymentStatus = GetString(stripeObject, "payment_status");
        var payment = await FindPaymentAsync(paymentIntentId, sessionId, sessionId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;

        if (!string.IsNullOrWhiteSpace(paymentIntentId))
        {
            payment.ProviderPaymentIntentRef ??= paymentIntentId;
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            payment.ProviderCheckoutSessionRef ??= sessionId;
            payment.ProviderTransactionRef ??= sessionId;
        }

        if (string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase))
        {
            ApplyCapturedPayment(payment, occurredAtUtc);
            await PromoteOrderToPaidAsync(payment.OrderId, ct).ConfigureAwait(false);
        }
    }

    private async Task ApplyPaymentIntentSucceededAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = GetString(stripeObject, "id");
        var latestChargeId = GetString(stripeObject, "latest_charge");
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        ApplyCapturedPayment(payment, occurredAtUtc);
        await PromoteOrderToPaidAsync(payment.OrderId, ct).ConfigureAwait(false);
    }

    private async Task ApplyPaymentIntentFailedAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = GetString(stripeObject, "id");
        var latestChargeId = GetString(stripeObject, "latest_charge");
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        payment.Status = PaymentStatus.Failed;
        payment.PaidAtUtc = null;
        payment.FailureReason = GetString(stripeObject, "last_payment_error", "message") ?? _localizer["OperationFailed"];
    }

    private async Task ApplyPaymentIntentCanceledAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = GetString(stripeObject, "id");
        var latestChargeId = GetString(stripeObject, "latest_charge");
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        payment.Status = PaymentStatus.Voided;
        payment.PaidAtUtc = null;
        payment.FailureReason = GetString(stripeObject, "cancellation_reason") ?? _localizer["OperationFailed"];
    }

    private async Task ApplyChargeRefundedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var chargeId = GetString(stripeObject, "id");
        var paymentIntentId = GetString(stripeObject, "payment_intent");
        var amountRefunded = GetInt64(stripeObject, "amount_refunded");
        var payment = await FindPaymentAsync(paymentIntentId, null, chargeId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= chargeId ?? paymentIntentId;
        payment.Status = PaymentStatus.Refunded;
        payment.FailureReason = null;

        if (payment.OrderId.HasValue)
        {
            var order = await _db.Set<Order>()
                .FirstOrDefaultAsync(x => x.Id == payment.OrderId.Value, ct)
                .ConfigureAwait(false);

            if (order is not null && amountRefunded.HasValue && amountRefunded.Value > 0)
            {
                order.Status = amountRefunded.Value >= payment.AmountMinor
                    ? OrderStatus.Refunded
                    : OrderStatus.PartiallyRefunded;
            }
        }
    }

    private async Task ApplyInvoicePaidAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var invoiceId = GetString(stripeObject, "id");
        var providerSubscriptionId = GetString(stripeObject, "subscription");
        var paidAtUtc = GetUnixDateTimeUtc(stripeObject, "status_transitions", "paid_at") ?? occurredAtUtc;

        var invoice = await FindSubscriptionInvoiceAsync(invoiceId, ct).ConfigureAwait(false);
        if (invoice is not null)
        {
            result.MatchedSubscriptionInvoiceId = invoice.Id;
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAtUtc ??= paidAtUtc;
            invoice.FailureReason = null;
        }

        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        if (subscription is not null)
        {
            result.MatchedBusinessSubscriptionId = subscription.Id;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CanceledAtUtc = null;
            subscription.CancelAtPeriodEnd = GetBoolean(stripeObject, "cancel_at_period_end") ?? subscription.CancelAtPeriodEnd;
        }
    }

    private async Task ApplyInvoicePaymentFailedAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var invoiceId = GetString(stripeObject, "id");
        var providerSubscriptionId = GetString(stripeObject, "subscription");

        var invoice = await FindSubscriptionInvoiceAsync(invoiceId, ct).ConfigureAwait(false);
        if (invoice is not null)
        {
            result.MatchedSubscriptionInvoiceId = invoice.Id;
            invoice.Status = SubscriptionInvoiceStatus.Open;
            invoice.FailureReason = GetString(stripeObject, "last_finalization_error", "message") ?? _localizer["OperationFailed"];
        }

        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        if (subscription is not null)
        {
            result.MatchedBusinessSubscriptionId = subscription.Id;
            subscription.Status = SubscriptionStatus.PastDue;
        }
    }

    private async Task ApplySubscriptionUpdatedAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var providerSubscriptionId = GetString(stripeObject, "id");
        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        if (subscription is null)
        {
            return;
        }

        result.MatchedBusinessSubscriptionId = subscription.Id;
        subscription.ProviderCustomerId ??= GetString(stripeObject, "customer");
        subscription.Status = MapSubscriptionStatus(GetString(stripeObject, "status"), subscription.Status);
        subscription.CurrentPeriodStartUtc = GetUnixDateTimeUtc(stripeObject, "current_period_start") ?? subscription.CurrentPeriodStartUtc;
        subscription.CurrentPeriodEndUtc = GetUnixDateTimeUtc(stripeObject, "current_period_end") ?? subscription.CurrentPeriodEndUtc;
        subscription.TrialEndsAtUtc = GetUnixDateTimeUtc(stripeObject, "trial_end") ?? subscription.TrialEndsAtUtc;
        subscription.CancelAtPeriodEnd = GetBoolean(stripeObject, "cancel_at_period_end") ?? subscription.CancelAtPeriodEnd;
        subscription.CanceledAtUtc = GetUnixDateTimeUtc(stripeObject, "canceled_at") ?? subscription.CanceledAtUtc;
    }

    private async Task ApplySubscriptionDeletedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var providerSubscriptionId = GetString(stripeObject, "id");
        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        if (subscription is null)
        {
            return;
        }

        result.MatchedBusinessSubscriptionId = subscription.Id;
        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAtUtc = GetUnixDateTimeUtc(stripeObject, "canceled_at") ?? occurredAtUtc;
        subscription.CancelAtPeriodEnd = false;
    }

    private async Task<Payment?> FindPaymentAsync(
        string? providerPaymentIntentRef,
        string? providerCheckoutSessionRef,
        string? providerTransactionRef,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(providerPaymentIntentRef))
        {
            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    (x.ProviderPaymentIntentRef == providerPaymentIntentRef || x.ProviderTransactionRef == providerPaymentIntentRef), ct)
                .ConfigureAwait(false);

            if (payment is not null)
            {
                return payment;
            }
        }

        if (!string.IsNullOrWhiteSpace(providerCheckoutSessionRef))
        {
            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    (x.ProviderCheckoutSessionRef == providerCheckoutSessionRef || x.ProviderTransactionRef == providerCheckoutSessionRef), ct)
                .ConfigureAwait(false);

            if (payment is not null)
            {
                return payment;
            }
        }

        if (string.IsNullOrWhiteSpace(providerTransactionRef))
        {
            return null;
        }

        return await _db.Set<Payment>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProviderTransactionRef == providerTransactionRef, ct)
            .ConfigureAwait(false);
    }

    private async Task<SubscriptionInvoice?> FindSubscriptionInvoiceAsync(string? providerInvoiceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerInvoiceId))
        {
            return null;
        }

        return await _db.Set<SubscriptionInvoice>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProviderInvoiceId == providerInvoiceId, ct)
            .ConfigureAwait(false);
    }

    private async Task<BusinessSubscription?> FindBusinessSubscriptionAsync(string? providerSubscriptionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
        {
            return null;
        }

        return await _db.Set<BusinessSubscription>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProviderSubscriptionId == providerSubscriptionId, ct)
            .ConfigureAwait(false);
    }

    private async Task PromoteOrderToPaidAsync(Guid? orderId, CancellationToken ct)
    {
        if (!orderId.HasValue)
        {
            return;
        }

        var order = await _db.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == orderId.Value, ct)
            .ConfigureAwait(false);

        if (order is not null && order.Status is OrderStatus.Created or OrderStatus.Confirmed)
        {
            order.Status = OrderStatus.Paid;
        }
    }

    private static void ApplyCapturedPayment(Payment payment, DateTime occurredAtUtc)
    {
        if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Voided)
        {
            return;
        }

        payment.Status = PaymentStatus.Captured;
        payment.PaidAtUtc ??= occurredAtUtc;
        payment.FailureReason = null;
    }

    private static string BuildEventLogType(string eventType) => $"StripeWebhook:{eventType}";

    private static SubscriptionStatus MapSubscriptionStatus(string? status, SubscriptionStatus fallback)
        => status?.Trim().ToLowerInvariant() switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "paused" => SubscriptionStatus.Paused,
            _ => fallback
        };

    private static string? GetString(JsonElement element, params string[] path)
    {
        if (!TryGetNested(element, out var nested, path))
        {
            return null;
        }

        return nested.ValueKind switch
        {
            JsonValueKind.String => nested.GetString(),
            JsonValueKind.Number => nested.GetRawText(),
            _ => null
        };
    }

    private static bool? GetBoolean(JsonElement element, params string[] path)
    {
        if (!TryGetNested(element, out var nested, path))
        {
            return null;
        }

        return nested.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static long? GetInt64(JsonElement element, params string[] path)
    {
        if (!TryGetNested(element, out var nested, path))
        {
            return null;
        }

        if (nested.ValueKind == JsonValueKind.Number && nested.TryGetInt64(out var value))
        {
            return value;
        }

        return null;
    }

    private static DateTime? GetUnixDateTimeUtc(JsonElement element, params string[] path)
    {
        var seconds = GetInt64(element, path);
        if (!seconds.HasValue)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime;
    }

    private static bool TryGetNested(JsonElement element, out JsonElement nested, params string[] path)
    {
        nested = element;
        foreach (var segment in path)
        {
            if (nested.ValueKind != JsonValueKind.Object || !nested.TryGetProperty(segment, out nested))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParsePayload(string rawPayloadJson, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(rawPayloadJson);
            return true;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }
}

public sealed class StripeWebhookProcessingResultDto
{
    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public bool IsDuplicate { get; set; }

    public Guid? MatchedPaymentId { get; set; }

    public Guid? MatchedSubscriptionInvoiceId { get; set; }

    public Guid? MatchedBusinessSubscriptionId { get; set; }
}
