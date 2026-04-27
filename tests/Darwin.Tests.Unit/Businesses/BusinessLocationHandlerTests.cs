using System;
using System.Threading.Tasks;
using Darwin.Application;
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
/// Unit tests for business location command handlers:
/// <see cref="CreateBusinessLocationHandler"/>, <see cref="UpdateBusinessLocationHandler"/>,
/// and <see cref="SoftDeleteBusinessLocationHandler"/>.
/// </summary>
public sealed class BusinessLocationHandlerTests
{
    // ─── CreateBusinessLocationHandler ───────────────────────────────────────

    [Fact]
    public async Task CreateBusinessLocation_Should_PersistLocation_WhenBusinessExists()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessLocationHandler(db, new BusinessLocationCreateDtoValidator(), new TestLocalizer());

        var id = await handler.HandleAsync(new BusinessLocationCreateDto
        {
            BusinessId = business.Id,
            Name = "Main Branch",
            City = "Northeim",
            CountryCode = "DE",
            IsPrimary = false
        }, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);

        var location = await db.Set<BusinessLocation>().SingleAsync(TestContext.Current.CancellationToken);
        location.Name.Should().Be("Main Branch");
        location.BusinessId.Should().Be(business.Id);
        location.City.Should().Be("Northeim");
    }

    [Fact]
    public async Task CreateBusinessLocation_Should_Throw_WhenBusinessNotFound()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var handler = new CreateBusinessLocationHandler(db, new BusinessLocationCreateDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessLocationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Ghost Branch"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBusinessLocation_Should_ClearOtherPrimaryFlags_WhenIsPrimaryIsTrue()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var existingPrimary = new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Old Primary",
            IsPrimary = true
        };
        db.Set<BusinessLocation>().Add(existingPrimary);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessLocationHandler(db, new BusinessLocationCreateDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessLocationCreateDto
        {
            BusinessId = business.Id,
            Name = "New Primary",
            IsPrimary = true
        }, TestContext.Current.CancellationToken);

        var all = await db.Set<BusinessLocation>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(x => x.IsPrimary).Should().Be(1, "only one location should be primary");
        all.Single(x => x.IsPrimary).Name.Should().Be("New Primary");
    }

    [Fact]
    public async Task CreateBusinessLocation_Should_TrimName()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessLocationHandler(db, new BusinessLocationCreateDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessLocationCreateDto
        {
            BusinessId = business.Id,
            Name = "  Trimmed Branch  "
        }, TestContext.Current.CancellationToken);

        var location = await db.Set<BusinessLocation>().SingleAsync(TestContext.Current.CancellationToken);
        location.Name.Should().Be("Trimmed Branch");
    }

    // ─── UpdateBusinessLocationHandler ───────────────────────────────────────

    [Fact]
    public async Task UpdateBusinessLocation_Should_UpdateFields_WhenLocationExists()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var location = new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Old Name",
            City = "Old City",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessLocationHandler(db, new BusinessLocationEditDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessLocationEditDto
        {
            Id = location.Id,
            BusinessId = business.Id,
            Name = "New Name",
            City = "New City",
            RowVersion = location.RowVersion
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<BusinessLocation>().SingleAsync(TestContext.Current.CancellationToken);
        updated.Name.Should().Be("New Name");
        updated.City.Should().Be("New City");
    }

    [Fact]
    public async Task UpdateBusinessLocation_Should_Throw_WhenLocationNotFound()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var handler = new UpdateBusinessLocationHandler(db, new BusinessLocationEditDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessLocationEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Ghost",
            RowVersion = new byte[] { 1 }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBusinessLocation_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var location = new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Conflict Location",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessLocationHandler(db, new BusinessLocationEditDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessLocationEditDto
        {
            Id = location.Id,
            BusinessId = business.Id,
            Name = "Updated",
            RowVersion = new byte[] { 99, 88 } // stale
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateBusinessLocation_Should_ClearOtherPrimaryFlags_WhenIsPrimaryIsTrue()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var business = CreateBusiness();
        db.Set<Business>().Add(business);

        var oldPrimary = new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Old Primary",
            IsPrimary = true,
            RowVersion = new byte[] { 1 }
        };
        var target = new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Target",
            IsPrimary = false,
            RowVersion = new byte[] { 2 }
        };
        db.Set<BusinessLocation>().AddRange(oldPrimary, target);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessLocationHandler(db, new BusinessLocationEditDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessLocationEditDto
        {
            Id = target.Id,
            BusinessId = business.Id,
            Name = "Target Updated",
            IsPrimary = true,
            RowVersion = target.RowVersion
        }, TestContext.Current.CancellationToken);

        var all = await db.Set<BusinessLocation>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(x => x.IsPrimary).Should().Be(1);
        all.Single(x => x.IsPrimary).Id.Should().Be(target.Id);
    }

    // ─── SoftDeleteBusinessLocationHandler ───────────────────────────────────

    [Fact]
    public async Task SoftDeleteBusinessLocation_Should_MarkAsDeleted()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var location = new BusinessLocation
        {
            BusinessId = Guid.NewGuid(),
            Name = "Delete Me",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessLocationHandler(db, new BusinessLocationDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessLocationDeleteDto
        {
            Id = location.Id,
            RowVersion = location.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<BusinessLocation>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteBusinessLocation_Should_Fail_WhenLocationNotFound()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var handler = new SoftDeleteBusinessLocationHandler(db, new BusinessLocationDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessLocationDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessLocationNotFound");
    }

    [Fact]
    public async Task SoftDeleteBusinessLocation_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var location = new BusinessLocation
        {
            BusinessId = Guid.NewGuid(),
            Name = "Conflict",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessLocationHandler(db, new BusinessLocationDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessLocationDeleteDto
        {
            Id = location.Id,
            RowVersion = new byte[] { 99, 88 } // stale
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task SoftDeleteBusinessLocation_Should_BeIdempotent_WhenAlreadyDeleted()
    {
        await using var db = BusinessLocationTestDbContext.Create();
        var location = new BusinessLocation
        {
            BusinessId = Guid.NewGuid(),
            Name = "Already Deleted",
            IsDeleted = true,
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<BusinessLocation>().Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBusinessLocationHandler(db, new BusinessLocationDeleteDtoValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new BusinessLocationDeleteDto
        {
            Id = location.Id,
            RowVersion = location.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("soft-deleting an already-deleted location should be idempotent");
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
            RowVersion = new byte[] { 1, 2, 3 }
        };
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class BusinessLocationTestDbContext : DbContext, IAppDbContext
    {
        private BusinessLocationTestDbContext(DbContextOptions<BusinessLocationTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessLocationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessLocationTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_location_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessLocationTestDbContext(options);
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

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
