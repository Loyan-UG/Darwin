using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers Loyalty reward tier and program read-only query handlers:
/// <see cref="GetLoyaltyRewardTiersPageHandler"/> (paged + summary),
/// <see cref="GetLoyaltyProgramForEditHandler"/>, and
/// <see cref="GetLoyaltyRewardTierForEditHandler"/>.
/// </summary>
public sealed class LoyaltyRewardTierQueryHandlersTests
{
    // ─── GetLoyaltyRewardTiersPageHandler ─────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_ReturnAllNonDeleted_ForProgram()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        var otherProgramId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, Description = "Tier A", RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, Description = "Tier B", RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = otherProgramId, PointsRequired = 100, Description = "Other Program Tier", RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 300, Description = "Deleted Tier", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Description == "Deleted Tier");
        items.Should().NotContain(x => x.LoyaltyProgramId == otherProgramId);
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_FilterBySelfRedemption()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, AllowSelfRedemption = true, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, AllowSelfRedemption = false, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, filter: LoyaltyRewardTierQueueFilter.SelfRedemption, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().AllowSelfRedemption.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_FilterByMissingDescription()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, Description = "Has description", RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, Description = null, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 300, Description = "", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, filter: LoyaltyRewardTierQueueFilter.MissingDescription, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().NotContain(x => x.Description == "Has description");
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_FilterByDiscountRewards()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, RewardType = LoyaltyRewardType.PercentDiscount, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, RewardType = LoyaltyRewardType.AmountDiscount, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 300, RewardType = LoyaltyRewardType.FreeItem, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, filter: LoyaltyRewardTierQueueFilter.DiscountRewards, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().NotContain(x => x.RewardType == LoyaltyRewardType.FreeItem);
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_FilterByFreeItem()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, RewardType = LoyaltyRewardType.FreeItem, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, RewardType = LoyaltyRewardType.PercentDiscount, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, filter: LoyaltyRewardTierQueueFilter.FreeItem, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().RewardType.Should().Be(LoyaltyRewardType.FreeItem);
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersPage_Should_RespectPagination()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        for (var i = 1; i <= 6; i++)
            db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
            {
                LoyaltyProgramId = programId,
                PointsRequired = i * 100,
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var (items, total) = await handler.HandleAsync(programId, 2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(6);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLoyaltyRewardTiersSummary_Should_ReturnCorrectCounts()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 100, RewardType = LoyaltyRewardType.FreeItem, Description = "Free coffee", AllowSelfRedemption = true, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 200, RewardType = LoyaltyRewardType.PercentDiscount, Description = null, AllowSelfRedemption = false, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 300, RewardType = LoyaltyRewardType.AmountDiscount, Description = "5€ off", AllowSelfRedemption = true, RowVersion = [1] },
            new LoyaltyRewardTier { LoyaltyProgramId = programId, PointsRequired = 400, RewardType = LoyaltyRewardType.FreeItem, Description = "Deleted", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTiersPageHandler(db);
        var summary = await handler.GetSummaryAsync(programId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.FreeItemCount.Should().Be(1);
        summary.DiscountRewardCount.Should().Be(2);
        summary.SelfRedemptionCount.Should().Be(2);
        summary.MissingDescriptionCount.Should().Be(1);
    }

    // ─── GetLoyaltyProgramForEditHandler ──────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyProgramForEdit_Should_ReturnProgram_WhenFound()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var programId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "My Rewards",
            AccrualMode = LoyaltyAccrualMode.PerCurrencyUnit,
            PointsPerCurrencyUnit = 2,
            IsActive = true,
            RulesJson = "{}",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramForEditHandler(db);
        var result = await handler.HandleAsync(programId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(programId);
        result.BusinessId.Should().Be(businessId);
        result.Name.Should().Be("My Rewards");
        result.AccrualMode.Should().Be(LoyaltyAccrualMode.PerCurrencyUnit);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoyaltyProgramForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var handler = new GetLoyaltyProgramForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoyaltyProgramForEdit_Should_ReturnNull_WhenDeleted()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var program = new LoyaltyProgram
        {
            BusinessId = Guid.NewGuid(),
            Name = "Ghost Program",
            IsDeleted = true,
            RowVersion = [1]
        };
        db.Set<LoyaltyProgram>().Add(program);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramForEditHandler(db);
        var result = await handler.HandleAsync(program.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── GetLoyaltyRewardTierForEditHandler ───────────────────────────────────

    [Fact]
    public async Task GetLoyaltyRewardTierForEdit_Should_ReturnTier_WhenFound()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 500,
            RewardType = LoyaltyRewardType.FreeItem,
            Description = "Free Espresso",
            AllowSelfRedemption = true,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTierForEditHandler(db);
        var result = await handler.HandleAsync(tierId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tierId);
        result.LoyaltyProgramId.Should().Be(programId);
        result.PointsRequired.Should().Be(500);
        result.RewardType.Should().Be(LoyaltyRewardType.FreeItem);
        result.Description.Should().Be("Free Espresso");
        result.AllowSelfRedemption.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoyaltyRewardTierForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var handler = new GetLoyaltyRewardTierForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoyaltyRewardTierForEdit_Should_ReturnNull_WhenDeleted()
    {
        await using var db = LoyaltyRewardTierTestDbContext.Create();
        var tier = new LoyaltyRewardTier
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            IsDeleted = true,
            RowVersion = [1]
        };
        db.Set<LoyaltyRewardTier>().Add(tier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRewardTierForEditHandler(db);
        var result = await handler.HandleAsync(tier.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class LoyaltyRewardTierTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyRewardTierTestDbContext(DbContextOptions<LoyaltyRewardTierTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyRewardTierTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyRewardTierTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_reward_tier_query_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyRewardTierTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<LoyaltyRewardTier>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyProgram>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.RewardTiers);
            });
        }
    }
}
