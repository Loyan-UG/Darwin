using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for the consumer/business-facing Billing handlers:
/// <see cref="GetBillingPlansHandler"/>,
/// <see cref="GetBusinessSubscriptionStatusHandler"/>,
/// <see cref="SetCancelAtPeriodEndHandler"/>, and
/// <see cref="CreateSubscriptionCheckoutIntentHandler"/>.
/// </summary>
public sealed class BillingSubscriptionHandlersTests
{
    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static IClock CreateClock(DateTime utcNow)
    {
        var mock = new Mock<IClock>();
        mock.Setup(c => c.UtcNow).Returns(utcNow);
        return mock.Object;
    }

    private static BillingPlan MakePlan(
        string code = "starter",
        string name = "Starter",
        bool isActive = true,
        bool isDeleted = false,
        long priceMinor = 2900,
        string currency = "EUR") =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsActive = isActive,
            IsDeleted = isDeleted,
            PriceMinor = priceMinor,
            Currency = currency,
            FeaturesJson = "{}"
        };

    private static BusinessSubscription MakeSubscription(
        Guid businessId,
        Guid planId,
        SubscriptionStatus status = SubscriptionStatus.Active,
        bool isDeleted = false,
        byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            BillingPlanId = planId,
            Provider = "Stripe",
            Status = status,
            StartedAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UnitPriceMinor = 2900,
            Currency = "EUR",
            IsDeleted = isDeleted,
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    // ═══════════════════════════════════════════════════════════════════════
    // GetBillingPlansHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBillingPlans_Should_Return_All_NonDeleted_When_ActiveOnly_False()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            MakePlan("BASIC", isActive: true),
            MakePlan("PRO", isActive: false),
            MakePlan("DELETED", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansHandler(db);
        var dto = await handler.HandleAsync(activeOnly: false, ct: TestContext.Current.CancellationToken);

        dto.Items.Should().HaveCount(2, "soft-deleted plans must be excluded");
        dto.Items.Should().NotContain(x => x.Code == "DELETED");
    }

    [Fact]
    public async Task GetBillingPlans_Should_Return_Only_Active_When_ActiveOnly_True()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            MakePlan("ACTIVE", isActive: true),
            MakePlan("INACTIVE", isActive: false));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansHandler(db);
        var dto = await handler.HandleAsync(activeOnly: true, ct: TestContext.Current.CancellationToken);

        dto.Items.Should().ContainSingle(x => x.Code == "ACTIVE");
    }

    [Fact]
    public async Task GetBillingPlans_Should_Return_Empty_List_When_No_Plans()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();

        var handler = new GetBillingPlansHandler(db);
        var dto = await handler.HandleAsync(activeOnly: false, ct: TestContext.Current.CancellationToken);

        dto.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBillingPlans_Should_Sort_By_Price_Then_Name()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            MakePlan("B", priceMinor: 5000),
            MakePlan("A", priceMinor: 1000),
            MakePlan("C", priceMinor: 5000));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansHandler(db);
        var dto = await handler.HandleAsync(activeOnly: false, ct: TestContext.Current.CancellationToken);

        dto.Items[0].Code.Should().Be("A", "cheapest plan should come first");
        // B and C have the same price; they are sorted by name alphabetically
        dto.Items[1].Code.Should().Be("B");
        dto.Items[2].Code.Should().Be("C");
    }

    [Fact]
    public async Task GetBillingPlans_Should_Map_Fields_Correctly()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var plan = MakePlan("TRIAL", name: "Trial Plan", priceMinor: 0, currency: "USD");
        plan.TrialDays = 14;
        plan.Interval = BillingInterval.Month;
        plan.IntervalCount = 3;
        db.Set<BillingPlan>().Add(plan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansHandler(db);
        var dto = await handler.HandleAsync(activeOnly: false, ct: TestContext.Current.CancellationToken);

        var item = dto.Items.Should().ContainSingle().Subject;
        item.Code.Should().Be("TRIAL");
        item.Name.Should().Be("Trial Plan");
        item.PriceMinor.Should().Be(0);
        item.Currency.Should().Be("USD");
        item.TrialDays.Should().Be(14);
        item.Interval.Should().Be("Month");
        item.IntervalCount.Should().Be(3);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetBusinessSubscriptionStatusHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Fail_When_BusinessId_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty businessId is not a valid request");
    }

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Return_NoSubscription_When_None_Found()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeFalse();
        result.Value.Status.Should().Be("None");
    }

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Return_NoSubscription_When_Subscription_Is_Deleted()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan();
        var subscription = MakeSubscription(businessId, plan.Id, isDeleted: true);
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeFalse("deleted subscriptions must be excluded");
    }

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Return_NoSubscription_When_Plan_Is_Deleted()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan(isDeleted: true);
        var subscription = MakeSubscription(businessId, plan.Id);
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeFalse("subscriptions with deleted plans must be excluded");
    }

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Return_Subscription_For_Valid_Business()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan("starter", "Starter", priceMinor: 2900, currency: "EUR");
        var subscription = MakeSubscription(businessId, plan.Id, SubscriptionStatus.Active);
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeTrue();
        result.Value.Status.Should().Be("Active");
        result.Value.PlanCode.Should().Be("starter");
        result.Value.Currency.Should().Be("EUR");
        result.Value.UnitPriceMinor.Should().Be(2900);
    }

    [Fact]
    public async Task GetBusinessSubscriptionStatus_Should_Return_Most_Recent_Subscription_For_Business()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan();

        var older = MakeSubscription(businessId, plan.Id, SubscriptionStatus.Canceled);
        older.StartedAtUtc = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var newer = MakeSubscription(businessId, plan.Id, SubscriptionStatus.Active);
        newer.StartedAtUtc = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSubscriptionStatusHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be("Active", "the most recently started subscription should be returned");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SetCancelAtPeriodEndHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Fail_When_BusinessId_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), true, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty businessId is not a valid request");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Fail_When_SubscriptionId_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty, true, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty subscriptionId is not a valid request");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Fail_When_Subscription_Not_Found()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), true, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a non-existent subscription must return failure");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Fail_When_RowVersion_Is_Stale()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan();
        var subscription = MakeSubscription(businessId, plan.Id, rowVersion: new byte[] { 1, 2, 3 });
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());
        var result = await handler.HandleAsync(
            businessId, subscription.Id, true, new byte[] { 9, 9, 9 },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a stale RowVersion must cause a concurrency failure");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Fail_When_RowVersion_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan();
        var subscription = MakeSubscription(businessId, plan.Id, rowVersion: new byte[] { 1 });
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());
        var result = await handler.HandleAsync(
            businessId, subscription.Id, true, Array.Empty<byte>(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("an empty RowVersion must be rejected");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Set_Flag_And_Record_CanceledAt()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var now = new DateTime(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var businessId = Guid.NewGuid();
        var plan = MakePlan();
        var rowVersion = new byte[] { 1 };
        var subscription = MakeSubscription(businessId, plan.Id, rowVersion: rowVersion);
        subscription.CancelAtPeriodEnd = false;
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(now), CreateLocalizer());
        var result = await handler.HandleAsync(
            businessId, subscription.Id, true, rowVersion,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.CancelAtPeriodEnd.Should().BeTrue();
        result.Value.CanceledAtUtc.Should().Be(now, "CanceledAtUtc should be set to the clock time");
    }

    [Fact]
    public async Task SetCancelAtPeriodEnd_Should_Clear_CanceledAt_When_Unscheduling_Cancellation()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var plan = MakePlan();
        var rowVersion = new byte[] { 2 };
        var subscription = MakeSubscription(businessId, plan.Id, rowVersion: rowVersion);
        subscription.CancelAtPeriodEnd = true;
        subscription.CanceledAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Set<BillingPlan>().Add(plan);
        db.Set<BusinessSubscription>().Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetCancelAtPeriodEndHandler(db, CreateClock(DateTime.UtcNow), CreateLocalizer());
        var result = await handler.HandleAsync(
            businessId, subscription.Id, false, rowVersion,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.CancelAtPeriodEnd.Should().BeFalse();
        result.Value.CanceledAtUtc.Should().BeNull("unscheduling cancellation must clear CanceledAtUtc");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CreateSubscriptionCheckoutIntentHandler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Fail_When_BusinessId_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());

        var result = await handler.ValidateAsync(Guid.Empty, Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty businessId is not valid");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Fail_When_PlanId_Is_Empty()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());

        var result = await handler.ValidateAsync(Guid.NewGuid(), Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty planId is not valid");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Fail_When_Plan_Not_Found()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());

        var result = await handler.ValidateAsync(Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a non-existent plan must cause failure");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Fail_When_Plan_Is_Inactive()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var plan = MakePlan(isActive: false);
        db.Set<BillingPlan>().Add(plan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());
        var result = await handler.ValidateAsync(Guid.NewGuid(), plan.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("an inactive plan is not available for subscription");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Fail_When_Business_Not_Found()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var plan = MakePlan(isActive: true);
        db.Set<BillingPlan>().Add(plan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());
        var result = await handler.ValidateAsync(Guid.NewGuid(), plan.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a non-existent business must cause failure");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntent_Should_Succeed_When_Plan_And_Business_Are_Valid()
    {
        await using var db = BillingSubscriptionTestDbContext.Create();
        var plan = MakePlan(isActive: true);
        var business = new Business { Id = Guid.NewGuid(), Name = "Test Business" };
        db.Set<BillingPlan>().Add(plan);
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSubscriptionCheckoutIntentHandler(db, CreateLocalizer());
        var result = await handler.ValidateAsync(business.Id, plan.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────

    private sealed class BillingSubscriptionTestDbContext : DbContext, IAppDbContext
    {
        private BillingSubscriptionTestDbContext(DbContextOptions<BillingSubscriptionTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingSubscriptionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingSubscriptionTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_subscription_{Guid.NewGuid()}")
                .Options;
            return new BillingSubscriptionTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BillingPlan>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Code).IsRequired();
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.FeaturesJson).IsRequired();
            });

            modelBuilder.Entity<BusinessSubscription>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRowVersion();
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
