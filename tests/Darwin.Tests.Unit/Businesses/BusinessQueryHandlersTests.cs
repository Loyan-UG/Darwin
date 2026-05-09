using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers admin- and consumer-facing Business query handlers:
/// <see cref="GetBusinessesPageHandler"/>, <see cref="GetBusinessForEditHandler"/>,
/// <see cref="GetBusinessPublicDetailHandler"/>, and <see cref="GetBusinessMembersPageHandler"/>.
/// </summary>
public sealed class BusinessQueryHandlersTests
{
    // ─── GetBusinessesPageHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetBusinessesPage_Should_ReturnAllBusinesses_WhenNoFilterApplied()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        db.Set<Business>().AddRange(
            CreateBusiness("Café Aurora", BusinessOperationalStatus.Approved),
            CreateBusiness("Backwerk Mitte", BusinessOperationalStatus.Approved),
            CreateBusiness("Pending Shop", BusinessOperationalStatus.PendingApproval));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBusinessesPage_Should_FilterByNameQuery()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        db.Set<Business>().AddRange(
            CreateBusiness("Café Aurora", BusinessOperationalStatus.Approved),
            CreateBusiness("Backwerk Mitte", BusinessOperationalStatus.Approved));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "Aurora", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Café Aurora");
    }

    [Fact]
    public async Task GetBusinessesPage_Should_FilterByOperationalStatus()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        db.Set<Business>().AddRange(
            CreateBusiness("Approved One", BusinessOperationalStatus.Approved),
            CreateBusiness("Pending One", BusinessOperationalStatus.PendingApproval),
            CreateBusiness("Approved Two", BusinessOperationalStatus.Approved));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20,
            operationalStatus: BusinessOperationalStatus.PendingApproval,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Pending One");
    }

    [Fact]
    public async Task GetBusinessesPage_Should_RespectPagination()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        for (var i = 1; i <= 5; i++)
            db.Set<Business>().Add(CreateBusiness($"Business {i:D2}", BusinessOperationalStatus.Approved));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesPageHandler(db);
        var (items, total) = await handler.HandleAsync(2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBusinessesPage_Should_FilterApprovedInactiveReadinessQueue()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var approvedActive = CreateBusiness("Active Approved", BusinessOperationalStatus.Approved);
        approvedActive.IsActive = true;
        var approvedInactive = CreateBusiness("Inactive Approved", BusinessOperationalStatus.Approved);
        approvedInactive.IsActive = false;
        db.Set<Business>().AddRange(approvedActive, approvedInactive);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20,
            readinessFilter: BusinessReadinessQueueFilter.ApprovedInactive,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Inactive Approved");
    }

    // ─── GetBusinessForEditHandler ────────────────────────────────────────────

    [Fact]
    public async Task GetBusinessForEdit_Should_ReturnDto_WhenBusinessExists()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var business = CreateBusiness("Edit Me", BusinessOperationalStatus.Approved);
        business.LegalName = "Edit Me GmbH";
        business.ContactEmail = "contact@edit.de";
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessForEditHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(business.Id);
        result.Name.Should().Be("Edit Me");
        result.LegalName.Should().Be("Edit Me GmbH");
        result.ContactEmail.Should().Be("contact@edit.de");
    }

    [Fact]
    public async Task GetBusinessForEdit_Should_ReturnNull_WhenBusinessNotFound()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var handler = new GetBusinessForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessForEdit_Should_IncludeMemberAndLocationCounts()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var business = CreateBusiness("With Members", BusinessOperationalStatus.Approved);
        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { BusinessId = business.Id, UserId = Guid.NewGuid(), Role = BusinessMemberRole.Owner, IsActive = true, RowVersion = [1] },
            new BusinessMember { BusinessId = business.Id, UserId = Guid.NewGuid(), Role = BusinessMemberRole.Staff, IsActive = true, RowVersion = [1] });
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Main Branch",
            IsPrimary = true,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessForEditHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.MemberCount.Should().Be(2);
        result.ActiveOwnerCount.Should().Be(1);
        result.LocationCount.Should().Be(1);
        result.PrimaryLocationCount.Should().Be(1);
    }

    // ─── GetBusinessPublicDetailHandler ──────────────────────────────────────

    [Fact]
    public async Task GetBusinessPublicDetail_Should_ReturnNull_WhenBusinessIsInactive()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var business = CreateBusiness("Inactive", BusinessOperationalStatus.Approved);
        business.IsActive = false;
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessPublicDetailHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessPublicDetail_Should_ReturnNull_WhenBusinessIsActiveButNotApproved()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var business = CreateBusiness("Pending Active", BusinessOperationalStatus.PendingApproval);
        business.IsActive = true;
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessPublicDetailHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessPublicDetail_Should_ReturnNull_ForEmptyGuid()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var handler = new GetBusinessPublicDetailHandler(db);

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessPublicDetail_Should_ReturnDetail_WithLocationsAndLoyaltyProgram()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Café Aurora",
            IsActive = true,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            RowVersion = [1, 2, 3]
        };
        business.OperationalStatus = BusinessOperationalStatus.Approved;
        db.Set<Business>().Add(business);
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = businessId,
            Name = "Main",
            IsPrimary = true,
            CountryCode = "DE",
            RowVersion = [1]
        });
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            BusinessId = businessId,
            Url = "https://cdn.test/logo.png",
            IsPrimary = true,
            RowVersion = [1]
        });
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "Aurora Rewards",
            IsActive = true,
            RowVersion = [1]
        });
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = Guid.NewGuid(),
            LoyaltyProgramId = programId,
            PointsRequired = 100,
            Description = "Free Coffee",
            AllowSelfRedemption = true,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessPublicDetailHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Café Aurora");
        result.Locations.Should().HaveCount(1);
        result.PrimaryImageUrl.Should().Be("https://cdn.test/logo.png");
        result.LoyaltyProgram.Should().NotBeNull();
        result.LoyaltyProgram!.RewardTiers.Should().HaveCount(1);
        result.LoyaltyProgram.RewardTiers[0].Description.Should().Be("Free Coffee");
    }

    [Fact]
    public async Task GetBusinessPublicDetail_Should_ReturnNull_WhenBusinessNotFound()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var handler = new GetBusinessPublicDetailHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessesForDiscovery_Should_FilterOutActiveButUnapprovedBusinesses()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var approved = CreateBusiness("Approved Public", BusinessOperationalStatus.Approved);
        var pending = CreateBusiness("Pending Public", BusinessOperationalStatus.PendingApproval);
        pending.IsActive = true;
        db.Set<Business>().AddRange(approved, pending);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessesForDiscoveryHandler(db);
        var (items, total) = await handler.HandleAsync(new BusinessDiscoveryRequestDto(), TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Approved Public");
    }

    // ─── GetBusinessMembersPageHandler ────────────────────────────────────────

    [Fact]
    public async Task GetBusinessMembersPage_Should_ReturnMembersForBusiness()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test", DefaultCurrency = "EUR", DefaultCulture = "de-DE", DefaultTimeZoneId = "Europe/Berlin", RowVersion = [1] });
        db.Set<User>().AddRange(
            CreateUser(userId1, "owner@test.de", "Alice", "Owner"),
            CreateUser(userId2, "staff@test.de", "Bob", "Staff"));
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { BusinessId = businessId, UserId = userId1, Role = BusinessMemberRole.Owner, IsActive = true, RowVersion = [1] },
            new BusinessMember { BusinessId = businessId, UserId = userId2, Role = BusinessMemberRole.Staff, IsActive = true, RowVersion = [1] },
            new BusinessMember { BusinessId = otherBusinessId, UserId = Guid.NewGuid(), Role = BusinessMemberRole.Owner, IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessMembersPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            businessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(m => m.BusinessId == businessId);
    }

    [Fact]
    public async Task GetBusinessMembersPage_Should_FilterByQueryString()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Set<User>().AddRange(
            CreateUser(userId1, "alice@test.de", "Alice", "Wonder"),
            CreateUser(userId2, "bob@test.de", "Bob", "Builder"));
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { BusinessId = businessId, UserId = userId1, Role = BusinessMemberRole.Owner, IsActive = true, RowVersion = [1] },
            new BusinessMember { BusinessId = businessId, UserId = userId2, Role = BusinessMemberRole.Staff, IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessMembersPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            businessId, 1, 20, "Alice", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().UserDisplayName.Should().Contain("Alice");
    }

    [Fact]
    public async Task GetBusinessMembersPage_Should_ReturnEmpty_WhenNoMembersExist()
    {
        await using var db = BusinessQueryTestDbContext.Create();
        var handler = new GetBusinessMembersPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            Guid.NewGuid(), 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Business CreateBusiness(string name, BusinessOperationalStatus status)
    {
        return new Business
        {
            Name = name,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            OperationalStatus = status,
            IsActive = status == BusinessOperationalStatus.Approved,
            RowVersion = [1, 2, 3]
        };
    }

    private static User CreateUser(Guid id, string email, string? firstName = null, string? lastName = null)
    {
        return new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
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

    private sealed class BusinessQueryTestDbContext : DbContext, IAppDbContext
    {
        private BusinessQueryTestDbContext(DbContextOptions<BusinessQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_query_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessQueryTestDbContext(options);
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

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Coordinate);
            });

            modelBuilder.Entity<BusinessInvitation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessMedia>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Url).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessEngagementStats>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BusinessId).IsRequired();
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
