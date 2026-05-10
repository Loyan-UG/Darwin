using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
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
    private const string EventLogTypePrefix = "StripeWebhook:";
    private const int MaxEventLogTypeLength = 100;
    private const int MaxEventLogIdempotencyKeyLength = 100;
    private const int MaxProviderReferenceLength = 256;
    private const int MaxProviderBillingReferenceLength = 128;
    private const int MaxPaymentFailureReasonLength = 1000;
    private const int MaxInvoiceFailureReasonLength = 2000;
    private static readonly DateTime MinProviderTimestampUtc = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MaxProviderTimestampUtc = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ProcessStripeWebhookHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
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
        var eventId = NormalizeEventId(GetString(root, "id"));
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookEventIdRequired"]);
        }

        var eventType = NormalizeEventType(GetString(root, "type"));
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return Result<StripeWebhookProcessingResultDto>.Fail(_localizer["StripeWebhookEventTypeRequired"]);
        }

        if (await EventAlreadyProcessedAsync(eventId, ct).ConfigureAwait(false))
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

        var occurredAtUtc = GetUnixDateTimeUtc(root, "created") ?? _clock.UtcNow;
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
            PropertiesJson = BuildEventLogPropertiesJson(root, stripeObject, eventId, eventType, occurredAtUtc),
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
            case "charge.refund.updated":
            case "refund.created":
            case "refund.updated":
                await ApplyRefundObjectAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
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

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            if (await EventAlreadyProcessedAsync(eventId, ct).ConfigureAwait(false))
            {
                result.IsDuplicate = true;
                result.MatchedPaymentId = null;
                result.MatchedSubscriptionInvoiceId = null;
                result.MatchedBusinessSubscriptionId = null;
                return Result<StripeWebhookProcessingResultDto>.Ok(result);
            }

            throw;
        }

        return Result<StripeWebhookProcessingResultDto>.Ok(result);
    }

    private Task<bool> EventAlreadyProcessedAsync(string eventId, CancellationToken ct)
    {
        return _db.Set<EventLog>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IdempotencyKey == eventId, ct);
    }

    private async Task ApplyCheckoutSessionCompletedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        if (string.Equals(GetString(stripeObject, "mode"), "subscription", StringComparison.OrdinalIgnoreCase))
        {
            await ApplySubscriptionCheckoutSessionCompletedAsync(stripeObject, occurredAtUtc, result, ct).ConfigureAwait(false);
            return;
        }

        var sessionId = NormalizeProviderReference(GetString(stripeObject, "id"));
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "payment_intent"));
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
            EnsurePaymentAmountMatches(payment, GetInt64(stripeObject, "amount_total"), GetString(stripeObject, "currency"));
            if (ApplyCapturedPayment(payment, occurredAtUtc))
            {
                await PromoteOrderToPaidAsync(payment.OrderId, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ApplySubscriptionCheckoutSessionCompletedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var sessionId = NormalizeProviderBillingReference(GetString(stripeObject, "id"));
        var providerSubscriptionId = NormalizeProviderBillingReference(GetString(stripeObject, "subscription"));
        var providerCustomerId = NormalizeProviderBillingReference(GetString(stripeObject, "customer"));
        var businessId = GetGuid(stripeObject, "metadata", "businessId");
        var planId = GetGuid(stripeObject, "metadata", "planId");

        if (sessionId is null || !businessId.HasValue || !planId.HasValue)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        var plan = await _db.Set<BillingPlan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == planId.Value, ct)
            .ConfigureAwait(false);
        var businessExists = await _db.Set<Domain.Entities.Businesses.Business>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id == businessId.Value, ct)
            .ConfigureAwait(false);
        if (plan is null || !businessExists)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false)
                           ?? await FindBusinessSubscriptionByCheckoutSessionAsync(sessionId, ct).ConfigureAwait(false)
                           ?? await _db.Set<BusinessSubscription>()
                               .FirstOrDefaultAsync(x => !x.IsDeleted && x.BusinessId == businessId.Value, ct)
                               .ConfigureAwait(false);

        if (subscription is null)
        {
            subscription = new BusinessSubscription
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId.Value,
                StartedAtUtc = occurredAtUtc
            };
            _db.Set<BusinessSubscription>().Add(subscription);
        }

        result.MatchedBusinessSubscriptionId = subscription.Id;
        subscription.BusinessId = businessId.Value;
        subscription.BillingPlanId = plan.Id;
        subscription.Provider = "Stripe";
        subscription.ProviderCheckoutSessionId = sessionId;
        subscription.ProviderSubscriptionId = providerSubscriptionId ?? subscription.ProviderSubscriptionId;
        ApplyProviderCustomerId(subscription, providerCustomerId);
        subscription.Status = ResolveCheckoutSubscriptionStatus(stripeObject, subscription.Status, plan.TrialDays);
        subscription.StartedAtUtc = subscription.StartedAtUtc == default ? occurredAtUtc : subscription.StartedAtUtc;
        subscription.UnitPriceMinor = plan.PriceMinor;
        subscription.Currency = NormalizeCurrency(plan.Currency) ?? subscription.Currency;
        subscription.TrialEndsAtUtc = GetUnixDateTimeUtc(stripeObject, "subscription", "trial_end") ?? subscription.TrialEndsAtUtc;
        subscription.CancelAtPeriodEnd = GetBoolean(stripeObject, "subscription", "cancel_at_period_end") ?? subscription.CancelAtPeriodEnd;
        subscription.CanceledAtUtc = null;
        subscription.MetadataJson = BuildSubscriptionCheckoutMetadataJson(sessionId, providerSubscriptionId, plan.Code, occurredAtUtc);

        if (TryGetNested(stripeObject, out var subscriptionObject, "subscription") &&
            subscriptionObject.ValueKind == JsonValueKind.Object)
        {
            subscription.Status = MapSubscriptionStatus(GetString(subscriptionObject, "status"), subscription.Status);
            ApplySubscriptionPeriod(subscriptionObject, subscription);
            subscription.TrialEndsAtUtc = GetUnixDateTimeUtc(subscriptionObject, "trial_end") ?? subscription.TrialEndsAtUtc;
        }
    }

    private async Task ApplyPaymentIntentSucceededAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "id"));
        var latestChargeId = NormalizeProviderReference(GetString(stripeObject, "latest_charge"));
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        EnsurePaymentAmountMatches(payment, GetInt64(stripeObject, "amount_received") ?? GetInt64(stripeObject, "amount"), GetString(stripeObject, "currency"));
        if (ApplyCapturedPayment(payment, occurredAtUtc))
        {
            await PromoteOrderToPaidAsync(payment.OrderId, ct).ConfigureAwait(false);
        }
    }

    private async Task ApplyPaymentIntentFailedAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "id"));
        var latestChargeId = NormalizeProviderReference(GetString(stripeObject, "latest_charge"));
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        if (IsSettledPayment(payment))
        {
            return;
        }

        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        payment.Status = PaymentStatus.Failed;
        payment.PaidAtUtc = null;
        payment.FailureReason = BuildProviderFailureReason(
            GetString(stripeObject, "last_payment_error", "message"),
            MaxPaymentFailureReasonLength);
    }

    private async Task ApplyPaymentIntentCanceledAsync(
        JsonElement stripeObject,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "id"));
        var latestChargeId = NormalizeProviderReference(GetString(stripeObject, "latest_charge"));
        var payment = await FindPaymentAsync(paymentIntentId, null, latestChargeId ?? paymentIntentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        if (IsSettledPayment(payment))
        {
            return;
        }

        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= latestChargeId ?? paymentIntentId;
        payment.Status = PaymentStatus.Voided;
        payment.PaidAtUtc = null;
        payment.FailureReason = BuildProviderFailureReason(
            GetString(stripeObject, "cancellation_reason"),
            MaxPaymentFailureReasonLength);
    }

    private async Task ApplyChargeRefundedAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var chargeId = NormalizeProviderReference(GetString(stripeObject, "id"));
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "payment_intent"));
        var amountRefunded = GetInt64(stripeObject, "amount_refunded");
        var payment = await FindPaymentAsync(paymentIntentId, null, chargeId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        EnsureRefundAmountMatches(payment, amountRefunded, GetString(stripeObject, "currency"));
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= chargeId ?? paymentIntentId;
        if (!amountRefunded.HasValue || amountRefunded.Value >= payment.AmountMinor)
        {
            payment.Status = PaymentStatus.Refunded;
        }

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

    private async Task ApplyRefundObjectAsync(
        JsonElement stripeObject,
        DateTime occurredAtUtc,
        StripeWebhookProcessingResultDto result,
        CancellationToken ct)
    {
        var refundReference = NormalizeProviderReference(GetString(stripeObject, "id"));
        var paymentIntentId = NormalizeProviderReference(GetString(stripeObject, "payment_intent"));
        var chargeId = NormalizeProviderReference(GetString(stripeObject, "charge"));
        var amount = GetInt64(stripeObject, "amount");
        var currency = GetString(stripeObject, "currency");
        var status = NormalizeNullable(GetString(stripeObject, "status")) ?? "unknown";

        var payment = await FindPaymentAsync(paymentIntentId, null, chargeId, ct).ConfigureAwait(false);
        if (payment is null || !amount.HasValue)
        {
            return;
        }

        result.MatchedPaymentId = payment.Id;
        EnsureRefundAmountMatches(payment, amount, currency);
        payment.ProviderPaymentIntentRef ??= paymentIntentId;
        payment.ProviderTransactionRef ??= chargeId ?? paymentIntentId;

        var refund = await FindRefundAsync(refundReference, stripeObject, payment.Id, amount.Value, ct).ConfigureAwait(false);
        if (refund is null)
        {
            refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                AmountMinor = amount.Value,
                Currency = string.IsNullOrWhiteSpace(currency) ? payment.Currency : currency.Trim().ToUpperInvariant(),
                Reason = "Stripe webhook refund",
                Provider = "Stripe",
                RequestedAtUtc = occurredAtUtc
            };
            _db.Set<Refund>().Add(refund);
        }

        result.MatchedRefundId = refund.Id;
        refund.Provider = "Stripe";
        refund.ProviderRefundReference = refundReference ?? refund.ProviderRefundReference;
        refund.ProviderPaymentReference = paymentIntentId ?? chargeId ?? refund.ProviderPaymentReference;
        refund.ProviderStatus = status;
        refund.FailureReason = string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
            ? BuildProviderFailureReason(GetString(stripeObject, "failure_reason"), 512)
            : null;
        refund.Status = status.ToLowerInvariant() switch
        {
            "succeeded" => RefundStatus.Completed,
            "failed" => RefundStatus.Failed,
            _ => RefundStatus.Pending
        };
        refund.CompletedAtUtc = refund.Status == RefundStatus.Completed
            ? GetUnixDateTimeUtc(stripeObject, "created") ?? occurredAtUtc
            : null;

        var completedRefunded = await GetCompletedRefundTotalAsync(payment.Id, refund, ct).ConfigureAwait(false);
        if (completedRefunded >= payment.AmountMinor)
        {
            payment.Status = PaymentStatus.Refunded;
        }

        payment.FailureReason = null;
        if (payment.OrderId.HasValue && completedRefunded > 0)
        {
            var order = await _db.Set<Order>()
                .FirstOrDefaultAsync(x => x.Id == payment.OrderId.Value, ct)
                .ConfigureAwait(false);

            if (order is not null)
            {
                order.Status = completedRefunded >= payment.AmountMinor
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
        var invoiceId = NormalizeProviderBillingReference(GetString(stripeObject, "id"));
        var providerSubscriptionId = NormalizeProviderBillingReference(GetString(stripeObject, "subscription"));
        var paidAtUtc = GetUnixDateTimeUtc(stripeObject, "status_transitions", "paid_at") ?? occurredAtUtc;

        var invoice = await FindSubscriptionInvoiceAsync(invoiceId, ct).ConfigureAwait(false);
        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        EnsureInvoiceMatchesSubscription(invoice, subscription);

        if (invoice is not null)
        {
            result.MatchedSubscriptionInvoiceId = invoice.Id;
            EnsureSubscriptionInvoiceAmountMatches(
                invoice,
                GetInt64(stripeObject, "amount_paid") ?? GetInt64(stripeObject, "total"),
                GetString(stripeObject, "currency"));
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAtUtc ??= paidAtUtc;
            invoice.FailureReason = null;
        }

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
        var invoiceId = NormalizeProviderBillingReference(GetString(stripeObject, "id"));
        var providerSubscriptionId = NormalizeProviderBillingReference(GetString(stripeObject, "subscription"));

        var invoice = await FindSubscriptionInvoiceAsync(invoiceId, ct).ConfigureAwait(false);
        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        EnsureInvoiceMatchesSubscription(invoice, subscription);

        if (invoice is not null)
        {
            result.MatchedSubscriptionInvoiceId = invoice.Id;
            EnsureSubscriptionInvoiceAmountMatches(
                invoice,
                GetInt64(stripeObject, "amount_due") ?? GetInt64(stripeObject, "total"),
                GetString(stripeObject, "currency"));
            invoice.Status = SubscriptionInvoiceStatus.Open;
            invoice.FailureReason = BuildProviderFailureReason(
                GetString(stripeObject, "last_finalization_error", "message"),
                MaxInvoiceFailureReasonLength);
        }

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
        var providerSubscriptionId = NormalizeProviderBillingReference(GetString(stripeObject, "id"));
        var subscription = await FindBusinessSubscriptionAsync(providerSubscriptionId, ct).ConfigureAwait(false);
        if (subscription is null)
        {
            return;
        }

        result.MatchedBusinessSubscriptionId = subscription.Id;
        ApplyProviderCustomerId(subscription, NormalizeProviderBillingReference(GetString(stripeObject, "customer")));
        subscription.Status = MapSubscriptionStatus(GetString(stripeObject, "status"), subscription.Status);
        ApplySubscriptionPeriod(stripeObject, subscription);
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
        var providerSubscriptionId = NormalizeProviderBillingReference(GetString(stripeObject, "id"));
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
            var payment = await FindUniquePaymentAsync(
                _db.Set<Payment>().Where(x =>
                    !x.IsDeleted &&
                    x.Provider == "Stripe" &&
                    (x.ProviderPaymentIntentRef == providerPaymentIntentRef || x.ProviderTransactionRef == providerPaymentIntentRef)),
                ct)
                .ConfigureAwait(false);

            if (payment is not null)
            {
                return payment;
            }
        }

        if (!string.IsNullOrWhiteSpace(providerCheckoutSessionRef))
        {
            var payment = await FindUniquePaymentAsync(
                _db.Set<Payment>().Where(x =>
                    !x.IsDeleted &&
                    x.Provider == "Stripe" &&
                    (x.ProviderCheckoutSessionRef == providerCheckoutSessionRef || x.ProviderTransactionRef == providerCheckoutSessionRef)),
                ct)
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

        return await FindUniquePaymentAsync(
                _db.Set<Payment>().Where(x => !x.IsDeleted && x.Provider == "Stripe" && x.ProviderTransactionRef == providerTransactionRef),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<Payment?> FindUniquePaymentAsync(IQueryable<Payment> query, CancellationToken ct)
    {
        var matches = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(2)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPaymentReferenceAmbiguous"]);
        }

        return matches.Count == 0 ? null : matches[0];
    }

    private async Task<Refund?> FindRefundAsync(
        string? providerRefundReference,
        JsonElement stripeObject,
        Guid paymentId,
        long amountMinor,
        CancellationToken ct)
    {
        var metadataRefundId = NormalizeNullable(GetString(stripeObject, "metadata", "refundId"));
        if (Guid.TryParse(metadataRefundId, out var refundId))
        {
            var refund = await _db.Set<Refund>()
                .FirstOrDefaultAsync(x => x.Id == refundId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (refund is not null)
            {
                return refund;
            }
        }

        if (!string.IsNullOrWhiteSpace(providerRefundReference))
        {
            var refund = await _db.Set<Refund>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    x.Provider == "Stripe" &&
                    x.ProviderRefundReference == providerRefundReference,
                    ct)
                .ConfigureAwait(false);

            if (refund is not null)
            {
                return refund;
            }
        }

        return await _db.Set<Refund>()
            .Where(x =>
                !x.IsDeleted &&
                x.PaymentId == paymentId &&
                x.AmountMinor == amountMinor &&
                x.Status == RefundStatus.Pending &&
                x.ProviderRefundReference == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<long> GetCompletedRefundTotalAsync(Guid paymentId, Refund currentRefund, CancellationToken ct)
    {
        var completed = await _db.Set<Refund>()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.PaymentId == paymentId &&
                x.Id != currentRefund.Id &&
                x.Status == RefundStatus.Completed)
            .SumAsync(x => (long?)x.AmountMinor, ct)
            .ConfigureAwait(false) ?? 0L;

        return currentRefund.Status == RefundStatus.Completed
            ? completed + currentRefund.AmountMinor
            : completed;
    }

    private async Task<SubscriptionInvoice?> FindSubscriptionInvoiceAsync(string? providerInvoiceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerInvoiceId))
        {
            return null;
        }

        return await FindUniqueSubscriptionInvoiceAsync(
                _db.Set<SubscriptionInvoice>()
                    .Where(x => !x.IsDeleted && x.Provider == "Stripe" && x.ProviderInvoiceId == providerInvoiceId),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<BusinessSubscription?> FindBusinessSubscriptionAsync(string? providerSubscriptionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
        {
            return null;
        }

        return await FindUniqueBusinessSubscriptionAsync(
                _db.Set<BusinessSubscription>()
                    .Where(x => !x.IsDeleted && x.Provider == "Stripe" && x.ProviderSubscriptionId == providerSubscriptionId),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<BusinessSubscription?> FindBusinessSubscriptionByCheckoutSessionAsync(string? providerCheckoutSessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerCheckoutSessionId))
        {
            return null;
        }

        return await FindUniqueBusinessSubscriptionAsync(
                _db.Set<BusinessSubscription>()
                    .Where(x => !x.IsDeleted && x.Provider == "Stripe" && x.ProviderCheckoutSessionId == providerCheckoutSessionId),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<SubscriptionInvoice?> FindUniqueSubscriptionInvoiceAsync(IQueryable<SubscriptionInvoice> query, CancellationToken ct)
    {
        var matches = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(2)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        return matches.Count == 0 ? null : matches[0];
    }

    private async Task<BusinessSubscription?> FindUniqueBusinessSubscriptionAsync(IQueryable<BusinessSubscription> query, CancellationToken ct)
    {
        var matches = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(2)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        return matches.Count == 0 ? null : matches[0];
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

    private static bool ApplyCapturedPayment(Payment payment, DateTime occurredAtUtc)
    {
        if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Voided)
        {
            return false;
        }

        payment.Status = PaymentStatus.Captured;
        payment.PaidAtUtc ??= occurredAtUtc;
        payment.FailureReason = null;
        return true;
    }

    private static bool IsSettledPayment(Payment payment)
        => payment.Status is PaymentStatus.Captured or PaymentStatus.Completed or PaymentStatus.Refunded;

    private void EnsurePaymentAmountMatches(Payment payment, long? amountMinor, string? currency)
    {
        if (amountMinor.HasValue && amountMinor.Value != payment.AmountMinor)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPaymentAmountMismatch"]);
        }

        if (!string.IsNullOrWhiteSpace(currency) &&
            !string.Equals(currency.Trim(), payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPaymentAmountMismatch"]);
        }
    }

    private void EnsureRefundAmountMatches(Payment payment, long? amountRefundedMinor, string? currency)
    {
        if (amountRefundedMinor.HasValue &&
            (amountRefundedMinor.Value <= 0 || amountRefundedMinor.Value > payment.AmountMinor))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPaymentAmountMismatch"]);
        }

        if (!string.IsNullOrWhiteSpace(currency) &&
            !string.Equals(currency.Trim(), payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPaymentAmountMismatch"]);
        }
    }

    private void EnsureSubscriptionInvoiceAmountMatches(SubscriptionInvoice invoice, long? amountMinor, string? currency)
    {
        if (amountMinor.HasValue && amountMinor.Value != invoice.TotalMinor)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookInvoiceAmountMismatch"]);
        }

        if (!string.IsNullOrWhiteSpace(currency) &&
            !string.Equals(currency.Trim(), invoice.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookInvoiceAmountMismatch"]);
        }
    }

    private void EnsureInvoiceMatchesSubscription(SubscriptionInvoice? invoice, BusinessSubscription? subscription)
    {
        if (invoice is null || subscription is null)
        {
            return;
        }

        if (invoice.BusinessSubscriptionId != subscription.Id ||
            (subscription.BusinessId.HasValue && invoice.BusinessId != subscription.BusinessId.Value))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }
    }

    private void ApplySubscriptionPeriod(JsonElement stripeObject, BusinessSubscription subscription)
    {
        var periodStartUtc = GetUnixDateTimeUtc(stripeObject, "current_period_start");
        var periodEndUtc = GetUnixDateTimeUtc(stripeObject, "current_period_end");
        if (periodStartUtc.HasValue && periodEndUtc.HasValue && periodStartUtc.Value > periodEndUtc.Value)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        subscription.CurrentPeriodStartUtc = periodStartUtc ?? subscription.CurrentPeriodStartUtc;
        subscription.CurrentPeriodEndUtc = periodEndUtc ?? subscription.CurrentPeriodEndUtc;
    }

    private void ApplyProviderCustomerId(BusinessSubscription subscription, string? providerCustomerId)
    {
        if (string.IsNullOrWhiteSpace(providerCustomerId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(subscription.ProviderCustomerId) &&
            !string.Equals(subscription.ProviderCustomerId.Trim(), providerCustomerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
        }

        subscription.ProviderCustomerId ??= providerCustomerId;
    }

    private static SubscriptionStatus ResolveCheckoutSubscriptionStatus(JsonElement stripeObject, SubscriptionStatus fallback, int? trialDays)
    {
        if (TryGetNested(stripeObject, out var subscriptionObject, "subscription") &&
            subscriptionObject.ValueKind == JsonValueKind.Object)
        {
            return MapSubscriptionStatus(GetString(subscriptionObject, "status"), fallback);
        }

        var paymentStatus = GetString(stripeObject, "payment_status");
        if (string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase))
        {
            return trialDays is > 0 ? SubscriptionStatus.Trialing : SubscriptionStatus.Active;
        }

        return SubscriptionStatus.Incomplete;
    }

    private static string BuildSubscriptionCheckoutMetadataJson(
        string providerCheckoutSessionId,
        string? providerSubscriptionId,
        string planCode,
        DateTime occurredAtUtc)
    {
        return JsonSerializer.Serialize(
            new
            {
                providerCheckoutSessionId,
                providerSubscriptionId,
                planCode,
                source = "stripe.checkout.session.completed",
                occurredAtUtc
            });
    }

    private static string? NormalizeCurrency(string? value)
    {
        var normalized = NormalizeNullable(value)?.ToUpperInvariant();
        return normalized?.Length == 3 ? normalized : null;
    }

    private string? NormalizeEventId(string? value)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length <= MaxEventLogIdempotencyKeyLength
            ? normalized
            : throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
    }

    private string? NormalizeEventType(string? value)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        return BuildEventLogType(normalized).Length <= MaxEventLogTypeLength
            ? normalized
            : throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
    }

    private string? NormalizeProviderReference(string? value)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length <= MaxProviderReferenceLength
            ? normalized
            : throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
    }

    private string? NormalizeProviderBillingReference(string? value)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length <= MaxProviderBillingReferenceLength
            ? normalized
            : throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
    }

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildEventLogPropertiesJson(
        JsonElement root,
        JsonElement stripeObject,
        string eventId,
        string eventType,
        DateTime occurredAtUtc)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider"] = "Stripe",
            ["eventId"] = eventId,
            ["eventType"] = eventType,
            ["occurredAtUtc"] = occurredAtUtc.ToString("O"),
            ["payloadCapturedInProviderCallbackInbox"] = "true"
        };

        AddIfNotBlank(properties, "created", GetString(root, "created"));
        AddIfNotBlank(properties, "objectType", GetString(stripeObject, "object"));
        AddIfNotBlank(properties, "objectId", GetString(stripeObject, "id"));
        AddIfNotBlank(properties, "refund", GetString(stripeObject, "id"));
        AddIfNotBlank(properties, "paymentIntent", GetString(stripeObject, "payment_intent"));
        AddIfNotBlank(properties, "checkoutSession", GetString(stripeObject, "checkout_session"));
        AddIfNotBlank(properties, "latestCharge", GetString(stripeObject, "latest_charge"));
        AddIfNotBlank(properties, "invoice", GetString(stripeObject, "invoice"));
        AddIfNotBlank(properties, "subscription", GetString(stripeObject, "subscription"));
        AddIfNotBlank(properties, "customer", GetString(stripeObject, "customer"));
        AddIfNotBlank(properties, "status", GetString(stripeObject, "status"));
        AddIfNotBlank(properties, "paymentStatus", GetString(stripeObject, "payment_status"));
        AddIfNotBlank(properties, "currency", GetString(stripeObject, "currency"));
        AddIfNotBlank(properties, "amount", GetString(stripeObject, "amount"));
        AddIfNotBlank(properties, "amountTotal", GetString(stripeObject, "amount_total"));
        AddIfNotBlank(properties, "amountReceived", GetString(stripeObject, "amount_received"));
        AddIfNotBlank(properties, "amountRefunded", GetString(stripeObject, "amount_refunded"));

        return JsonSerializer.Serialize(properties);
    }

    private static void AddIfNotBlank(Dictionary<string, string> properties, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties[key] = value.Trim();
        }
    }

    private string BuildProviderFailureReason(string? providerReason, int maxLength)
    {
        var fallback = _localizer["OperationFailed"].Value;
        var sanitized = OperatorDisplayTextSanitizer.SanitizeFailureText(providerReason);
        var value = string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string BuildEventLogType(string eventType) => $"{EventLogTypePrefix}{eventType}";

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

    private static Guid? GetGuid(JsonElement element, params string[] path)
    {
        var value = GetString(element, path);
        return Guid.TryParse(value, out var guid) ? guid : null;
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

    private DateTime? GetUnixDateTimeUtc(JsonElement element, params string[] path)
    {
        var seconds = GetInt64(element, path);
        if (!seconds.HasValue)
        {
            return null;
        }

        try
        {
            var value = DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime;
            if (value < MinProviderTimestampUtc || value > MaxProviderTimestampUtc)
            {
                throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"]);
            }

            return value;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException(_localizer["StripeWebhookPayloadInvalid"], ex);
        }
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

    public Guid? MatchedRefundId { get; set; }

    public Guid? MatchedSubscriptionInvoiceId { get; set; }

    public Guid? MatchedBusinessSubscriptionId { get; set; }
}
