using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers <see cref="GetBusinessEngagementForMemberHandler"/>:
/// business not found, unauthenticated user, stats/likes/favorites, recent reviews.
/// </summary>
public sealed class BusinessEngagementQueryHandlerTests
{
    [Fact]
    public async Task GetBusinessEngagementForMember_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_Fail_WhenBusinessNotFound()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessNotFound");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_Fail_WhenBusinessIsActiveButNotApproved()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        business.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        business.IsActive = true;
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessNotFound");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_Fail_WhenUserNotAuthenticated()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(Guid.Empty), new TestLocalizer());

        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotAuthenticated");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_ReturnStats_WhenBusinessExists()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessEngagementStats>().Add(new BusinessEngagementStats
        {
            BusinessId = business.Id,
            LikeCount = 42,
            FavoriteCount = 10,
            RatingCount = 5,
            RatingSum = 22,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.LikeCount.Should().Be(42);
        result.Value.FavoriteCount.Should().Be(10);
        result.Value.RatingCount.Should().Be(5);
        result.Value.RatingAverage.Should().BeApproximately(4.4m, 0.01m);
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_IndicateIsLikedByMe()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessLike>().Add(new BusinessLike(userId, business.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsLikedByMe.Should().BeTrue();
        result.Value.IsFavoritedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_IndicateIsFavoritedByMe()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<BusinessFavorite>().Add(new BusinessFavorite(userId, business.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.IsFavoritedByMe.Should().BeTrue();
        result.Value.IsLikedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_IncludeRecentReviews()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<User>().Add(CreateUser(reviewerId, "reviewer@test.de", "Max", "Mustermann"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<BusinessReview>().Add(new BusinessReview(reviewerId, business.Id, 5, "Excellent!"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.RecentReviews.Should().HaveCount(1);
        result.Value.RecentReviews[0].Rating.Should().Be(5);
        result.Value.RecentReviews[0].AuthorName.Should().Be("Max Mustermann");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_IncludeMyReview()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        db.Set<User>().Add(CreateUser(userId, "me@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<BusinessReview>().Add(new BusinessReview(userId, business.Id, 4, "Pretty good"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.MyReview.Should().NotBeNull();
        result.Value.MyReview!.Rating.Should().Be(4);
        result.Value.MyReview.Comment.Should().Be("Pretty good");
    }

    [Fact]
    public async Task GetBusinessEngagementForMember_Should_ReturnZeroCounts_WhenNoStats()
    {
        await using var db = BusinessEngagementQueryTestDbContext.Create();
        var userId = Guid.NewGuid();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessEngagementForMemberHandler(db, new FakeCurrentUser(userId), new TestLocalizer());
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.LikeCount.Should().Be(0);
        result.Value.FavoriteCount.Should().Be(0);
        result.Value.RatingCount.Should().Be(0);
        result.Value.RatingAverage.Should().BeNull();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Business CreateBusiness() => new Business
    {
        Name = "Test Business",
        DefaultCurrency = "EUR",
        DefaultCulture = "de-DE",
        DefaultTimeZoneId = "Europe/Berlin",
        OperationalStatus = BusinessOperationalStatus.Approved,
        IsActive = true,
        RowVersion = [1, 2, 3]
    };

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

    private sealed class BusinessEngagementQueryTestDbContext : DbContext, IAppDbContext
    {
        private BusinessEngagementQueryTestDbContext(DbContextOptions<BusinessEngagementQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessEngagementQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessEngagementQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_engagement_query_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessEngagementQueryTestDbContext(options);
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

            modelBuilder.Entity<BusinessEngagementStats>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
            });

            modelBuilder.Entity<BusinessLike>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<BusinessFavorite>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<BusinessReview>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Business);
                builder.Ignore(x => x.User);
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
