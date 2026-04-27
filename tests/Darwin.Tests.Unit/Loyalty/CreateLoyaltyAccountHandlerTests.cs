using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Commands;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for <see cref="CreateLoyaltyAccountHandler"/>.
/// Covers self-service join (create if not existing), idempotent return of existing account,
/// business not found, and unauthenticated user scenarios.
/// </summary>
public sealed class CreateLoyaltyAccountHandlerTests
{
    [Fact]
    public async Task CreateLoyaltyAccount_Should_CreateNewAccount_WhenNoneExists()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Café Aurora" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(userId), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(businessId, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BusinessId.Should().Be(businessId);
        result.Value.PointsBalance.Should().Be(0);
        result.Value.Status.Should().Be(LoyaltyAccountStatus.Active);

        var persisted = await db.Set<LoyaltyAccount>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.UserId.Should().Be(userId);
        persisted.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task CreateLoyaltyAccount_Should_ReturnExistingAccount_WhenAlreadyExists()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Café Aurora" });
        var existing = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            PointsBalance = 250,
            LifetimePoints = 300,
            Status = LoyaltyAccountStatus.Active
        };
        db.Set<LoyaltyAccount>().Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(userId), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(businessId, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        result.Value.PointsBalance.Should().Be(250, "existing balance should be returned");

        var count = await db.Set<LoyaltyAccount>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1, "no duplicate account should be created");
    }

    [Fact]
    public async Task CreateLoyaltyAccount_Should_Fail_WhenBusinessNotFound()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var userId = Guid.NewGuid();

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(userId), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessNotFound");
    }

    [Fact]
    public async Task CreateLoyaltyAccount_Should_Fail_WhenUserNotAuthenticated()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Business" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(Guid.Empty), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(businessId, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    [Fact]
    public async Task CreateLoyaltyAccount_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var userId = Guid.NewGuid();

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(userId), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task CreateLoyaltyAccount_Should_IncludeBusinessName_InNewAccountDto()
    {
        await using var db = CreateLoyaltyAccountSelfServiceTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Sunset Bakery" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountHandler(db, new FakeCurrentUser(userId), new StubClock(), new TestLocalizer());

        var result = await handler.HandleAsync(businessId, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.BusinessName.Should().Be("Sunset Bakery");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        public FakeCurrentUser(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class CreateLoyaltyAccountSelfServiceTestDbContext : DbContext, IAppDbContext
    {
        private CreateLoyaltyAccountSelfServiceTestDbContext(DbContextOptions<CreateLoyaltyAccountSelfServiceTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CreateLoyaltyAccountSelfServiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CreateLoyaltyAccountSelfServiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_create_loyalty_account_self_{Guid.NewGuid()}")
                .Options;
            return new CreateLoyaltyAccountSelfServiceTestDbContext(options);
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
        }
    }
}
