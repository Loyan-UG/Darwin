using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers Loyalty admin account query handlers:
/// <see cref="GetLoyaltyAccountForAdminHandler"/> and
/// <see cref="GetLoyaltyAccountByBusinessAndUserHandler"/>.
/// </summary>
public sealed class LoyaltyAccountAdminQueryHandlersTests
{
    // ─── GetLoyaltyAccountForAdminHandler ─────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyAccountForAdmin_Should_ReturnAccount_WhenFound()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "alice@test.de", "Alice", "Smith"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 350,
            LifetimePoints = 500,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountForAdminHandler(db);
        var result = await handler.HandleAsync(account.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.BusinessId.Should().Be(businessId);
        result.UserId.Should().Be(userId);
        result.UserEmail.Should().Be("alice@test.de");
        result.UserDisplayName.Should().Be("Alice Smith");
        result.PointsBalance.Should().Be(350);
        result.LifetimePoints.Should().Be(500);
        result.Status.Should().Be(LoyaltyAccountStatus.Active);
    }

    [Fact]
    public async Task GetLoyaltyAccountForAdmin_Should_UseEmailAsDisplayName_WhenNameIsEmpty()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "noname@test.de")); // no first/last name
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 0,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountForAdminHandler(db);
        var result = await handler.HandleAsync(account.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.UserDisplayName.Should().Be("noname@test.de");
    }

    [Fact]
    public async Task GetLoyaltyAccountForAdmin_Should_ReturnNull_WhenNotFound()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var handler = new GetLoyaltyAccountForAdminHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoyaltyAccountForAdmin_Should_ReturnNull_WhenAccountIsDeleted()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "deleted@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var account = new LoyaltyAccount
        {
            BusinessId = Guid.NewGuid(),
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            IsDeleted = true,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountForAdminHandler(db);
        var result = await handler.HandleAsync(account.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── GetLoyaltyAccountByBusinessAndUserHandler ────────────────────────────

    [Fact]
    public async Task GetLoyaltyAccountByBusinessAndUser_Should_ReturnAccount_WhenFound()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 120,
            LifetimePoints = 240,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountByBusinessAndUserHandler(db);
        var result = await handler.HandleAsync(businessId, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.BusinessId.Should().Be(businessId);
        result.UserId.Should().Be(userId);
        result.PointsBalance.Should().Be(120);
        result.LifetimePoints.Should().Be(240);
        result.Status.Should().Be(LoyaltyAccountStatus.Active);
    }

    [Fact]
    public async Task GetLoyaltyAccountByBusinessAndUser_Should_ReturnNull_WhenNotFound()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var handler = new GetLoyaltyAccountByBusinessAndUserHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoyaltyAccountByBusinessAndUser_Should_ReturnNull_WhenDeleted()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            IsDeleted = true,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountByBusinessAndUserHandler(db);
        var result = await handler.HandleAsync(businessId, userId, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoyaltyAccountByBusinessAndUser_Should_ReturnNull_WhenBusinessIdDoesNotMatch()
    {
        await using var db = LoyaltyAccountAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            RowVersion = [1]
        };
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLoyaltyAccountByBusinessAndUserHandler(db);
        var result = await handler.HandleAsync(Guid.NewGuid(), userId, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id, string email, string? firstName = null, string? lastName = null)
    {
        var user = new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            IsActive = true,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };
        return user;
    }

    private sealed class LoyaltyAccountAdminTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyAccountAdminTestDbContext(DbContextOptions<LoyaltyAccountAdminTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyAccountAdminTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyAccountAdminTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_account_admin_query_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyAccountAdminTestDbContext(options);
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
