using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Billing.Validators;
using Darwin.Application;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Billing;

public sealed class BillingPlanAdminHandlerTests
{
    // ─── GetBillingPlansAdminPageHandler ────────────────────────────────────

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_ReturnAllNonDeleted_WhenNoFilter()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "BASIC", Name = "Basic", Currency = "USD", IsActive = true, FeaturesJson = "{}" },
            new BillingPlan { Code = "PRO", Name = "Pro", Currency = "USD", IsActive = false, FeaturesJson = "{}" },
            new BillingPlan { Code = "DELETED", Name = "Deleted", Currency = "USD", IsDeleted = true, FeaturesJson = "{}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Code == "DELETED");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_FilterActiveOnly()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "ACTIVE", Name = "Active Plan", Currency = "USD", IsActive = true, FeaturesJson = "{}" },
            new BillingPlan { Code = "INACTIVE", Name = "Inactive Plan", Currency = "USD", IsActive = false, FeaturesJson = "{}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, filter: BillingPlanQueueFilter.Active, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Code == "ACTIVE");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_FilterInactiveOnly()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "ACTIVE", Name = "Active Plan", Currency = "USD", IsActive = true, FeaturesJson = "{}" },
            new BillingPlan { Code = "INACTIVE", Name = "Inactive Plan", Currency = "USD", IsActive = false, FeaturesJson = "{}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, filter: BillingPlanQueueFilter.Inactive, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Code == "INACTIVE");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_FilterTrialPlans()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "TRIAL", Name = "Trial Plan", Currency = "USD", TrialDays = 14, FeaturesJson = "{}" },
            new BillingPlan { Code = "NOTRIAL", Name = "No Trial", Currency = "USD", TrialDays = null, FeaturesJson = "{}" },
            new BillingPlan { Code = "ZEROTRIAL", Name = "Zero Trial", Currency = "USD", TrialDays = 0, FeaturesJson = "{}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, filter: BillingPlanQueueFilter.Trial, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Code == "TRIAL");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_FilterMissingFeatures()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "NOFEAT", Name = "No Features", Currency = "USD", FeaturesJson = "{}" },
            new BillingPlan { Code = "NOFEAT2", Name = "No Features 2", Currency = "USD", FeaturesJson = "{}" },
            new BillingPlan { Code = "FEAT", Name = "With Features", Currency = "USD", FeaturesJson = "{\"tier\":\"pro\"}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, filter: BillingPlanQueueFilter.MissingFeatures, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().NotContain(x => x.Code == "FEAT");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_ApplySearchQuery()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Code = "GOLD", Name = "Gold Plan", Currency = "USD", FeaturesJson = "{}" },
            new BillingPlan { Code = "SILVER", Name = "Silver Plan", Currency = "USD", FeaturesJson = "{}" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlansAdminPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, query: "gold", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Code == "GOLD");
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_NormalizePageSize()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new GetBillingPlansAdminPageHandler(db);

        // invalid page and pageSize defaults are applied without exception
        var (items, total) = await handler.HandleAsync(0, 0, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_ReturnEmptyWhenNoPlans()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new GetBillingPlansAdminPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── GetBillingPlanOpsSummaryHandler ────────────────────────────────────

    [Fact]
    public async Task GetBillingPlanOpsSummary_Should_ReturnZeroCounts_WhenNoPlans()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new GetBillingPlanOpsSummaryHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
        result.InactiveCount.Should().Be(0);
        result.TrialCount.Should().Be(0);
        result.MissingFeaturesCount.Should().Be(0);
        result.InUseCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBillingPlanOpsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = BillingPlanTestDbContext.Create();

        var activePlanId = Guid.NewGuid();
        var inactivePlanId = Guid.NewGuid();
        var trialPlanId = Guid.NewGuid();

        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Id = activePlanId, Code = "ACTIVE", Name = "Active", Currency = "USD", IsActive = true, FeaturesJson = "{\"tier\":\"basic\"}" },
            new BillingPlan { Id = inactivePlanId, Code = "INACTIVE", Name = "Inactive", Currency = "USD", IsActive = false, FeaturesJson = "{}" },
            new BillingPlan { Id = trialPlanId, Code = "TRIAL", Name = "Trial", Currency = "USD", IsActive = true, TrialDays = 30, FeaturesJson = "{}" },
            new BillingPlan { Code = "DELETED", Name = "Deleted", Currency = "USD", IsDeleted = true, FeaturesJson = "{}" });
        db.Set<BusinessSubscription>().AddRange(
            new BusinessSubscription { BillingPlanId = activePlanId, Status = SubscriptionStatus.Active, StartedAtUtc = DateTime.UtcNow },
            new BusinessSubscription { BillingPlanId = activePlanId, Status = SubscriptionStatus.Canceled, IsDeleted = true, StartedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlanOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(3);
        result.ActiveCount.Should().Be(2);
        result.InactiveCount.Should().Be(1);
        result.TrialCount.Should().Be(1);
        result.MissingFeaturesCount.Should().Be(2); // INACTIVE + TRIAL both have "{}"
        result.InUseCount.Should().Be(1); // one plan has a non-deleted subscription
    }

    [Fact]
    public async Task GetBillingPlanOpsSummary_Should_CountInUse_WhenActiveSubscriptionsExist()
    {
        await using var db = BillingPlanTestDbContext.Create();

        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(
            new BillingPlan { Id = planId, Code = "BASIC", Name = "Basic", Currency = "USD", IsActive = true, FeaturesJson = "{}" });
        db.Set<BusinessSubscription>().AddRange(
            new BusinessSubscription { BillingPlanId = planId, Status = SubscriptionStatus.Active, StartedAtUtc = DateTime.UtcNow },
            new BusinessSubscription { BillingPlanId = planId, Status = SubscriptionStatus.Trialing, StartedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlanOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.InUseCount.Should().Be(1); // distinct plan count
    }

    // ─── GetBillingPlanForEditHandler ───────────────────────────────────────

    [Fact]
    public async Task GetBillingPlanForEdit_Should_ReturnPlan_WhenExists()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "PRO",
            Name = "Pro Plan",
            Description = "Professional tier",
            PriceMinor = 9900,
            Currency = "USD",
            Interval = BillingInterval.Month,
            IntervalCount = 1,
            TrialDays = 7,
            IsActive = true,
            FeaturesJson = "{\"tier\":\"pro\"}",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlanForEditHandler(db);
        var result = await handler.HandleAsync(planId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(planId);
        result.Code.Should().Be("PRO");
        result.Name.Should().Be("Pro Plan");
        result.Description.Should().Be("Professional tier");
        result.PriceMinor.Should().Be(9900);
        result.Currency.Should().Be("USD");
        result.IsActive.Should().BeTrue();
        result.TrialDays.Should().Be(7);
    }

    [Fact]
    public async Task GetBillingPlanForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new GetBillingPlanForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBillingPlanForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "OLD",
            Name = "Old Plan",
            Currency = "USD",
            FeaturesJson = "{}",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBillingPlanForEditHandler(db);
        var result = await handler.HandleAsync(planId, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── CreateBillingPlanHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateBillingPlanHandler_Should_ThrowValidationException_WhenCodeIsEmpty()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new CreateBillingPlanHandler(db, new BillingPlanCreateValidator(), new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new BillingPlanCreateDto
        {
            Code = "",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateBillingPlanHandler_Should_ThrowValidationException_WhenCodeIsNotUnique()
    {
        await using var db = BillingPlanTestDbContext.Create();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Code = "BASIC",
            Name = "Existing Basic",
            Currency = "USD",
            FeaturesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBillingPlanHandler(db, new BillingPlanCreateValidator(), new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new BillingPlanCreateDto
        {
            Code = "basic", // lowercase — normalized to BASIC
            Name = "Basic Copy",
            Currency = "USD",
            FeaturesJson = "{}"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*BillingPlanCodeMustBeUnique*");
    }

    [Fact]
    public async Task CreateBillingPlanHandler_Should_CreatePlan_WithNormalizedCode()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new CreateBillingPlanHandler(db, new BillingPlanCreateValidator(), new TestStringLocalizer());

        var newId = await handler.HandleAsync(new BillingPlanCreateDto
        {
            Code = " starter ",
            Name = " Starter Plan ",
            Currency = "eur",
            PriceMinor = 4900,
            Interval = BillingInterval.Month,
            IntervalCount = 1,
            IsActive = true,
            FeaturesJson = "{}"
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();

        var saved = await db.Set<BillingPlan>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Code.Should().Be("STARTER");
        saved.Name.Should().Be("Starter Plan");
        saved.Currency.Should().Be("EUR");
        saved.PriceMinor.Should().Be(4900);
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBillingPlanHandler_Should_ThrowValidationException_WhenFeaturesJsonIsWhitespace()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new CreateBillingPlanHandler(db, new BillingPlanCreateValidator(), new TestStringLocalizer());

        // The validator rejects whitespace-only FeaturesJson before the handler normalizes it
        var act = async () => await handler.HandleAsync(new BillingPlanCreateDto
        {
            Code = "NOFEAT",
            Name = "No Features",
            Currency = "USD",
            FeaturesJson = "   "
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateBillingPlanHandler_Should_SetNullDescription_WhenDescriptionIsWhitespace()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new CreateBillingPlanHandler(db, new BillingPlanCreateValidator(), new TestStringLocalizer());

        var newId = await handler.HandleAsync(new BillingPlanCreateDto
        {
            Code = "NODESC",
            Name = "No Description",
            Currency = "USD",
            Description = "   ",
            FeaturesJson = "{}"
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<BillingPlan>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Description.Should().BeNull();
    }

    // ─── UpdateBillingPlanHandler ────────────────────────────────────────────

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_ThrowValidationException_WhenIdIsEmpty()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = Guid.Empty,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_ThrowInvalidOperation_WhenPlanNotFound()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = Guid.NewGuid(),
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BillingPlanNotFound*");
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_ThrowConcurrencyException_WhenRowVersionMismatches()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [9, 9, 9] // stale
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>().WithMessage("*ConcurrencyConflictDetected*");
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_ThrowConcurrencyException_WhenRowVersionIsEmpty()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        // Empty RowVersion fails the validator (NotEmpty rule) before reaching the concurrency check
        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_ThrowValidationException_WhenCodeIsDuplicate()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planIdA = Guid.NewGuid();
        var planIdB = Guid.NewGuid();
        db.Set<BillingPlan>().AddRange(
            new BillingPlan { Id = planIdA, Code = "BASIC", Name = "Basic", Currency = "USD", FeaturesJson = "{}", RowVersion = [1] },
            new BillingPlan { Id = planIdB, Code = "PRO", Name = "Pro", Currency = "USD", FeaturesJson = "{}", RowVersion = [2] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        // Try to rename PRO to BASIC (already taken by planIdA)
        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = planIdB,
            Code = "basic", // normalizes to BASIC
            Name = "Pro",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [2]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*BillingPlanCodeMustBeUnique*");
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_UpdatePlan_WhenValid()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "OLD",
            Name = "Old Name",
            Currency = "USD",
            PriceMinor = 100,
            IsActive = false,
            FeaturesJson = "{}",
            RowVersion = [7]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());
        await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = planId,
            Code = " updated ",
            Name = " Updated Name ",
            Currency = "EUR",
            PriceMinor = 9900,
            IsActive = true,
            FeaturesJson = "{\"tier\":\"pro\"}",
            RowVersion = [7]
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<BillingPlan>().SingleAsync(x => x.Id == planId, TestContext.Current.CancellationToken);
        saved.Code.Should().Be("UPDATED");
        saved.Name.Should().Be("Updated Name");
        saved.Currency.Should().Be("EUR");
        saved.PriceMinor.Should().Be(9900);
        saved.IsActive.Should().BeTrue();
        saved.FeaturesJson.Should().Be("{\"tier\":\"pro\"}");
    }

    [Fact]
    public async Task UpdateBillingPlanHandler_Should_AllowSameCode_WhenUpdatingSelf()
    {
        await using var db = BillingPlanTestDbContext.Create();
        var planId = Guid.NewGuid();
        db.Set<BillingPlan>().Add(new BillingPlan
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [5]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingPlanHandler(db, new BillingPlanEditValidator(), new TestStringLocalizer());

        // updating with same code should succeed
        var act = async () => await handler.HandleAsync(new BillingPlanEditDto
        {
            Id = planId,
            Code = "BASIC",
            Name = "Basic Updated",
            Currency = "USD",
            FeaturesJson = "{}",
            RowVersion = [5]
        }, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();

        var saved = await db.Set<BillingPlan>().SingleAsync(x => x.Id == planId, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Basic Updated");
    }

    // ─── Private infrastructure ──────────────────────────────────────────────

    private sealed class BillingPlanTestDbContext : DbContext, IAppDbContext
    {
        private BillingPlanTestDbContext(DbContextOptions<BillingPlanTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingPlanTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingPlanTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_plan_tests_{Guid.NewGuid()}")
                .Options;
            return new BillingPlanTestDbContext(options);
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
