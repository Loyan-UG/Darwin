using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Darwin.WebApi.Tests.Services;

public sealed class LoyaltyPresentationServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var logger = new Mock<ILogger<LoyaltyPresentationService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        Action noHandler = () => new LoyaltyPresentationService(null!, cache, logger.Object);
        Action noCache = () => new LoyaltyPresentationService(
            new GetAvailableLoyaltyRewardsForBusinessHandler(
                CreateDbContext(),
                new StubCurrentUserService(Guid.NewGuid()),
                new TestValidationLocalizer()),
            null!,
            logger.Object);
        Action noLogger = () => new LoyaltyPresentationService(
            new GetAvailableLoyaltyRewardsForBusinessHandler(
                CreateDbContext(),
                new StubCurrentUserService(Guid.NewGuid()),
                new TestValidationLocalizer()),
            cache,
            null!);

        noHandler.Should().Throw<ArgumentNullException>().WithParameterName("availableRewardsHandler");
        noCache.Should().Throw<ArgumentNullException>().WithParameterName("cache");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetAvailableRewardsForBusinessAsync_Should_ThrowWhenDependenciesAreInvalid()
    {
        await using var db = CreateDbContext();
        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(
            db,
            new StubCurrentUserService(Guid.Empty),
            new TestValidationLocalizer());
        var service = CreateService(db, Guid.Empty, new MemoryCache(new MemoryCacheOptions()));

        var result = await service.GetAvailableRewardsForBusinessAsync(Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task GetAvailableRewardsForBusinessAsync_Should_ReturnFail_WhenBusinessIdIsEmpty()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, Guid.NewGuid(), new MemoryCache(new MemoryCacheOptions()));

        var result = await service.GetAvailableRewardsForBusinessAsync(Guid.Empty);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessId is required.");
    }

    [Fact]
    public async Task GetAvailableRewardsForBusinessAsync_Should_ReturnEmpty_WhenNoActiveProgramExists()
    {
        await using var db = CreateDbContext();
        var businessId = Guid.NewGuid();
        var service = CreateService(db, Guid.NewGuid(), new MemoryCache(new MemoryCacheOptions()));

        var result = await service.GetAvailableRewardsForBusinessAsync(businessId);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableRewardsForBusinessAsync_Should_MapRewardsAndComputeSelectionFlags()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var db = CreateDbContext();
        SeedRewardsData(db, businessId, userId, accountPoints: 100);
        await db.SaveChangesAsync();

        var service = CreateService(db, userId, new MemoryCache(new MemoryCacheOptions()));
        var result = await service.GetAvailableRewardsForBusinessAsync(businessId);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        // Ordered by PointsRequired: fallback name from program, then first desc, then second desc.
        result.Value![0].Name.Should().Be("Rewards Program");
        result.Value![0].RequiredPoints.Should().Be(30);
        result.Value![0].Description.Should().BeNull();
        result.Value![0].IsSelectable.Should().BeTrue();
        result.Value![0].RequiresConfirmation.Should().BeTrue();

        result.Value![1].Name.Should().Be("Free Drink");
        result.Value![1].IsSelectable.Should().BeTrue();

        result.Value![2].Name.Should().Be("Flat 10%");
        result.Value![2].IsSelectable.Should().BeFalse();
        result.Value![2].RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableRewardsForBusinessAsync_Should_UseCache_OnSecondCall()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var db = CreateDbContext();
        SeedRewardsData(db, businessId, userId, accountPoints: 10);
        await db.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, userId, cache);

        var first = await service.GetAvailableRewardsForBusinessAsync(businessId);
        first.Succeeded.Should().BeTrue();
        first.Value.Should().HaveCount(3);

        db.RemoveRange(await db.Set<LoyaltyRewardTier>().ToListAsync());
        db.RemoveRange(await db.Set<LoyaltyProgram>().ToListAsync());
        db.RemoveRange(await db.Set<LoyaltyAccount>().ToListAsync());
        await db.SaveChangesAsync();

        var second = await service.GetAvailableRewardsForBusinessAsync(businessId);
        second.Succeeded.Should().BeTrue();
        second.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task EnrichSelectedRewardsAsync_Should_ReturnEmpty_WhenSelectedTiersNotProvided()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, Guid.NewGuid(), new MemoryCache(new MemoryCacheOptions()));

        var result = await service.EnrichSelectedRewardsAsync(Guid.NewGuid(), null, failIfMissing: true);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task EnrichSelectedRewardsAsync_Should_ReturnDistinctOrderedRewards_WhenAllSelectedExist()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var rewardIds = SeedRewardsData(db, businessId, userId, accountPoints: 100);
        await db.SaveChangesAsync();

        var service = CreateService(db, userId, new MemoryCache(new MemoryCacheOptions()));
        var selectedIds = new[] { rewardIds.tier2, rewardIds.tier1, Guid.Empty, rewardIds.tier2, rewardIds.tier3 };

        var result = await service.EnrichSelectedRewardsAsync(
            businessId,
            selectedIds,
            failIfMissing: false);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value!.Select(x => x.LoyaltyRewardTierId).Should().Equal(rewardIds.tier2, rewardIds.tier1, rewardIds.tier3);
        result.Value.Select(x => x.Name).Should().Equal("Free Drink", "Rewards Program", "Flat 10%");
    }

    [Fact]
    public async Task EnrichSelectedRewardsAsync_Should_Fail_WhenMissingRewardsAndFailIfMissingIsTrue()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var rewardIds = SeedRewardsData(db, businessId, userId, accountPoints: 100);
        await db.SaveChangesAsync();

        var service = CreateService(db, userId, new MemoryCache(new MemoryCacheOptions()));
        var selectedIds = new[] { rewardIds.tier1, Guid.NewGuid(), rewardIds.tier2 };

        var result = await service.EnrichSelectedRewardsAsync(
            businessId,
            selectedIds,
            failIfMissing: true);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Some selected rewards are not available for this business.");
    }

    [Fact]
    public async Task EnrichSelectedRewardsAsync_Should_ReturnPartial_WhenMissingRewardsAndFailIfMissingIsFalse()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var rewardIds = SeedRewardsData(db, businessId, userId, accountPoints: 100);
        await db.SaveChangesAsync();

        var service = CreateService(db, userId, new MemoryCache(new MemoryCacheOptions()));
        var selectedIds = new[] { rewardIds.tier1, Guid.NewGuid(), rewardIds.tier2 };

        var result = await service.EnrichSelectedRewardsAsync(
            businessId,
            selectedIds,
            failIfMissing: false);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(x => x.LoyaltyRewardTierId).Should().Equal(rewardIds.tier1, rewardIds.tier2);
    }

    [Fact]
    public async Task EnrichSelectedRewardsAsync_Should_ReturnEmpty_WhenRewardsLoadFailsAndFailIfMissingIsFalse()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, Guid.Empty, new MemoryCache(new MemoryCacheOptions()));

        var result = await service.EnrichSelectedRewardsAsync(
            Guid.NewGuid(),
            new[] { Guid.NewGuid() },
            failIfMissing: false);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static LoyaltyPresentationService CreateService(
        LoyaltyPresentationServiceTestDbContext db,
        Guid currentUserId,
        IMemoryCache cache)
    {
        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(
            db,
            new StubCurrentUserService(currentUserId),
            new TestValidationLocalizer());

        return new LoyaltyPresentationService(
            handler,
            cache,
            new Mock<ILogger<LoyaltyPresentationService>>().Object);
    }

    private static LoyaltyPresentationServiceTestDbContext CreateDbContext()
    {
        return LoyaltyPresentationServiceTestDbContext.Create();
    }

    private static (Guid tier1, Guid tier2, Guid tier3) SeedRewardsData(
        LoyaltyPresentationServiceTestDbContext db,
        Guid businessId,
        Guid userId,
        int accountPoints)
    {
        var programId = Guid.NewGuid();
        var reward1 = Guid.NewGuid();
        var reward2 = Guid.NewGuid();
        var reward3 = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "Rewards Program",
            IsActive = true
        });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = accountPoints
        });
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier
            {
                Id = reward2,
                LoyaltyProgramId = programId,
                PointsRequired = 50,
                Description = "Free Drink",
                AllowSelfRedemption = false
            },
            new LoyaltyRewardTier
            {
                Id = reward3,
                LoyaltyProgramId = programId,
                PointsRequired = 150,
                Description = "Flat 10%",
                AllowSelfRedemption = false
            },
            new LoyaltyRewardTier
            {
                Id = reward1,
                LoyaltyProgramId = programId,
                PointsRequired = 30,
                Description = null,
                AllowSelfRedemption = true
            });

        return (reward1, reward2, reward3);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId) => _userId = userId;

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class TestValidationLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyPresentationServiceTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyPresentationServiceTestDbContext(DbContextOptions<LoyaltyPresentationServiceTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyPresentationServiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyPresentationServiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_presentation_service_tests_{Guid.NewGuid()}")
                .Options;

            return new LoyaltyPresentationServiceTestDbContext(options);
        }
    }
}
