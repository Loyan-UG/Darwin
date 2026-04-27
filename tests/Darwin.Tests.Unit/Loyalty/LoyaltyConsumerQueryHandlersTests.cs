using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers consumer-facing Loyalty query handlers:
/// <see cref="GetMyLoyaltyAccountsHandler"/>, <see cref="GetMyLoyaltyHistoryHandler"/>,
/// and <see cref="GetLoyaltyAccountTransactionsHandler"/>.
/// </summary>
public sealed class LoyaltyConsumerQueryHandlersTests
{
    // ─── GetMyLoyaltyAccountsHandler ─────────────────────────────────────────

    [Fact]
    public async Task GetMyLoyaltyAccounts_Should_ReturnOnlyCurrentUsersAccounts()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var businessId1 = Guid.NewGuid();
        var businessId2 = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = businessId1, Name = "Café Aurora", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", IsActive = true, RowVersion = [1] },
            new Business { Id = businessId2, Name = "Backwerk", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId1, UserId = userId, Status = LoyaltyAccountStatus.Active, PointsBalance = 150, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId2, UserId = userId, Status = LoyaltyAccountStatus.Active, PointsBalance = 80, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId1, UserId = otherUserId, Status = LoyaltyAccountStatus.Active, PointsBalance = 300, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountsHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Should().OnlyContain(a => a.BusinessId == businessId1 || a.BusinessId == businessId2);
    }

    [Fact]
    public async Task GetMyLoyaltyAccounts_Should_ReturnEmptyList_WhenUserHasNoAccounts()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();

        var handler = new GetMyLoyaltyAccountsHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLoyaltyAccounts_Should_ExcludeDeletedAccounts()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Active Biz", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountsHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyLoyaltyAccounts_Should_ExcludeAccountsForDeletedBusinesses()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();
        var activeBizId = Guid.NewGuid();
        var deletedBizId = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = activeBizId, Name = "Active", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", IsActive = true, RowVersion = [1] },
            new Business { Id = deletedBizId, Name = "Deleted Biz", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", IsActive = true, IsDeleted = true, RowVersion = [1] });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = activeBizId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = deletedBizId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountsHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.Single().BusinessName.Should().Be("Active");
    }

    // ─── GetMyLoyaltyHistoryHandler ───────────────────────────────────────────

    [Fact]
    public async Task GetMyLoyaltyHistory_Should_ReturnTransactions_ForCurrentUserAndBusiness()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });
        db.Set<LoyaltyPointsTransaction>().AddRange(
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 50,
                Reference = "ORD-001",
                CreatedAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 30,
                Reference = "ORD-002",
                CreatedAtUtc = new DateTime(2030, 1, 3, 9, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyHistoryHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        // Ordered newest first
        result.Value![0].Reference.Should().Be("ORD-002");
        result.Value[1].Reference.Should().Be("ORD-001");
    }

    [Fact]
    public async Task GetMyLoyaltyHistory_Should_Fail_WhenNoAccountExistsForBusiness()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();

        var handler = new GetMyLoyaltyHistoryHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMyLoyaltyHistory_Should_ExcludeDeletedTransactions()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });
        db.Set<LoyaltyPointsTransaction>().AddRange(
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 50,
                RowVersion = [1]
            },
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 20,
                IsDeleted = true,
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyHistoryHandler(db, new StubCurrentUser(userId));
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].PointsDelta.Should().Be(50);
    }

    // ─── GetLoyaltyAccountTransactionsHandler ─────────────────────────────────

    [Fact]
    public async Task GetLoyaltyAccountTransactions_Should_ReturnTransactions_ForAccount()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });
        db.Set<LoyaltyPointsTransaction>().AddRange(
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 100,
                CreatedAtUtc = new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 50,
                CreatedAtUtc = new DateTime(2030, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountTransactionsHandler(db, new TestLocalizer());
        var items = await handler.HandleAsync(accountId, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(2);
        // Newest first
        items[0].PointsDelta.Should().Be(50);
        items[1].PointsDelta.Should().Be(100);
    }

    [Fact]
    public async Task GetLoyaltyAccountTransactions_Should_RespectMaxCount()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });

        for (var i = 0; i < 10; i++)
        {
            db.Set<LoyaltyPointsTransaction>().Add(new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = i + 1,
                CreatedAtUtc = new DateTime(2030, 1, i + 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountTransactionsHandler(db, new TestLocalizer());
        var items = await handler.HandleAsync(accountId, maxCount: 3, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLoyaltyAccountTransactions_Should_Throw_WhenAccountIdIsEmpty()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var handler = new GetLoyaltyAccountTransactionsHandler(db, new TestLocalizer());

        var act = async () => await handler.HandleAsync(Guid.Empty, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLoyaltyAccountTransactions_Should_UseDefaultMaxCount_WhenZeroOrNegativeMaxCountProvided()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });

        for (var i = 0; i < 5; i++)
        {
            db.Set<LoyaltyPointsTransaction>().Add(new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = i + 1,
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountTransactionsHandler(db, new TestLocalizer());
        // maxCount <= 0 should fall back to the default (50) and return all 5 rows
        var items = await handler.HandleAsync(accountId, maxCount: 0, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetLoyaltyAccountTransactions_Should_ReturnEmpty_WhenNoTransactionsExist()
    {
        await using var db = LoyaltyConsumerTestDbContext.Create();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountTransactionsHandler(db, new TestLocalizer());
        var items = await handler.HandleAsync(accountId, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        public StubCurrentUser(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
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

    private sealed class LoyaltyConsumerTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyConsumerTestDbContext(DbContextOptions<LoyaltyConsumerTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyConsumerTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyConsumerTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_consumer_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyConsumerTestDbContext(options);
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
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
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
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
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

            modelBuilder.Entity<ScanSession>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.Outcome).IsRequired();
            });
        }
    }
}
