using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Billing;

public sealed class ProcessStripeWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CaptureStorefrontPayment_And_RecordStripeEvent()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-STRIPE-1001",
            Currency = "EUR",
            GrandTotalGrossMinor = 3200,
            SubtotalNetMinor = 2689,
            TaxTotalMinor = 511,
            Status = OrderStatus.Created
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 3200,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_test_123",
            Status = PaymentStatus.Pending
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync("""
            {
              "id": "evt_test_success",
              "type": "payment_intent.succeeded",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "pi_test_123",
                  "latest_charge": "ch_test_123"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MatchedPaymentId.Should().Be(paymentId);

        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        var eventLog = await db.Set<EventLog>().SingleAsync(x => x.IdempotencyKey == "evt_test_success", TestContext.Current.CancellationToken);

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.ProviderTransactionRef.Should().Be("ch_test_123");
        payment.PaidAtUtc.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Paid);
        eventLog.Type.Should().Be("StripeWebhook:payment_intent.succeeded");
    }

    [Fact]
    public async Task HandleAsync_Should_BeIdempotent_ForDuplicateStripeEventIds()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        const string payload = """
            {
              "id": "evt_test_duplicate",
              "type": "customer.subscription.updated",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "sub_test_123",
                  "status": "active"
                }
              }
            }
            """;

        var first = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.Value.Should().NotBeNull();
        second.Value!.IsDuplicate.Should().BeTrue();

        var eventLogs = await db.Set<EventLog>().CountAsync(x => x.IdempotencyKey == "evt_test_duplicate", TestContext.Current.CancellationToken);
        eventLogs.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // checkout.session.completed — subscription mode
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_Should_CreateNewBusinessSubscription_OnCheckoutSessionCompleted_SubscriptionMode()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "starter",
            Name = "Starter",
            Currency = "EUR",
            PriceMinor = 2900,
            FeaturesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_1",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_session1",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_stripe_1",
                  "customer": "cus_stripe_1",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.MatchedBusinessSubscriptionId.Should().NotBeNull("a new subscription should be created");

        var subscription = await db.Set<BusinessSubscription>()
            .SingleAsync(x => x.BusinessId == businessId, TestContext.Current.CancellationToken);
        subscription.ProviderCheckoutSessionId.Should().Be("cs_test_session1");
        subscription.ProviderSubscriptionId.Should().Be("sub_stripe_1");
        subscription.ProviderCustomerId.Should().Be("cus_stripe_1");
        subscription.Provider.Should().Be("Stripe");
        subscription.BillingPlanId.Should().Be(planId);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateExistingSubscription_MatchedByProviderSubscriptionId()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "pro",
            Name = "Pro",
            Currency = "EUR",
            PriceMinor = 4900,
            FeaturesJson = "{}"
        });
        var existingSubscription = new BusinessSubscription
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ProviderSubscriptionId = "sub_existing",
            Status = SubscriptionStatus.Trialing
        };
        db.Set<BusinessSubscription>().Add(existingSubscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_update",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_session_update",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_existing",
                  "customer": "cus_updated",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.MatchedBusinessSubscriptionId.Should().Be(existingSubscription.Id,
            "the existing subscription matched by provider subscription id should be updated");

        var subscriptions = await db.Set<BusinessSubscription>().ToListAsync(TestContext.Current.CancellationToken);
        subscriptions.Should().HaveCount(1, "no new subscription should be created");
        subscriptions[0].ProviderCustomerId.Should().Be("cus_updated");
        subscriptions[0].ProviderCheckoutSessionId.Should().Be("cs_test_session_update");
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateExistingSubscription_MatchedByCheckoutSessionId()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "basic",
            Name = "Basic",
            Currency = "EUR",
            PriceMinor = 1900,
            FeaturesJson = "{}"
        });
        var existingSubscription = new BusinessSubscription
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ProviderCheckoutSessionId = "cs_test_existing_session",
            Status = SubscriptionStatus.Incomplete
        };
        db.Set<BusinessSubscription>().Add(existingSubscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_session_match",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_existing_session",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_new_ref",
                  "customer": "cus_new",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.MatchedBusinessSubscriptionId.Should().Be(existingSubscription.Id,
            "the existing subscription matched by checkout session id should be updated");

        var subscriptions = await db.Set<BusinessSubscription>().ToListAsync(TestContext.Current.CancellationToken);
        subscriptions.Should().HaveCount(1, "no new subscription should be created");
        subscriptions[0].ProviderSubscriptionId.Should().Be("sub_new_ref");
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateExistingSubscription_MatchedByBusinessId_WhenNoSessionOrSubscriptionMatch()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "team",
            Name = "Team",
            Currency = "EUR",
            PriceMinor = 9900,
            FeaturesJson = "{}"
        });
        // Existing subscription with no provider references - only linked by businessId
        var existingSubscription = new BusinessSubscription
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Status = SubscriptionStatus.Incomplete
        };
        db.Set<BusinessSubscription>().Add(existingSubscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_bizid_match",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_no_match_session",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_brand_new",
                  "customer": "cus_brand_new",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.MatchedBusinessSubscriptionId.Should().Be(existingSubscription.Id,
            "the subscription matched by businessId should be updated when no session/subscription id matches");

        var subscriptions = await db.Set<BusinessSubscription>().ToListAsync(TestContext.Current.CancellationToken);
        subscriptions.Should().HaveCount(1, "no new subscription should be created");
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_BusinessId_Metadata_Missing_In_SubscriptionCheckout()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var planId = Guid.NewGuid();

        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "starter",
            Name = "Starter",
            Currency = "EUR",
            PriceMinor = 2900,
            FeaturesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        // businessId is missing from metadata
        var act = () => handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_no_biz",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_no_biz",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_test",
                  "metadata": {
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "missing businessId metadata must be rejected with an exception");
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_PlanId_Metadata_Missing_In_SubscriptionCheckout()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        // planId is missing from metadata
        var act = () => handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_no_plan",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_no_plan",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_test",
                  "metadata": {
                    "businessId": "{{businessId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "missing planId metadata must be rejected with an exception");
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_Business_Not_Found_In_SubscriptionCheckout()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var planId = Guid.NewGuid();

        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "starter",
            Name = "Starter",
            Currency = "EUR",
            PriceMinor = 2900,
            FeaturesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        // businessId refers to a non-existent business
        var missingBusinessId = Guid.NewGuid();
        var act = () => handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_missing_biz",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_missing_biz",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_test",
                  "metadata": {
                    "businessId": "{{missingBusinessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "a non-existent business must be rejected with an exception");
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_Plan_Not_Found_In_SubscriptionCheckout()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        // planId refers to a non-existent plan
        var missingPlanId = Guid.NewGuid();
        var act = () => handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_missing_plan",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_missing_plan",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_test",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{missingPlanId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "a non-existent plan must be rejected with an exception");
    }

    [Fact]
    public async Task HandleAsync_Should_StoreProviderReferences_OnCheckoutSessionCompleted_SubscriptionMode()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Co" });
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "enterprise",
            Name = "Enterprise",
            Currency = "EUR",
            PriceMinor = 19900,
            FeaturesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync($$"""
            {
              "id": "evt_checkout_sub_refs",
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_test_refs_session",
                  "mode": "subscription",
                  "payment_status": "paid",
                  "subscription": "sub_ref_abc",
                  "customer": "cus_ref_xyz",
                  "metadata": {
                    "businessId": "{{businessId}}",
                    "planId": "{{planId}}"
                  }
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var subscription = await db.Set<BusinessSubscription>()
            .SingleAsync(x => x.BusinessId == businessId, TestContext.Current.CancellationToken);

        subscription.ProviderCheckoutSessionId.Should().Be("cs_test_refs_session");
        subscription.ProviderSubscriptionId.Should().Be("sub_ref_abc");
        subscription.ProviderCustomerId.Should().Be("cus_ref_xyz");
    }

    [Fact]
    public async Task HandleAsync_Should_CreateRefundRecord_OnRefundCreated_WithSucceededStatus()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-REF-001",
            Currency = "EUR",
            GrandTotalGrossMinor = 4500,
            SubtotalNetMinor = 3782,
            TaxTotalMinor = 718,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 4500,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_ref001",
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync("""
            {
              "id": "evt_refund_created",
              "type": "refund.created",
              "created": 1710100000,
              "data": {
                "object": {
                  "id": "re_abc123",
                  "payment_intent": "pi_ref001",
                  "amount": 4500,
                  "currency": "eur",
                  "status": "succeeded"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);

        refund.ProviderRefundReference.Should().Be("re_abc123");
        refund.ProviderStatus.Should().Be("succeeded");
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.CompletedAtUtc.Should().NotBeNull();
        refund.Provider.Should().Be("Stripe");
        payment.Status.Should().Be(PaymentStatus.Refunded);
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public async Task HandleAsync_Should_CreateRefundRecord_OnRefundCreated_WithFailedStatus()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var paymentId = Guid.NewGuid();

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            AmountMinor = 2000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_failed_ref",
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync("""
            {
              "id": "evt_refund_failed",
              "type": "refund.created",
              "created": 1710200000,
              "data": {
                "object": {
                  "id": "re_failed001",
                  "payment_intent": "pi_failed_ref",
                  "amount": 2000,
                  "currency": "eur",
                  "status": "failed",
                  "failure_reason": "lost_or_stolen_card"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.PaymentId == paymentId, TestContext.Current.CancellationToken);

        refund.ProviderRefundReference.Should().Be("re_failed001");
        refund.ProviderStatus.Should().Be("failed");
        refund.Status.Should().Be(RefundStatus.Failed);
        refund.FailureReason.Should().Be("lost_or_stolen_card");
        refund.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateExistingRefundStatus_OnRefundUpdated()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            AmountMinor = 3000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_update_ref",
            Status = PaymentStatus.Captured
        });
        db.Set<Refund>().Add(new Refund
        {
            Id = refundId,
            PaymentId = paymentId,
            AmountMinor = 3000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderRefundReference = "re_update001",
            Status = RefundStatus.Pending,
            Reason = "Existing pending refund"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync("""
            {
              "id": "evt_refund_updated",
              "type": "refund.updated",
              "created": 1710300000,
              "data": {
                "object": {
                  "id": "re_update001",
                  "payment_intent": "pi_update_ref",
                  "amount": 3000,
                  "currency": "eur",
                  "status": "succeeded"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var refund = await db.Set<Refund>().SingleAsync(x => x.Id == refundId, TestContext.Current.CancellationToken);

        refund.Status.Should().Be(RefundStatus.Completed,
            "refund.updated with succeeded status must update the existing refund record");
        refund.ProviderStatus.Should().Be("succeeded");
        refund.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_UpdatePaymentAndOrderStatus_OnChargeRefunded_FullAmount()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CHG-001",
            Currency = "EUR",
            GrandTotalGrossMinor = 5000,
            SubtotalNetMinor = 4202,
            TaxTotalMinor = 798,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 5000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_charge_ref",
            ProviderTransactionRef = "ch_charge001",
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync("""
            {
              "id": "evt_charge_refunded",
              "type": "charge.refunded",
              "created": 1710400000,
              "data": {
                "object": {
                  "id": "ch_charge001",
                  "payment_intent": "pi_charge_ref",
                  "amount_refunded": 5000,
                  "currency": "eur"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);

        payment.Status.Should().Be(PaymentStatus.Refunded,
            "charge.refunded with full amount_refunded must mark payment as Refunded");
        order.Status.Should().Be(OrderStatus.Refunded,
            "charge.refunded for the full payment amount must mark the order as Refunded");
    }

    [Fact]
    public async Task HandleAsync_Should_MarkPartiallyRefunded_OnChargeRefunded_PartialAmount()
    {
        await using var db = StripeWebhookTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CHG-002",
            Currency = "EUR",
            GrandTotalGrossMinor = 6000,
            SubtotalNetMinor = 5042,
            TaxTotalMinor = 958,
            Status = OrderStatus.Paid
        });
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            AmountMinor = 6000,
            Currency = "EUR",
            Provider = "Stripe",
            ProviderPaymentIntentRef = "pi_partial_ref",
            Status = PaymentStatus.Captured
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        await handler.HandleAsync("""
            {
              "id": "evt_charge_partial_refund",
              "type": "charge.refunded",
              "created": 1710500000,
              "data": {
                "object": {
                  "id": "ch_partial001",
                  "payment_intent": "pi_partial_ref",
                  "amount_refunded": 2500,
                  "currency": "eur"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        var payment = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);

        order.Status.Should().Be(OrderStatus.PartiallyRefunded,
            "charge.refunded with partial amount must mark the order as PartiallyRefunded");
        payment.Status.Should().Be(PaymentStatus.Captured,
            "a partial charge.refunded must not change the payment status to Refunded");
    }

    [Fact]
    public async Task HandleAsync_Should_Skip_OnRefundCreated_WhenNoMatchingPayment()
    {
        await using var db = StripeWebhookTestDbContext.Create();

        var handler = new ProcessStripeWebhookHandler(db, new TestStringLocalizer());
        var act = () => handler.HandleAsync("""
            {
              "id": "evt_refund_no_match",
              "type": "refund.created",
              "created": 1710600000,
              "data": {
                "object": {
                  "id": "re_nomatch001",
                  "payment_intent": "pi_not_in_db",
                  "amount": 1000,
                  "currency": "eur",
                  "status": "succeeded"
                }
              }
            }
            """, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync(
            "a refund.created event referencing an unknown payment_intent must be silently skipped");

        var refundCount = await db.Set<Refund>().CountAsync(TestContext.Current.CancellationToken);
        refundCount.Should().Be(0, "no refund record should be created when the payment is not found");
    }

    private sealed class StripeWebhookTestDbContext : DbContext, IAppDbContext
    {
        private StripeWebhookTestDbContext(DbContextOptions<StripeWebhookTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static StripeWebhookTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<StripeWebhookTestDbContext>()
                .UseInMemoryDatabase($"darwin_stripe_webhook_tests_{Guid.NewGuid()}")
                .Options;

            return new StripeWebhookTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.BillingAddressJson).IsRequired();
                builder.Property(x => x.ShippingAddressJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<EventLog>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Type).IsRequired();
                builder.Property(x => x.PropertiesJson).IsRequired();
                builder.Property(x => x.UtmSnapshotJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessSubscription>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BillingPlan>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Code).IsRequired();
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.FeaturesJson).IsRequired();
            });

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<SubscriptionInvoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.LinesJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Refund>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Reason).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
