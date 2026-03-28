using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Darwin.Tests.Unit.Billing;

public sealed class BusinessSubscriptionInvoiceHandlersTests
{
    [Fact]
    public async Task GetBusinessSubscriptionInvoicesPage_Should_FilterOpenInvoices()
    {
        await using var db = BusinessSubscriptionInvoiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "starter",
            Name = "Starter",
            PriceMinor = 2900,
            Currency = "EUR",
            FeaturesJson = "{}"
        });
        db.Set<BusinessSubscription>().Add(new BusinessSubscription
        {
            Id = subscriptionId,
            BusinessId = businessId,
            BillingPlanId = planId,
            Provider = "Stripe",
            Status = SubscriptionStatus.Active,
            StartedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UnitPriceMinor = 2900,
            Currency = "EUR"
        });
        db.Set<SubscriptionInvoice>().AddRange(
            new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                BusinessSubscriptionId = subscriptionId,
                Provider = "Stripe",
                ProviderInvoiceId = "in_open",
                Status = SubscriptionInvoiceStatus.Open,
                TotalMinor = 2900,
                Currency = "EUR",
                IssuedAtUtc = new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc),
                LinesJson = "[]"
            },
            new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                BusinessSubscriptionId = subscriptionId,
                Provider = "Stripe",
                ProviderInvoiceId = "in_paid",
                Status = SubscriptionInvoiceStatus.Paid,
                TotalMinor = 2900,
                Currency = "EUR",
                IssuedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                PaidAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc),
                LinesJson = "[]"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionInvoicesPageHandler(db);

        var result = await handler.HandleAsync(
            businessId,
            filter: BusinessSubscriptionInvoiceQueueFilter.Open,
            ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].ProviderInvoiceId.Should().Be("in_open");
        result.Items[0].PlanName.Should().Be("Starter");
    }

    [Fact]
    public async Task GetBusinessSubscriptionInvoiceOpsSummary_Should_ReturnStatusCounters()
    {
        await using var db = BusinessSubscriptionInvoiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        db.Set<SubscriptionInvoice>().AddRange(
            new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                BusinessSubscriptionId = subscriptionId,
                Provider = "Stripe",
                Status = SubscriptionInvoiceStatus.Open,
                TotalMinor = 1000,
                Currency = "EUR",
                IssuedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                HostedInvoiceUrl = null,
                LinesJson = "[]"
            },
            new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                BusinessSubscriptionId = subscriptionId,
                Provider = "Stripe",
                Status = SubscriptionInvoiceStatus.Paid,
                TotalMinor = 1000,
                Currency = "EUR",
                IssuedAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc),
                HostedInvoiceUrl = "https://billing.example/in_paid",
                LinesJson = "[]"
            },
            new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                BusinessSubscriptionId = subscriptionId,
                Provider = "Stripe",
                Status = SubscriptionInvoiceStatus.Uncollectible,
                TotalMinor = 1000,
                Currency = "EUR",
                IssuedAtUtc = new DateTime(2030, 1, 3, 8, 0, 0, DateTimeKind.Utc),
                FailureReason = "card_declined",
                LinesJson = "[]"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionInvoiceOpsSummaryHandler(db);

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(3);
        result.OpenCount.Should().Be(1);
        result.PaidCount.Should().Be(1);
        result.UncollectibleCount.Should().Be(1);
        result.HostedLinkMissingCount.Should().Be(2);
        result.StripeCount.Should().Be(3);
    }

    private sealed class BusinessSubscriptionInvoiceTestDbContext : DbContext, IAppDbContext
    {
        private BusinessSubscriptionInvoiceTestDbContext(DbContextOptions<BusinessSubscriptionInvoiceTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessSubscriptionInvoiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessSubscriptionInvoiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_subscription_invoice_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessSubscriptionInvoiceTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BillingPlan>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Code).IsRequired();
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.FeaturesJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessSubscription>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SubscriptionInvoice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.LinesJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
