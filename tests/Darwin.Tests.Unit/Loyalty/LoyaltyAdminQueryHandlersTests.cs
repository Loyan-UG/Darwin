using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
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
/// Covers admin-facing Loyalty query and command handlers:
/// <see cref="GetLoyaltyProgramsPageHandler"/> (page + summary),
/// <see cref="GetLoyaltyAccountsPageHandler"/> (page + summary),
/// and <see cref="SoftDeleteLoyaltyProgramHandler"/>.
/// </summary>
public sealed class LoyaltyAdminQueryHandlersTests
{
    // ─── GetLoyaltyProgramsPageHandler ────────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyProgramsPage_Should_ReturnAllNonDeleted()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().AddRange(
            new LoyaltyProgram { BusinessId = businessId, Name = "Program A", IsActive = true, RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Program B", IsActive = false, RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Deleted", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramsPageHandler(db);
        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Name == "Deleted");
    }

    [Fact]
    public async Task GetLoyaltyProgramsPage_Should_FilterByBusinessId()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var targetBiz = Guid.NewGuid();
        var otherBiz = Guid.NewGuid();
        db.Set<LoyaltyProgram>().AddRange(
            new LoyaltyProgram { BusinessId = targetBiz, Name = "Target Program", IsActive = true, RowVersion = [1] },
            new LoyaltyProgram { BusinessId = otherBiz, Name = "Other Program", IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId: targetBiz, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Target Program");
    }

    [Fact]
    public async Task GetLoyaltyProgramsPage_Should_FilterByActiveFilter()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().AddRange(
            new LoyaltyProgram { BusinessId = businessId, Name = "Active", IsActive = true, RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Inactive", IsActive = false, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            filter: LoyaltyProgramQueueFilter.Active,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetLoyaltyProgramsPage_Should_RespectPagination()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        for (var i = 1; i <= 5; i++)
            db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
            {
                BusinessId = businessId,
                Name = $"Program {i:D2}",
                IsActive = true,
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramsPageHandler(db);
        var (items, total) = await handler.HandleAsync(2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLoyaltyProgramsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().AddRange(
            new LoyaltyProgram { BusinessId = businessId, Name = "Active PerVisit", IsActive = true, AccrualMode = LoyaltyAccrualMode.PerVisit, RulesJson = "{}", RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Active PerCurrencyUnit", IsActive = true, AccrualMode = LoyaltyAccrualMode.PerCurrencyUnit, RulesJson = "{}", RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Inactive MissingRules", IsActive = false, RulesJson = null, RowVersion = [1] },
            new LoyaltyProgram { BusinessId = businessId, Name = "Deleted", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyProgramsPageHandler(db);
        var summary = await handler.GetSummaryAsync(ct: TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.ActiveCount.Should().Be(2);
        summary.InactiveCount.Should().Be(1);
        summary.PerCurrencyUnitCount.Should().Be(1);
        summary.MissingRulesCount.Should().Be(1);
    }

    // ─── GetLoyaltyAccountsPageHandler ───────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyAccountsPage_Should_ReturnAccountsForBusiness()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBiz = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Set<User>().AddRange(
            CreateUser(userId1, "alice@test.de"),
            CreateUser(userId2, "bob@test.de"));
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId, UserId = userId1, Status = LoyaltyAccountStatus.Active, PointsBalance = 100, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = userId2, Status = LoyaltyAccountStatus.Active, PointsBalance = 200, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = otherBiz, UserId = userId1, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(a => a.BusinessId == businessId);
    }

    [Fact]
    public async Task GetLoyaltyAccountsPage_Should_FilterByStatus()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Set<User>().AddRange(
            CreateUser(userId1, "active@test.de"),
            CreateUser(userId2, "suspended@test.de"));
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId, UserId = userId1, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = userId2, Status = LoyaltyAccountStatus.Suspended, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, status: LoyaltyAccountStatus.Suspended, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().UserEmail.Should().Be("suspended@test.de");
    }

    [Fact]
    public async Task GetLoyaltyAccountsPage_Should_ExcludeDeletedAccounts()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Set<User>().AddRange(
            CreateUser(userId1, "visible@test.de"),
            CreateUser(userId2, "deleted@test.de"));
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId, UserId = userId1, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = userId2, Status = LoyaltyAccountStatus.Active, IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().UserEmail.Should().Be("visible@test.de");
    }

    [Fact]
    public async Task GetLoyaltyAccountsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var recentAccrual = DateTime.UtcNow.AddDays(-5);
        var oldAccrual = DateTime.UtcNow.AddDays(-60);

        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            db.Set<User>().Add(CreateUser(userId, $"user{i}@test.de"));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var users = db.Set<User>().ToList();
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { BusinessId = businessId, UserId = users[0].Id, Status = LoyaltyAccountStatus.Active, PointsBalance = 100, LastAccrualAtUtc = recentAccrual, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = users[1].Id, Status = LoyaltyAccountStatus.Suspended, PointsBalance = 0, LastAccrualAtUtc = oldAccrual, RowVersion = [1] },
            new LoyaltyAccount { BusinessId = businessId, UserId = users[2].Id, Status = LoyaltyAccountStatus.Active, PointsBalance = 0, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.ActiveCount.Should().Be(2);
        summary.SuspendedCount.Should().Be(1);
        summary.ZeroBalanceCount.Should().Be(2);
        summary.RecentAccrualCount.Should().Be(1);
    }

    // ─── SoftDeleteLoyaltyProgramHandler ─────────────────────────────────────

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_MarkAsDeleted()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var program = new LoyaltyProgram
        {
            BusinessId = Guid.NewGuid(),
            Name = "To Delete",
            IsActive = false,
            RowVersion = [1, 2, 3]
        };
        db.Set<LoyaltyProgram>().Add(program);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = program.Id,
            RowVersion = program.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<LoyaltyProgram>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == program.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_BeIdempotent_WhenAlreadyDeleted()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var program = new LoyaltyProgram
        {
            BusinessId = Guid.NewGuid(),
            Name = "Already Deleted",
            IsDeleted = true,
            RowVersion = [1, 2, 3]
        };
        db.Set<LoyaltyProgram>().Add(program);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = program.Id,
            RowVersion = program.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Fail_WhenProgramNotFound()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyProgramNotFound");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Fail_OnConcurrencyConflict()
    {
        await using var db = LoyaltyAdminTestDbContext.Create();
        var program = new LoyaltyProgram
        {
            BusinessId = Guid.NewGuid(),
            Name = "Concurrency Program",
            IsActive = false,
            RowVersion = [1, 2, 3]
        };
        db.Set<LoyaltyProgram>().Add(program);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = program.Id,
            RowVersion = [99, 88, 77] // stale
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyProgramConcurrencyConflict");
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

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyAdminTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyAdminTestDbContext(DbContextOptions<LoyaltyAdminTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyAdminTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyAdminTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_admin_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyAdminTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<LoyaltyProgram>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.RewardTiers);
            });

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
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
