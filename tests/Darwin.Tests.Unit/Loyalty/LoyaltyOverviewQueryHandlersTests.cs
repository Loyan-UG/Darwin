using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers aggregated member-facing loyalty overview queries.
/// </summary>
public sealed class LoyaltyOverviewQueryHandlersTests
{
    [Fact]
    public async Task GetMyLoyaltyOverview_Should_AggregateBalancesAcrossAccounts()
    {
        await using var db = LoyaltyOverviewTestDbContext.Create();
        var userId = Guid.NewGuid();
        var firstBusinessId = Guid.NewGuid();
        var secondBusinessId = Guid.NewGuid();
        var firstProgramId = Guid.NewGuid();
        var secondProgramId = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = firstBusinessId, Name = "Cafe Aurora" },
            new Business { Id = secondBusinessId, Name = "Backwerk Mitte" });
        db.Set<LoyaltyProgram>().AddRange(
            new LoyaltyProgram
            {
                Id = firstProgramId,
                BusinessId = firstBusinessId,
                Name = "Aurora Rewards",
                IsActive = true
            },
            new LoyaltyProgram
            {
                Id = secondProgramId,
                BusinessId = secondBusinessId,
                Name = "Backwerk Rewards",
                IsActive = true
            });
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier
            {
                Id = Guid.NewGuid(),
                LoyaltyProgramId = firstProgramId,
                PointsRequired = 500,
                Description = "Free Cake",
                AllowSelfRedemption = true
            },
            new LoyaltyRewardTier
            {
                Id = Guid.NewGuid(),
                LoyaltyProgramId = secondProgramId,
                PointsRequired = 200,
                Description = "Free Coffee",
                AllowSelfRedemption = true
            });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = firstBusinessId,
                UserId = userId,
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 300,
                LifetimePoints = 1000,
                LastAccrualAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc)
            },
            new LoyaltyAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = secondBusinessId,
                UserId = userId,
                Status = LoyaltyAccountStatus.Suspended,
                PointsBalance = 120,
                LifetimePoints = 450,
                LastAccrualAtUtc = new DateTime(2030, 1, 3, 9, 0, 0, DateTimeKind.Utc)
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyOverviewHandler(
            db,
            new StubCurrentUserService(userId),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalAccounts.Should().Be(2);
        result.Value.ActiveAccounts.Should().Be(1);
        result.Value.TotalPointsBalance.Should().Be(420);
        result.Value.TotalLifetimePoints.Should().Be(1450);
        result.Value.LastAccrualAtUtc.Should().Be(new DateTime(2030, 1, 3, 9, 0, 0, DateTimeKind.Utc));
        result.Value.Accounts.Should().HaveCount(2);
        result.Value.Accounts[0].NextRewardTitle.Should().Be("Free Cake");
        result.Value.Accounts[0].PointsToNextReward.Should().Be(200);
        result.Value.Accounts[0].NextRewardProgressPercent.Should().Be(60m);
        result.Value.Accounts[1].NextRewardTitle.Should().Be("Free Coffee");
        result.Value.Accounts[1].PointsToNextReward.Should().Be(80);
        result.Value.Accounts[1].NextRewardProgressPercent.Should().Be(60m);
    }

    [Fact]
    public async Task GetMyLoyaltyOverview_Should_ReturnEmptyOverview_WhenUserHasNoAccounts()
    {
        // Proves members with zero loyalty accounts receive an empty overview with null LastAccrualAtUtc
        // instead of an aggregate exception (backlog item §7).
        await using var db = LoyaltyOverviewTestDbContext.Create();
        var userId = Guid.NewGuid();
        var handler = new GetMyLoyaltyOverviewHandler(
            db,
            new StubCurrentUserService(userId),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalAccounts.Should().Be(0);
        result.Value.ActiveAccounts.Should().Be(0);
        result.Value.TotalPointsBalance.Should().Be(0);
        result.Value.TotalLifetimePoints.Should().Be(0);
        result.Value.LastAccrualAtUtc.Should().BeNull("no accounts means no accrual, not an aggregate exception");
        result.Value.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLoyaltyBusinessDashboard_Should_ReturnRewardCountsNextRewardAndRecentTransactions()
    {
        await using var db = LoyaltyOverviewTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 180,
            LifetimePoints = 600
        });
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "Aurora Rewards",
            IsActive = true
        });
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier
            {
                Id = Guid.NewGuid(),
                LoyaltyProgramId = programId,
                PointsRequired = 120,
                Description = "Free Cookie",
                AllowSelfRedemption = true
            },
            new LoyaltyRewardTier
            {
                Id = Guid.NewGuid(),
                LoyaltyProgramId = programId,
                PointsRequired = 250,
                Description = "Free Espresso",
                AllowSelfRedemption = false
            });
        db.Set<LoyaltyPointsTransaction>().AddRange(
            new LoyaltyPointsTransaction
            {
                Id = Guid.NewGuid(),
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 50,
                Reference = "ORD-1001",
                CreatedAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc)
            },
            new LoyaltyPointsTransaction
            {
                Id = Guid.NewGuid(),
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 30,
                Reference = "ORD-1002",
                CreatedAtUtc = new DateTime(2030, 1, 3, 9, 0, 0, DateTimeKind.Utc)
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessDashboardHandler(
            db,
            new StubCurrentUserService(userId),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AvailableRewardsCount.Should().Be(2);
        result.Value.RedeemableRewardsCount.Should().Be(1);
        result.Value.NextReward.Should().NotBeNull();
        result.Value.NextReward!.Name.Should().Be("Free Espresso");
        result.Value.Account.NextRewardTitle.Should().Be("Free Espresso");
        result.Value.Account.PointsToNextReward.Should().Be(70);
        result.Value.Account.NextRewardRequiredPoints.Should().Be(250);
        result.Value.Account.NextRewardProgressPercent.Should().Be(72m);
        result.Value.PointsToNextReward.Should().Be(70);
        result.Value.NextRewardRequiredPoints.Should().Be(250);
        result.Value.NextRewardProgressPercent.Should().Be(72m);
        result.Value.ExpiryTrackingEnabled.Should().BeFalse();
        result.Value.PointsExpiringSoon.Should().Be(0);
        result.Value.NextPointsExpiryAtUtc.Should().BeNull();
        result.Value.RecentTransactions.Should().HaveCount(2);
        result.Value.RecentTransactions[0].Reference.Should().Be("ORD-1002");
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId)
        {
            _userId = userId;
        }

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments), false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyOverviewTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyOverviewTestDbContext(DbContextOptions<LoyaltyOverviewTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyOverviewTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyOverviewTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_overview_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyOverviewTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.EngagementStats);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.LoyaltyAccountId);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyProgram>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.RewardTiers).WithOne().HasForeignKey(x => x.LoyaltyProgramId);
            });

            modelBuilder.Entity<LoyaltyRewardTier>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
