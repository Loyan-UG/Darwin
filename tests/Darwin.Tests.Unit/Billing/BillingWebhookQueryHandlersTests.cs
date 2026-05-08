using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Entities.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Darwin.Tests.Unit.Billing;

public sealed class BillingWebhookQueryHandlersTests
{
    [Fact]
    public async Task GetBillingWebhookOpsSummary_Should_ReturnCounts()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        var subscriptionId = Guid.NewGuid();

        db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "billing.subscription.updated",
            CallbackUrl = "https://hooks.example/subscription",
            Secret = "secret",
            IsActive = true
        });
        db.Set<WebhookDelivery>().AddRange(
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                Status = "Pending",
                RetryCount = 0
            },
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                Status = "Failed",
                RetryCount = 1
            },
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                Status = "Succeeded",
                RetryCount = 0
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookOpsSummaryHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.ActiveSubscriptionCount.Should().Be(1);
        result.PendingDeliveryCount.Should().Be(1);
        result.FailedDeliveryCount.Should().Be(1);
        result.SucceededDeliveryCount.Should().Be(1);
        result.RetryPendingCount.Should().Be(1);
    }

    // ─── GetBillingWebhookSubscriptionsPageHandler ───────────────────────────

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_ReturnAllNonDeleted()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        db.Set<WebhookSubscription>().AddRange(
            new WebhookSubscription
            {
                EventType = "billing.payment.succeeded",
                CallbackUrl = "https://hooks.example/payment",
                Secret = "secret-a",
                IsActive = true
            },
            new WebhookSubscription
            {
                EventType = "billing.invoice.created",
                CallbackUrl = "https://hooks.example/invoice",
                Secret = "secret-b",
                IsActive = false
            },
            new WebhookSubscription
            {
                EventType = "billing.deleted.event",
                CallbackUrl = "https://hooks.example/deleted",
                Secret = "secret-c",
                IsDeleted = true
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);
        var result = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().NotContain(x => x.EventType == "billing.deleted.event");
    }

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_ReturnEmpty_WhenNoSubscriptions()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);

        var result = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_ApplySearchQuery_OnEventType()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        db.Set<WebhookSubscription>().AddRange(
            new WebhookSubscription
            {
                EventType = "billing.payment.succeeded",
                CallbackUrl = "https://hooks.example/payment",
                Secret = "secret-pay"
            },
            new WebhookSubscription
            {
                EventType = "billing.invoice.created",
                CallbackUrl = "https://hooks.example/invoice",
                Secret = "secret-inv"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);
        var result = await handler.HandleAsync(1, 20, query: "payment", ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.EventType == "billing.payment.succeeded");
    }

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_ApplySearchQuery_OnCallbackUrl()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        db.Set<WebhookSubscription>().AddRange(
            new WebhookSubscription
            {
                EventType = "billing.payment.succeeded",
                CallbackUrl = "https://hooks.alpha.example/pay",
                Secret = "secret-alpha"
            },
            new WebhookSubscription
            {
                EventType = "billing.invoice.created",
                CallbackUrl = "https://hooks.beta.example/inv",
                Secret = "secret-beta"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);
        var result = await handler.HandleAsync(1, 20, query: "alpha", ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.CallbackUrl.Contains("alpha"));
    }

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);

        // page < 1 and pageSize < 1 should not throw
        var result = await handler.HandleAsync(0, 0, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetBillingWebhookSubscriptionsPage_Should_ExposeMappedFields()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        var subId = Guid.NewGuid();
        db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subId,
            EventType = "billing.subscription.updated",
            CallbackUrl = "https://hooks.example/sub",
            Secret = "secret-sub",
            IsActive = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookSubscriptionsPageHandler(db);
        var result = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        var item = result.Items[0];
        item.Id.Should().Be(subId);
        item.EventType.Should().Be("billing.subscription.updated");
        item.CallbackUrl.Should().Be("https://hooks.example/sub");
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetBillingWebhookDeliveriesPage_Should_FilterFailedRows()
    {
        await using var db = BillingWebhookTestDbContext.Create();
        var subscriptionId = Guid.NewGuid();

        db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "billing.invoice.failed",
            CallbackUrl = "https://hooks.example/invoice",
            Secret = "secret",
            IsActive = true
        });
        db.Set<WebhookDelivery>().AddRange(
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                Status = "Pending",
                RetryCount = 0
            },
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                Status = "Failed",
                RetryCount = 2,
                ResponseCode = 500,
                IdempotencyKey = "idem-failed"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingWebhookDeliveriesPageHandler(db);

        var result = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: BillingWebhookDeliveryQueueFilter.Failed,
            ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Status.Should().Be("Failed");
        result.Items[0].RetryCount.Should().Be(2);
        result.Items[0].EventType.Should().Be("billing.invoice.failed");
    }

    private sealed class BillingWebhookTestDbContext : DbContext, IAppDbContext
    {
        private BillingWebhookTestDbContext(DbContextOptions<BillingWebhookTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingWebhookTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingWebhookTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_webhook_tests_{Guid.NewGuid()}")
                .Options;
            return new BillingWebhookTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WebhookSubscription>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.EventType).IsRequired();
                builder.Property(x => x.CallbackUrl).IsRequired();
                builder.Property(x => x.Secret).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<WebhookDelivery>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
