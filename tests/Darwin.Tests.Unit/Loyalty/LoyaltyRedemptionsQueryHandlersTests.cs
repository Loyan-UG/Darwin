using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers loyalty redemption admin query handlers:
/// <see cref="GetLoyaltyRedemptionsPageHandler"/> (paged + summary)
/// and <see cref="GetLoyaltyAccountRedemptionsHandler"/>.
/// </summary>
public sealed class LoyaltyRedemptionsQueryHandlersTests
{
    // ─── GetLoyaltyRedemptionsPageHandler ─────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyRedemptionsPage_Should_ReturnAllForBusiness()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "buyer@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account1 = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        var account2 = new LoyaltyAccount { BusinessId = otherBusinessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().AddRange(account1, account2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 200, Description = "Free Cake", RowVersion = [1] });
        db.Set<LoyaltyRewardRedemption>().AddRange(
            new LoyaltyRewardRedemption { LoyaltyAccountId = account1.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 200, Status = LoyaltyRedemptionStatus.Pending, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account2.Id, BusinessId = otherBusinessId, LoyaltyRewardTierId = tierId, PointsSpent = 200, Status = LoyaltyRedemptionStatus.Completed, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRedemptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items.Single().BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task GetLoyaltyRedemptionsPage_Should_FilterByStatus()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "status@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 100, Description = "Tier", RowVersion = [1] });
        db.Set<LoyaltyRewardRedemption>().AddRange(
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Pending, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Completed, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Cancelled, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRedemptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, status: LoyaltyRedemptionStatus.Pending, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(LoyaltyRedemptionStatus.Pending);
    }

    [Fact]
    public async Task GetLoyaltyRedemptionsPage_Should_ExcludeDeletedRedemptions()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "deleted@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 100, RowVersion = [1] });
        db.Set<LoyaltyRewardRedemption>().AddRange(
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Pending, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Pending, IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRedemptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetLoyaltyRedemptionsPage_Should_RespectPagination()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "paged@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 100, RowVersion = [1] });
        for (var i = 0; i < 5; i++)
            db.Set<LoyaltyRewardRedemption>().Add(new LoyaltyRewardRedemption
            {
                LoyaltyAccountId = account.Id,
                BusinessId = businessId,
                LoyaltyRewardTierId = tierId,
                PointsSpent = 100,
                Status = LoyaltyRedemptionStatus.Pending,
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRedemptionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLoyaltyRedemptionsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "summary@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 100, RowVersion = [1] });
        db.Set<LoyaltyRewardRedemption>().AddRange(
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Pending, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Completed, RowVersion = [1] },
            new LoyaltyRewardRedemption { LoyaltyAccountId = account.Id, BusinessId = businessId, LoyaltyRewardTierId = tierId, PointsSpent = 100, Status = LoyaltyRedemptionStatus.Cancelled, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyRedemptionsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.PendingCount.Should().Be(1);
        summary.CompletedCount.Should().Be(1);
        summary.CancelledCount.Should().Be(1);
    }

    // ─── GetLoyaltyAccountRedemptionsHandler ──────────────────────────────────

    [Fact]
    public async Task GetLoyaltyAccountRedemptions_Should_ReturnEmpty_WhenNoRedemptions()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var userId = Guid.NewGuid();
        var account = new LoyaltyAccount { BusinessId = Guid.NewGuid(), UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<User>().Add(CreateUser(userId, "empty@test.de"));
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountRedemptionsHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(account.Id, ct: TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLoyaltyAccountRedemptions_Should_ReturnRedemptions_WithRewardLabel()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "redeemer@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 300, Description = "Free Donut", RowVersion = [1] });
        db.Set<LoyaltyRewardRedemption>().Add(new LoyaltyRewardRedemption
        {
            LoyaltyAccountId = account.Id,
            BusinessId = businessId,
            LoyaltyRewardTierId = tierId,
            PointsSpent = 300,
            Status = LoyaltyRedemptionStatus.Completed,
            RedeemedAtUtc = DateTime.UtcNow.AddDays(-1),
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountRedemptionsHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(account.Id, ct: TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0].LoyaltyAccountId.Should().Be(account.Id);
        result[0].RewardLabel.Should().Be("Free Donut");
        result[0].PointsSpent.Should().Be(300);
        result[0].Status.Should().Be(LoyaltyRedemptionStatus.Completed);
    }

    [Fact]
    public async Task GetLoyaltyAccountRedemptions_Should_Throw_WhenLoyaltyAccountIdIsEmpty()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var handler = new GetLoyaltyAccountRedemptionsHandler(db, new TestLocalizer());

        var act = async () => await handler.HandleAsync(Guid.Empty, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLoyaltyAccountRedemptions_Should_RespectMaxCount()
    {
        await using var db = LoyaltyRedemptionsTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "maxcount@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = tierId, LoyaltyProgramId = Guid.NewGuid(), PointsRequired = 100, Description = "Tier", RowVersion = [1] });
        for (var i = 0; i < 10; i++)
            db.Set<LoyaltyRewardRedemption>().Add(new LoyaltyRewardRedemption
            {
                LoyaltyAccountId = account.Id,
                BusinessId = businessId,
                LoyaltyRewardTierId = tierId,
                PointsSpent = 100,
                Status = LoyaltyRedemptionStatus.Completed,
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountRedemptionsHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(account.Id, maxCount: 3, ct: TestContext.Current.CancellationToken);

        result.Should().HaveCount(3);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id, string email)
    {
        return new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            IsActive = true,
            EmailConfirmed = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyRedemptionsTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyRedemptionsTestDbContext(DbContextOptions<LoyaltyRedemptionsTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyRedemptionsTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyRedemptionsTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_redemptions_query_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyRedemptionsTestDbContext(options);
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

            modelBuilder.Entity<ScanSession>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Outcome).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.Logins);
                builder.Ignore(x => x.Tokens);
                builder.Ignore(x => x.TwoFactorSecrets);
                builder.Ignore(x => x.Devices);
                builder.Ignore(x => x.BusinessFavorites);
                builder.Ignore(x => x.BusinessLikes);
                builder.Ignore(x => x.BusinessReviews);
                builder.Ignore(x => x.EngagementSnapshot);
            });
        }
    }
}
