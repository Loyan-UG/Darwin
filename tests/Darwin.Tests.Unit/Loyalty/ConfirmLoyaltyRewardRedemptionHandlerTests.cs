using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for <see cref="ConfirmLoyaltyRewardRedemptionHandler"/>.
/// </summary>
public sealed class ConfirmLoyaltyRewardRedemptionHandlerTests
{
    [Fact]
    public async Task ConfirmRedemption_Should_DeductPoints_AndSetConfirmedStatus()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var (accountId, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 500, pointsSpent: 100);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(400);
        result.Value.LoyaltyAccountId.Should().Be(accountId);
        result.Value.RedemptionId.Should().Be(redemptionId);

        var account = await db.Set<LoyaltyAccount>().SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);
        account.PointsBalance.Should().Be(400);

        var redemption = await db.Set<LoyaltyRewardRedemption>().SingleAsync(x => x.Id == redemptionId, TestContext.Current.CancellationToken);
        redemption.Status.Should().Be(LoyaltyRedemptionStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmRedemption_Should_CreateLedgerTransaction()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var (accountId, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 300, pointsSpent: 75);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var transaction = await db.Set<LoyaltyPointsTransaction>().SingleAsync(TestContext.Current.CancellationToken);
        transaction.LoyaltyAccountId.Should().Be(accountId);
        transaction.BusinessId.Should().Be(businessId);
        transaction.Type.Should().Be(LoyaltyPointsTransactionType.Redemption);
        transaction.PointsDelta.Should().Be(-75);
        transaction.RewardRedemptionId.Should().Be(redemptionId);
        transaction.Id.Should().Be(result.Value!.TransactionId);
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenRedemptionNotFound()
    {
        await using var db = RedemptionTestDbContext.Create();
        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RedemptionNotFound");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenBusinessMismatch()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var (_, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 200, pointsSpent: 50);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = Guid.NewGuid() // different business
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessMismatchForRedemption");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenAlreadyConfirmed()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 100
        });

        var redemption = new LoyaltyRewardRedemption
        {
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            LoyaltyRewardTierId = Guid.NewGuid(),
            PointsSpent = 50,
            Status = LoyaltyRedemptionStatus.Confirmed
        };
        db.Set<LoyaltyRewardRedemption>().Add(redemption);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemption.Id,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RedemptionAlreadyConfirmed");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenCancelled()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 100
        });

        var redemption = new LoyaltyRewardRedemption
        {
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            LoyaltyRewardTierId = Guid.NewGuid(),
            PointsSpent = 50,
            Status = LoyaltyRedemptionStatus.Cancelled
        };
        db.Set<LoyaltyRewardRedemption>().Add(redemption);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemption.Id,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RedemptionCancelledCannotConfirm");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenAccountIsInactive()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended,
            PointsBalance = 500
        });

        var redemption = new LoyaltyRewardRedemption
        {
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            LoyaltyRewardTierId = Guid.NewGuid(),
            PointsSpent = 100,
            Status = LoyaltyRedemptionStatus.Pending
        };
        db.Set<LoyaltyRewardRedemption>().Add(redemption);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemption.Id,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountInactiveForRedemption");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenInsufficientPoints()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 30 // less than pointsSpent
        });

        var redemption = new LoyaltyRewardRedemption
        {
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            LoyaltyRewardTierId = Guid.NewGuid(),
            PointsSpent = 100,
            Status = LoyaltyRedemptionStatus.Pending
        };
        db.Set<LoyaltyRewardRedemption>().Add(redemption);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemption.Id,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InsufficientPointsForRedemptionConfirmation");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var (_, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 200, pointsSpent: 50);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = businessId,
            RowVersion = [99, 88, 77] // stale
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RedemptionConcurrencyConflict");
    }

    [Fact]
    public async Task ConfirmRedemption_Should_OverrideLocationWhenProvided()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var (_, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 200, pointsSpent: 50);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = businessId,
            BusinessLocationId = locationId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.BusinessLocationId.Should().Be(locationId);

        var redemption = await db.Set<LoyaltyRewardRedemption>().SingleAsync(x => x.Id == redemptionId, TestContext.Current.CancellationToken);
        redemption.BusinessLocationId.Should().Be(locationId);
    }

    [Fact]
    public async Task ConfirmRedemption_Should_NotDecreaseLifetimePoints()
    {
        await using var db = RedemptionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var (accountId, redemptionId) = await SeedPendingRedemption(db, businessId, pointsBalance: 500, pointsSpent: 200);

        // Set a specific lifetime points value
        var account = await db.Set<LoyaltyAccount>().SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);
        account.LifetimePoints = 1000;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmLoyaltyRewardRedemptionHandler(db, new TestLocalizer());

        await handler.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = redemptionId,
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<LoyaltyAccount>().AsNoTracking().SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);
        updated.LifetimePoints.Should().Be(1000, "lifetime points should not be decremented on redemption");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<(Guid AccountId, Guid RedemptionId)> SeedPendingRedemption(
        RedemptionTestDbContext db,
        Guid businessId,
        int pointsBalance,
        int pointsSpent)
    {
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = pointsBalance,
            LifetimePoints = pointsBalance
        });

        var redemption = new LoyaltyRewardRedemption
        {
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            LoyaltyRewardTierId = Guid.NewGuid(),
            PointsSpent = pointsSpent,
            Status = LoyaltyRedemptionStatus.Pending
        };
        db.Set<LoyaltyRewardRedemption>().Add(redemption);

        await db.SaveChangesAsync(default);

        return (accountId, redemption.Id);
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class RedemptionTestDbContext : DbContext, IAppDbContext
    {
        private RedemptionTestDbContext(DbContextOptions<RedemptionTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RedemptionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RedemptionTestDbContext>()
                .UseInMemoryDatabase($"darwin_redemption_confirm_tests_{Guid.NewGuid()}")
                .Options;
            return new RedemptionTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyRewardRedemption>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.LoyaltyRewardTierId).IsRequired();
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
