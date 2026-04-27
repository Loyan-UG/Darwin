using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for business delete and engagement handlers:
/// <see cref="SoftDeleteBusinessHandler"/> and <see cref="ToggleBusinessFavoriteHandler"/>.
/// </summary>
public sealed class SoftDeleteBusinessAndFavoriteHandlerTests
{
    // ─── SoftDeleteBusinessHandler ────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteBusiness_Should_MarkBusinessAsDeleted()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessHandler(db, new BusinessDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessDeleteDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == business.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteBusiness_Should_BeIdempotent_WhenAlreadyDeleted()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var business = CreateBusiness();
        business.IsDeleted = true;
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessHandler(db, new BusinessDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessDeleteDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteBusiness_Should_Fail_WhenNotFound()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var handler = new SoftDeleteBusinessHandler(db, new BusinessDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessNotFound");
    }

    [Fact]
    public async Task SoftDeleteBusiness_Should_Fail_OnConcurrencyConflict()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessHandler(db, new BusinessDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessDeleteDto
        {
            Id = business.Id,
            RowVersion = [9, 8, 7] // stale version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    // ─── ToggleBusinessFavoriteHandler ────────────────────────────────────────

    [Fact]
    public async Task ToggleBusinessFavorite_Should_AddFavorite_WhenNotExisting()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new ToggleBusinessFavoriteHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);

        var count = await db.Set<BusinessFavorite>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ToggleBusinessFavorite_Should_RemoveFavorite_WhenAlreadyFavorited()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var stats = new BusinessEngagementStats { BusinessId = business.Id };
        stats.SetSnapshot(0, 0, 0, 1, DateTime.UtcNow);
        db.Set<BusinessEngagementStats>().Add(stats);

        db.Set<BusinessFavorite>().Add(new BusinessFavorite(userId, business.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var currentUser = new FakeCurrentUser(userId);
        var handler = new ToggleBusinessFavoriteHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.TotalCount.Should().Be(0);

        var count = await db.Set<BusinessFavorite>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(0);
    }

    [Fact]
    public async Task ToggleBusinessFavorite_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var currentUser = new FakeCurrentUser(Guid.NewGuid());
        var handler = new ToggleBusinessFavoriteHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task ToggleBusinessFavorite_Should_Fail_WhenUserNotAuthenticated()
    {
        await using var db = BusinessDeleteFavoriteTestDbContext.Create();
        var currentUser = new FakeCurrentUser(Guid.Empty); // unauthenticated
        var handler = new ToggleBusinessFavoriteHandler(db, currentUser, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Business CreateBusiness()
    {
        return new Business
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            OperationalStatus = BusinessOperationalStatus.Approved,
            IsActive = true,
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        public FakeCurrentUser(Guid userId) => _userId = userId;
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

    private sealed class BusinessDeleteFavoriteTestDbContext : DbContext, IAppDbContext
    {
        private BusinessDeleteFavoriteTestDbContext(DbContextOptions<BusinessDeleteFavoriteTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessDeleteFavoriteTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessDeleteFavoriteTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_delete_favorite_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessDeleteFavoriteTestDbContext(options);
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

            modelBuilder.Entity<BusinessFavorite>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<BusinessEngagementStats>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
            });

            modelBuilder.Entity<BusinessReview>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<BusinessLike>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
            });
        }
    }
}
