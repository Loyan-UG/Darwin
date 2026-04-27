using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
/// Unit tests for <see cref="AdjustLoyaltyPointsHandler"/>.
/// </summary>
public sealed class AdjustLoyaltyPointsHandlerTests
{
    private static readonly DateTime FixedNow = new(2030, 8, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Adjust_Should_Fail_WhenAccountNotFound()
    {
        await using var db = AdjustTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 100
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task Adjust_Should_Fail_WhenBusinessMismatch()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 200, LoyaltyAccountStatus.Active);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = Guid.NewGuid(), // different business
            PointsDelta = 50
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessMismatchForLoyaltyAccount");
    }

    [Fact]
    public async Task Adjust_Should_Fail_WhenAccountIsNotActive()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 100, LoyaltyAccountStatus.Suspended);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = 50
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountMustBeActiveForAdjustment");
    }

    [Fact]
    public async Task Adjust_Should_Fail_WhenNegativeAdjustmentWouldResultInNegativeBalance()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 50, LoyaltyAccountStatus.Active);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = -100, // would make balance -50
            Reason = "Correction"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAdjustmentWouldResultInNegativeBalance");
    }

    [Fact]
    public async Task Adjust_Should_Throw_WhenPointsDeltaIsZero()
    {
        await using var db = AdjustTestDbContext.Create();
        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 0
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Adjust_Should_Fail_WhenConcurrencyConflict()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 100, LoyaltyAccountStatus.Active, rowVersion: [1, 2, 3]);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = 50,
            RowVersion = [9, 9, 9] // wrong version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictLoyaltyAccountModified");
    }

    [Fact]
    public async Task Adjust_Should_IncreaseBalance_AndCreateTransaction_WhenPositiveDelta()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 100, LoyaltyAccountStatus.Active);
        account.LifetimePoints = 500;
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = 75,
            Reason = "Bonus award"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(175);
        result.Value.NewLifetimePoints.Should().Be(575);

        var persisted = await db.Set<LoyaltyAccount>().AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);
        persisted.PointsBalance.Should().Be(175);
        persisted.LifetimePoints.Should().Be(575);
        persisted.LastAccrualAtUtc.Should().Be(FixedNow);

        var transaction = await db.Set<LoyaltyPointsTransaction>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        transaction.LoyaltyAccountId.Should().Be(account.Id);
        transaction.BusinessId.Should().Be(businessId);
        transaction.Type.Should().Be(LoyaltyPointsTransactionType.Adjustment);
        transaction.PointsDelta.Should().Be(75);
        transaction.Notes.Should().Be("Bonus award");
    }

    [Fact]
    public async Task Adjust_Should_DecreaseBalance_WhenNegativeDelta()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 200, LoyaltyAccountStatus.Active);
        account.LifetimePoints = 1000;
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = -50,
            Reason = "Correction for billing error"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(150);
        result.Value.NewLifetimePoints.Should().Be(950);

        var persisted = await db.Set<LoyaltyAccount>().AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);
        persisted.PointsBalance.Should().Be(150);
        persisted.LifetimePoints.Should().Be(950);

        var transaction = await db.Set<LoyaltyPointsTransaction>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        transaction.PointsDelta.Should().Be(-50);
        transaction.Type.Should().Be(LoyaltyPointsTransactionType.Adjustment);
    }

    [Fact]
    public async Task Adjust_Should_Succeed_WhenNegativeAdjustmentExactlyZeroesBalance()
    {
        await using var db = AdjustTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var account = CreateAccount(businessId, 100, LoyaltyAccountStatus.Active);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            PointsDelta = -100,
            Reason = "Full points reversal"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(0);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private AdjustLoyaltyPointsHandler CreateHandler(IAppDbContext db)
        => new(db, new FakeClock(FixedNow), new AdjustLoyaltyPointsValidator(new TestLocalizer()), new TestLocalizer());

    private static LoyaltyAccount CreateAccount(Guid businessId, int balance, LoyaltyAccountStatus status, byte[]? rowVersion = null) =>
        new()
        {
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            PointsBalance = balance,
            LifetimePoints = balance,
            Status = status,
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class AdjustTestDbContext : DbContext, IAppDbContext
    {
        private AdjustTestDbContext(DbContextOptions<AdjustTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AdjustTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AdjustTestDbContext>()
                .UseInMemoryDatabase($"darwin_adjust_loyalty_points_tests_{Guid.NewGuid()}")
                .Options;
            return new AdjustTestDbContext(options);
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

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }
    }
}
