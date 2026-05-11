using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for WebAdmin Billing/Subscriptions query handlers:
/// <see cref="GetBusinessSubscriptionsPageHandler"/> and
/// <see cref="GetBusinessSubscriptionOpsSummaryHandler"/>.
/// </summary>
public sealed class BillingSubscriptionQueryHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static BusinessSubscription MakeSubscription(
        Guid? businessId = null,
        Guid? planId = null,
        string provider = "Stripe",
        SubscriptionStatus status = SubscriptionStatus.Active,
        bool isDeleted = false,
        bool cancelAtPeriodEnd = false,
        string? providerSubscriptionId = null,
        string? providerCheckoutSessionId = null,
        string? providerCustomerId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            BillingPlanId = planId ?? Guid.NewGuid(),
            Provider = provider,
            Status = status,
            IsDeleted = isDeleted,
            CancelAtPeriodEnd = cancelAtPeriodEnd,
            ProviderSubscriptionId = providerSubscriptionId,
            ProviderCheckoutSessionId = providerCheckoutSessionId,
            ProviderCustomerId = providerCustomerId,
            StartedAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Currency = "EUR",
            UnitPriceMinor = 2900
        };

    private static BillingPlan MakePlan(
        string code = "starter",
        string name = "Starter",
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Currency = "EUR",
            PriceMinor = 2900,
            FeaturesJson = "{}"
        };

    private static Business MakeBusiness(
        string name = "Test Business",
        string? contactEmail = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactEmail = contactEmail
        };

    // ═══════════════════════════════════════════════════════════════════════
    // GetBusinessSubscriptionsPageHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Return_Empty_When_No_Subscriptions()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Exclude_SoftDeleted_Subscriptions()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(status: SubscriptionStatus.Active),
            MakeSubscription(status: SubscriptionStatus.Active, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1, "soft-deleted subscriptions must be excluded");
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Normalize_Page_When_Below_One()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().Add(MakeSubscription());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        // page=0 should be normalized to 1
        var (items, _) = await handler.HandleAsync(0, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Normalize_PageSize_When_Below_One()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        for (var i = 0; i < 3; i++)
            db.Set<BusinessSubscription>().Add(MakeSubscription());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        // pageSize=0 should be normalized to default (20)
        var (items, total) = await handler.HandleAsync(1, 0, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Normalize_PageSize_Above_Maximum()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        for (var i = 0; i < 3; i++)
            db.Set<BusinessSubscription>().Add(MakeSubscription());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        // pageSize=999 should be clamped to 200
        var (items, total) = await handler.HandleAsync(1, 999, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_Active_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(status: SubscriptionStatus.Active),
            MakeSubscription(status: SubscriptionStatus.Trialing),
            MakeSubscription(status: SubscriptionStatus.Canceled));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.Active, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        total.Should().Be(1);
        items[0].Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_Trialing_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(status: SubscriptionStatus.Active),
            MakeSubscription(status: SubscriptionStatus.Trialing));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.Trialing, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_PastDue_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(status: SubscriptionStatus.PastDue),
            MakeSubscription(status: SubscriptionStatus.Active));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.PastDue, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_Canceled_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(status: SubscriptionStatus.Canceled),
            MakeSubscription(status: SubscriptionStatus.Active));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.Canceled, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(SubscriptionStatus.Canceled);
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_Stripe_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(provider: "Stripe"),
            MakeSubscription(provider: "Manual"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.Stripe, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Provider.Should().Be("Stripe");
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_MissingProviderReference_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(providerSubscriptionId: null, providerCheckoutSessionId: null),
            MakeSubscription(providerSubscriptionId: "sub_123"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.MissingProviderReference, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].MissingProviderReference.Should().BeTrue();
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Filter_By_CancelAtPeriodEnd_Queue()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(cancelAtPeriodEnd: true),
            MakeSubscription(cancelAtPeriodEnd: false));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, filter: BusinessSubscriptionQueueFilter.CancelAtPeriodEnd, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].CancelAtPeriodEnd.Should().BeTrue();
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Enrich_BusinessName_From_Business_Entity()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        var business = MakeBusiness("Acme Corp", "contact@acme.com");
        var plan = MakePlan("growth", "Growth");
        db.Set<Business>().Add(business);
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(MakeSubscription(businessId: business.Id, planId: plan.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].BusinessName.Should().Be("Acme Corp");
        items[0].BusinessContactEmail.Should().Be("contact@acme.com");
        items[0].PlanCode.Should().Be("growth");
        items[0].PlanName.Should().Be("Growth");
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Compute_ProviderReferenceState_StripeMissingRef()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().Add(
            MakeSubscription(provider: "Stripe", providerSubscriptionId: null, providerCheckoutSessionId: null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items[0].ProviderReferenceState.Should().Be("Stripe subscription ref missing");
        items[0].MissingProviderReference.Should().BeTrue();
        items[0].IsStripe.Should().BeTrue();
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Compute_ProviderReferenceState_ActiveOnProvider()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().Add(
            MakeSubscription(
                provider: "Stripe",
                status: SubscriptionStatus.Active,
                providerSubscriptionId: "sub_active"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items[0].ProviderReferenceState.Should().Be("Active on provider");
    }

    [Fact]
    public async Task GetBusinessSubscriptionsPage_Should_Compute_ProviderReferenceState_CancelAtPeriodEnd()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().Add(
            MakeSubscription(
                provider: "Stripe",
                status: SubscriptionStatus.Active,
                cancelAtPeriodEnd: true,
                providerSubscriptionId: "sub_cancel_end"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items[0].ProviderReferenceState.Should().Be("Cancel at period end");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetBusinessSubscriptionOpsSummaryHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBusinessSubscriptionOpsSummary_Should_Return_Zero_Counts_When_Empty()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();

        var handler = new GetBusinessSubscriptionOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.ActiveCount.Should().Be(0);
        summary.TrialingCount.Should().Be(0);
        summary.PastDueCount.Should().Be(0);
        summary.CanceledCount.Should().Be(0);
        summary.StripeCount.Should().Be(0);
        summary.MissingProviderReferenceCount.Should().Be(0);
        summary.CancelAtPeriodEndCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBusinessSubscriptionOpsSummary_Should_Exclude_SoftDeleted_In_All_Counts()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Active),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Active, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(1, "soft-deleted subscriptions must be excluded from all counts");
        summary.ActiveCount.Should().Be(1);
        summary.StripeCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessSubscriptionOpsSummary_Should_Count_All_Status_And_Provider_Groups()
    {
        await using var db = SubscriptionQueryTestDbContext.Create();
        db.Set<BusinessSubscription>().AddRange(
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Active, providerSubscriptionId: "sub_active_1"),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Active, providerSubscriptionId: "sub_active_2"),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Trialing, providerSubscriptionId: "sub_trial"),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.PastDue, providerSubscriptionId: "sub_pastdue"),
            MakeSubscription(provider: "Manual", status: SubscriptionStatus.Canceled, providerSubscriptionId: "sub_manual"),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Active, cancelAtPeriodEnd: true, providerSubscriptionId: "sub_cancel"),
            MakeSubscription(provider: "Stripe", status: SubscriptionStatus.Incomplete, providerSubscriptionId: null, providerCheckoutSessionId: null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(7);
        summary.ActiveCount.Should().Be(3, "two plain active + one cancel-at-period-end which is still Active");
        summary.TrialingCount.Should().Be(1);
        summary.PastDueCount.Should().Be(1);
        summary.CanceledCount.Should().Be(1);
        summary.StripeCount.Should().Be(6, "all Stripe subscriptions should be counted");
        summary.CancelAtPeriodEndCount.Should().Be(1);
        summary.MissingProviderReferenceCount.Should().Be(1, "only the Incomplete subscription has no provider references");
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────

    private sealed class SubscriptionQueryTestDbContext : DbContext, IAppDbContext
    {
        private SubscriptionQueryTestDbContext(DbContextOptions<SubscriptionQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static SubscriptionQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SubscriptionQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_subscription_query_{Guid.NewGuid()}")
                .Options;
            return new SubscriptionQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BusinessSubscription>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BillingPlan>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Code).IsRequired();
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.FeaturesJson).IsRequired();
            });

            modelBuilder.Entity<Business>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Ignore(x => x.Locations);
                b.Ignore(x => x.Members);
                b.Ignore(x => x.Invitations);
                b.Ignore(x => x.Subscriptions);
                b.Ignore(x => x.Favorites);
                b.Ignore(x => x.Likes);
                b.Ignore(x => x.Reviews);
                b.Ignore(x => x.StaffQrCodes);
                b.Ignore(x => x.AnalyticsExportJobs);
            });
        }
    }
}
