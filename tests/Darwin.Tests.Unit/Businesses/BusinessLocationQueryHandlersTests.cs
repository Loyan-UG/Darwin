using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers read-only Business location and owner-override query handlers:
/// <see cref="GetBusinessLocationsPageHandler"/>, <see cref="GetBusinessLocationForEditHandler"/>,
/// and <see cref="GetBusinessOwnerOverrideAuditsPageHandler"/>.
/// </summary>
public sealed class BusinessLocationQueryHandlersTests
{
    // ─── GetBusinessLocationsPageHandler ─────────────────────────────────────

    [Fact]
    public async Task GetBusinessLocationsPage_Should_ReturnAllNonDeletedForBusiness()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Main Branch", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "North Branch", RowVersion = [1] },
            new BusinessLocation { BusinessId = otherBusinessId, Name = "Other Business", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Deleted Branch", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Name == "Deleted Branch");
        items.Should().NotContain(x => x.BusinessId == otherBusinessId);
    }

    [Fact]
    public async Task GetBusinessLocationsPage_Should_FilterByQuery_OnName()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Airport Lounge", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "City Center", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, "Airport", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Airport Lounge");
    }

    [Fact]
    public async Task GetBusinessLocationsPage_Should_FilterByQuery_OnCity()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Branch A", City = "Berlin", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Branch B", City = "Hamburg", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, "Hamburg", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().City.Should().Be("Hamburg");
    }

    [Fact]
    public async Task GetBusinessLocationsPage_Should_FilterByPrimaryFilter()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Primary Branch", IsPrimary = true, RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Secondary Branch", IsPrimary = false, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            businessId, 1, 10, filter: BusinessLocationQueueFilter.Primary,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Primary Branch");
    }

    [Fact]
    public async Task GetBusinessLocationsPage_Should_FilterByMissingAddressFilter()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Complete", AddressLine1 = "St 1", City = "Berlin", CountryCode = "DE", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Incomplete", AddressLine1 = null, City = "Hamburg", CountryCode = "DE", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            businessId, 1, 10, filter: BusinessLocationQueueFilter.MissingAddress,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Incomplete");
    }

    [Fact]
    public async Task GetBusinessLocationsPage_Should_RespectPagination()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        for (var i = 1; i <= 5; i++)
            db.Set<BusinessLocation>().Add(new BusinessLocation
            {
                BusinessId = businessId,
                Name = $"Branch {i:D2}",
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBusinessLocationsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<BusinessLocation>().AddRange(
            new BusinessLocation { BusinessId = businessId, Name = "Primary & Complete", IsPrimary = true, AddressLine1 = "St 1", City = "B", CountryCode = "DE", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Missing Address", AddressLine1 = null, City = "H", CountryCode = "DE", RowVersion = [1] },
            new BusinessLocation { BusinessId = businessId, Name = "Deleted", IsDeleted = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2);
        summary.PrimaryCount.Should().Be(1);
        summary.MissingAddressCount.Should().Be(1);
        // MissingCoordinatesCount queries owned-type null which requires relational provider; skipped for in-memory.
    }

    // ─── GetBusinessLocationForEditHandler ───────────────────────────────────

    [Fact]
    public async Task GetBusinessLocationForEdit_Should_ReturnLocation_WhenFound()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var location = new BusinessLocation
        {
            BusinessId = businessId,
            Name = "Edit Me",
            City = "Frankfurt",
            CountryCode = "DE",
            IsPrimary = true,
            RowVersion = [1]
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationForEditHandler(db);
        var result = await handler.HandleAsync(location.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(location.Id);
        result.BusinessId.Should().Be(businessId);
        result.Name.Should().Be("Edit Me");
        result.City.Should().Be("Frankfurt");
        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task GetBusinessLocationForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var handler = new GetBusinessLocationForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessLocationForEdit_Should_ReturnNull_WhenDeleted()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var location = new BusinessLocation
        {
            BusinessId = Guid.NewGuid(),
            Name = "Deleted Location",
            IsDeleted = true,
            RowVersion = [1]
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLocationForEditHandler(db);
        var result = await handler.HandleAsync(location.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─── GetBusinessOwnerOverrideAuditsPageHandler ────────────────────────────

    [Fact]
    public async Task GetBusinessOwnerOverrideAuditsPage_Should_ReturnAllForBusiness()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "owner@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<BusinessOwnerOverrideAudit>().AddRange(
            new BusinessOwnerOverrideAudit
            {
                BusinessId = businessId,
                BusinessMemberId = Guid.NewGuid(),
                AffectedUserId = userId,
                ActionKind = BusinessOwnerOverrideActionKind.DemoteOrDeactivate,
                Reason = "Business restructuring",
                RowVersion = [1]
            },
            new BusinessOwnerOverrideAudit
            {
                BusinessId = otherBusinessId,
                BusinessMemberId = Guid.NewGuid(),
                AffectedUserId = userId,
                ActionKind = BusinessOwnerOverrideActionKind.ForceRemove,
                Reason = "Other business reason",
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessOwnerOverrideAuditsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().BusinessId.Should().Be(businessId);
        items.Single().Reason.Should().Be("Business restructuring");
    }

    [Fact]
    public async Task GetBusinessOwnerOverrideAuditsPage_Should_FilterByQuery_OnReason()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Set<User>().AddRange(
            CreateUser(userId1, "user1@test.de"),
            CreateUser(userId2, "user2@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<BusinessOwnerOverrideAudit>().AddRange(
            new BusinessOwnerOverrideAudit
            {
                BusinessId = businessId,
                BusinessMemberId = Guid.NewGuid(),
                AffectedUserId = userId1,
                ActionKind = BusinessOwnerOverrideActionKind.DemoteOrDeactivate,
                Reason = "Ownership transfer",
                RowVersion = [1]
            },
            new BusinessOwnerOverrideAudit
            {
                BusinessId = businessId,
                BusinessMemberId = Guid.NewGuid(),
                AffectedUserId = userId2,
                ActionKind = BusinessOwnerOverrideActionKind.ForceRemove,
                Reason = "Account closure",
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessOwnerOverrideAuditsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, "transfer", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Reason.Should().Contain("transfer");
    }

    [Fact]
    public async Task GetBusinessOwnerOverrideAuditsPage_Should_RespectPagination()
    {
        await using var db = BusinessLocationQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "multi@test.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (var i = 1; i <= 5; i++)
        {
            db.Set<BusinessOwnerOverrideAudit>().Add(new BusinessOwnerOverrideAudit
            {
                BusinessId = businessId,
                BusinessMemberId = Guid.NewGuid(),
                AffectedUserId = userId,
                ActionKind = BusinessOwnerOverrideActionKind.DemoteOrDeactivate,
                Reason = $"Reason {i}",
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessOwnerOverrideAuditsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
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

    private sealed class BusinessLocationQueryTestDbContext : DbContext, IAppDbContext
    {
        private BusinessLocationQueryTestDbContext(DbContextOptions<BusinessLocationQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessLocationQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessLocationQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_location_query_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessLocationQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.OwnsOne(x => x.Coordinate, coord =>
                {
                    coord.Property(c => c.Latitude);
                    coord.Property(c => c.Longitude);
                    coord.Property(c => c.AltitudeMeters);
                });
            });

            modelBuilder.Entity<BusinessOwnerOverrideAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Reason).IsRequired();
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
