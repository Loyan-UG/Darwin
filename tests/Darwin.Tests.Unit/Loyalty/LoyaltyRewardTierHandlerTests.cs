using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for loyalty reward tier command handlers:
/// <see cref="UpdateLoyaltyRewardTierHandler"/> and <see cref="SoftDeleteLoyaltyRewardTierHandler"/>.
/// </summary>
public sealed class LoyaltyRewardTierHandlerTests
{
    // ─── UpdateLoyaltyRewardTierHandler ──────────────────────────────────────

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_PersistChanges_WhenTierExists()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem,
            AllowSelfRedemption = false
        };
        db.Set<LoyaltyRewardTier>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyRewardTierHandler(db, new LoyaltyRewardTierEditValidator(), new TestLocalizer());

        await handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 250,
            RewardType = LoyaltyRewardType.PercentDiscount,
            RewardValue = 15m,
            Description = "15% discount on next purchase",
            AllowSelfRedemption = true,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<LoyaltyRewardTier>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        updated.PointsRequired.Should().Be(250);
        updated.RewardType.Should().Be(LoyaltyRewardType.PercentDiscount);
        updated.RewardValue.Should().Be(15m);
        updated.Description.Should().Be("15% discount on next purchase");
        updated.AllowSelfRedemption.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_Throw_WhenTierNotFound()
    {
        await using var db = RewardTierTestDbContext.Create();
        var handler = new UpdateLoyaltyRewardTierHandler(db, new LoyaltyRewardTierEditValidator(), new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = Guid.NewGuid(),
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("tier must exist to be updated");
    }

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_Throw_WhenTierIsSoftDeleted()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();

        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 50,
            RewardType = LoyaltyRewardType.FreeItem,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyRewardTierHandler(db, new LoyaltyRewardTierEditValidator(), new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("deleted tiers cannot be updated");
    }

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();

        var entity = new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        };
        db.Set<LoyaltyRewardTier>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyRewardTierHandler(db, new LoyaltyRewardTierEditValidator(), new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = tierId,
            LoyaltyProgramId = entity.LoyaltyProgramId,
            PointsRequired = 200,
            RewardType = LoyaltyRewardType.FreeItem,
            RowVersion = [0xDE, 0xAD] // stale version
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("concurrency conflict must block the update");
    }

    // ─── SoftDeleteLoyaltyRewardTierHandler ──────────────────────────────────

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_MarkAsDeleted_WhenTierExists()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();

        var entity = new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 200,
            RewardType = LoyaltyRewardType.FreeItem
        };
        db.Set<LoyaltyRewardTier>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new LoyaltyRewardTierDeleteDto
        {
            Id = tierId,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var deleted = await db.Set<LoyaltyRewardTier>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_BeIdempotent_WhenAlreadyDeleted()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();

        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 300,
            RewardType = LoyaltyRewardType.FreeItem,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new LoyaltyRewardTierDeleteDto
        {
            Id = tierId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("deleting an already-deleted tier should succeed idempotently");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_Fail_WhenTierNotFound()
    {
        await using var db = RewardTierTestDbContext.Create();
        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new LoyaltyRewardTierDeleteDto
        {
            Id = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyRewardTierNotFound");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = RewardTierTestDbContext.Create();
        var tierId = Guid.NewGuid();

        var entity = new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        };
        db.Set<LoyaltyRewardTier>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new LoyaltyRewardTierDeleteDto
        {
            Id = tierId,
            RowVersion = [0xBA, 0xAD] // stale row version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyRewardTierConcurrencyConflict");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class RewardTierTestDbContext : DbContext, IAppDbContext
    {
        private RewardTierTestDbContext(DbContextOptions<RewardTierTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RewardTierTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RewardTierTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_reward_tier_tests_{Guid.NewGuid()}")
                .Options;
            return new RewardTierTestDbContext(options);
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
        }
    }
}
