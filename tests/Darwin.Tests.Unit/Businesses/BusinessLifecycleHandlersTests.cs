using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers operational business lifecycle actions such as approval, suspension, and reactivation.
/// </summary>
public sealed class BusinessLifecycleHandlersTests
{
    [Fact]
    public async Task ApproveBusiness_Should_SetApprovedState_AndClearSuspension()
    {
        await using var db = BusinessLifecycleTestDbContext.Create();
        var entity = CreateBusiness();
        entity.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        entity.IsActive = false;
        entity.SuspendedAtUtc = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        entity.SuspensionReason = "Old note";

        db.Set<Business>().Add(entity);
        SeedApprovalPrerequisites(db, entity.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApproveBusinessHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc)),
            new BusinessLifecycleActionDtoValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Approved);
        persisted.IsActive.Should().BeTrue();
        persisted.ApprovedAtUtc.Should().Be(new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc));
        persisted.SuspendedAtUtc.Should().BeNull();
        persisted.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public async Task ApproveBusiness_Should_Reject_WhenApprovalPrerequisitesAreMissing()
    {
        await using var db = BusinessLifecycleTestDbContext.Create();
        var entity = CreateBusiness();
        entity.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        entity.IsActive = false;
        entity.ContactEmail = null;

        db.Set<Business>().Add(entity);
        SeedApprovalPrerequisites(db, entity.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApproveBusinessHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc)),
            new BusinessLifecycleActionDtoValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("BusinessApprovalPrerequisitesMissing");
    }

    [Fact]
    public async Task SuspendBusiness_Should_SetSuspendedState_AndDeactivateBusiness()
    {
        await using var db = BusinessLifecycleTestDbContext.Create();
        var entity = CreateBusiness();
        entity.OperationalStatus = BusinessOperationalStatus.Approved;
        entity.IsActive = true;
        entity.ApprovedAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc);

        db.Set<Business>().Add(entity);
        SeedApprovalPrerequisites(db, entity.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendBusinessHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 6, 9, 30, 0, DateTimeKind.Utc)),
            new BusinessLifecycleActionDtoValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Note = "Compliance review pending."
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Suspended);
        persisted.IsActive.Should().BeFalse();
        persisted.SuspendedAtUtc.Should().Be(new DateTime(2030, 1, 6, 9, 30, 0, DateTimeKind.Utc));
        persisted.SuspensionReason.Should().Be("Compliance review pending.");
    }

    [Fact]
    public async Task ReactivateBusiness_Should_RestoreApprovedState_AndClearSuspension()
    {
        await using var db = BusinessLifecycleTestDbContext.Create();
        var entity = CreateBusiness();
        entity.OperationalStatus = BusinessOperationalStatus.Suspended;
        entity.IsActive = false;
        entity.SuspendedAtUtc = new DateTime(2030, 1, 4, 7, 0, 0, DateTimeKind.Utc);
        entity.SuspensionReason = "Manual hold";

        db.Set<Business>().Add(entity);
        SeedApprovalPrerequisites(db, entity.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ReactivateBusinessHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc)),
            new BusinessLifecycleActionDtoValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Approved);
        persisted.IsActive.Should().BeTrue();
        persisted.SuspendedAtUtc.Should().BeNull();
        persisted.SuspensionReason.Should().BeNull();
        persisted.ApprovedAtUtc.Should().Be(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ReactivateBusiness_Should_Reject_WhenApprovalPrerequisitesAreMissing()
    {
        await using var db = BusinessLifecycleTestDbContext.Create();
        var entity = CreateBusiness();
        entity.OperationalStatus = BusinessOperationalStatus.Suspended;
        entity.IsActive = false;
        entity.ContactEmail = null;

        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ReactivateBusinessHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc)),
            new BusinessLifecycleActionDtoValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("BusinessApprovalPrerequisitesMissing");
    }

    private static Business CreateBusiness()
    {
        return new Business
        {
            Name = "Konditorei Sonnenschein",
            LegalName = "Sonnenschein GmbH",
            ContactEmail = "kontakt@sonnenschein.test",
            Category = BusinessCategoryKind.Bakery,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            IsActive = true,
            RowVersion = [1, 2, 3]
        };
    }

    private static void SeedApprovalPrerequisites(IAppDbContext db, Guid businessId)
    {
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Role = BusinessMemberRole.Owner,
            IsActive = true,
            RowVersion = [1]
        });

        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = businessId,
            Name = "Main",
            IsPrimary = true,
            RowVersion = [1]
        });
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class BusinessLifecycleTestDbContext : DbContext, IAppDbContext
    {
        private BusinessLifecycleTestDbContext(DbContextOptions<BusinessLifecycleTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessLifecycleTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessLifecycleTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_lifecycle_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessLifecycleTestDbContext(options);
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
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Coordinate);
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
