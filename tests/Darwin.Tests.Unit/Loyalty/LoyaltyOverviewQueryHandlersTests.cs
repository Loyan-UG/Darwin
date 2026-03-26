using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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

        db.Set<Business>().AddRange(
            new Business { Id = firstBusinessId, Name = "Cafe Aurora" },
            new Business { Id = secondBusinessId, Name = "Backwerk Mitte" });
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

        var handler = new GetMyLoyaltyOverviewHandler(db, new StubCurrentUserService(userId));

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalAccounts.Should().Be(2);
        result.Value.ActiveAccounts.Should().Be(1);
        result.Value.TotalPointsBalance.Should().Be(420);
        result.Value.TotalLifetimePoints.Should().Be(1450);
        result.Value.LastAccrualAtUtc.Should().Be(new DateTime(2030, 1, 3, 9, 0, 0, DateTimeKind.Utc));
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

        var handler = new GetMyLoyaltyBusinessDashboardHandler(db, new StubCurrentUserService(userId));

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AvailableRewardsCount.Should().Be(2);
        result.Value.RedeemableRewardsCount.Should().Be(1);
        result.Value.NextReward.Should().NotBeNull();
        result.Value.NextReward!.Name.Should().Be("Free Espresso");
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
