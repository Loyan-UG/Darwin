using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for business engagement handlers:
/// <see cref="ToggleBusinessLikeHandler"/> and <see cref="UpsertBusinessReviewHandler"/>.
/// </summary>
public sealed class BusinessEngagementHandlersTests
{
    // ─── ToggleBusinessLikeHandler ────────────────────────────────────────────

    [Fact]
    public async Task ToggleBusinessLike_Should_AddLike_WhenNotExisting()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();
        var handler = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);

        var count = await db.Set<BusinessLike>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ToggleBusinessLike_Should_RemoveLike_WhenAlreadyLiked()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var stats = new BusinessEngagementStats { BusinessId = business.Id };
        stats.SetSnapshot(0, 0, 1, 0, DateTime.UtcNow);
        db.Set<BusinessEngagementStats>().Add(stats);
        db.Set<BusinessLike>().Add(new BusinessLike(userId, business.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.TotalCount.Should().Be(0);

        var count = await db.Set<BusinessLike>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(0);
    }

    [Fact]
    public async Task ToggleBusinessLike_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(Guid.NewGuid()), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task ToggleBusinessLike_Should_Fail_WhenUserNotAuthenticated()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(Guid.Empty), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    [Fact]
    public async Task ToggleBusinessLike_Should_TrackLikeCountCorrectly_AcrossMultipleUsers()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Two different users like the business
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var handler1 = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(user1), new TestLocalizer());
        var handler2 = new ToggleBusinessLikeHandler(db, new FakeCurrentUser(user2), new TestLocalizer());

        var result1 = await handler1.HandleAsync(business.Id, TestContext.Current.CancellationToken);
        var result2 = await handler2.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result1.Succeeded.Should().BeTrue();
        result2.Succeeded.Should().BeTrue();
        result2.Value!.TotalCount.Should().Be(2);
    }

    // ─── UpsertBusinessReviewHandler ──────────────────────────────────────────

    [Fact]
    public async Task UpsertBusinessReview_Should_CreateNewReview_WhenNoneExists()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();
        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, new UpsertBusinessReviewDto
        {
            Rating = 4,
            Comment = "Great service!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var review = await db.Set<BusinessReview>().SingleAsync(TestContext.Current.CancellationToken);
        review.BusinessId.Should().Be(business.Id);
        review.UserId.Should().Be(userId);
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Great service!");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_UpdateExistingReview_WhenAlreadyExists()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        db.Set<BusinessReview>().Add(new BusinessReview(userId, business.Id, 3, "Average"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, new UpsertBusinessReviewDto
        {
            Rating = 5,
            Comment = "Changed my mind, excellent!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var reviews = await db.Set<BusinessReview>().ToListAsync(TestContext.Current.CancellationToken);
        reviews.Should().HaveCount(1, "upsert should not create a duplicate review");
        reviews[0].Rating.Should().Be(5);
        reviews[0].Comment.Should().Be("Changed my mind, excellent!");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(Guid.NewGuid()), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, new UpsertBusinessReviewDto { Rating = 3 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_Fail_WhenRatingIsBelowOne()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(Guid.NewGuid()), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpsertBusinessReviewDto { Rating = 0 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RatingMustBeBetweenOneAndFive");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_Fail_WhenRatingExceedsFive()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(Guid.NewGuid()), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpsertBusinessReviewDto { Rating = 6 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RatingMustBeBetweenOneAndFive");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_Fail_WhenDtoIsNull()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(Guid.NewGuid()), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RequestPayloadRequired");
    }

    [Fact]
    public async Task UpsertBusinessReview_Should_UpdateEngagementStats()
    {
        await using var db = BusinessEngagementTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats { BusinessId = business.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpsertBusinessReviewHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        await handler.HandleAsync(business.Id, new UpsertBusinessReviewDto { Rating = 5 }, TestContext.Current.CancellationToken);

        var stats = await db.Set<BusinessEngagementStats>()
            .SingleAsync(x => x.BusinessId == business.Id, TestContext.Current.CancellationToken);
        stats.RatingCount.Should().Be(1);
        stats.RatingSum.Should().Be(5);
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
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class BusinessEngagementTestDbContext : DbContext, IAppDbContext
    {
        private BusinessEngagementTestDbContext(DbContextOptions<BusinessEngagementTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessEngagementTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessEngagementTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_engagement_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessEngagementTestDbContext(options);
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

            modelBuilder.Entity<BusinessLike>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
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

            modelBuilder.Entity<BusinessReview>(builder =>
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
        }
    }
}
