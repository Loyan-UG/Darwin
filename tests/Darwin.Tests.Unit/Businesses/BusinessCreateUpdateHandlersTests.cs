using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers business create and edit rules that protect the operational approval workflow.
/// </summary>
public sealed class BusinessCreateUpdateHandlersTests
{
    [Fact]
    public async Task CreateBusiness_Should_DefaultPendingApprovalBusinesses_ToInactive()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var handler = new CreateBusinessHandler(db, new BusinessCreateDtoValidator());

        var id = await handler.HandleAsync(new BusinessCreateDto
        {
            Name = "Baeckerei Morgenstern",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.PendingApproval);
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBusiness_Should_NotActivatePendingApprovalBusiness()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var entity = CreateBusiness(BusinessOperationalStatus.PendingApproval, isActive: false);

        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator());

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = entity.Name,
            LegalName = entity.LegalName,
            Category = entity.Category,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.PendingApproval);
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBusiness_Should_AllowApprovedBusiness_ToRemainActive()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        entity.ApprovedAtUtc = new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc);

        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator());

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = "Baeckerei Morgenstern Innenstadt",
            LegalName = entity.LegalName,
            Category = entity.Category,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Approved);
        persisted.IsActive.Should().BeTrue();
        persisted.Name.Should().Be("Baeckerei Morgenstern Innenstadt");
    }

    private static Business CreateBusiness(BusinessOperationalStatus status, bool isActive)
    {
        return new Business
        {
            Name = "Baeckerei Morgenstern",
            LegalName = "Morgenstern GmbH",
            Category = BusinessCategoryKind.Bakery,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            OperationalStatus = status,
            IsActive = isActive,
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class BusinessCreateUpdateTestDbContext : DbContext, IAppDbContext
    {
        private BusinessCreateUpdateTestDbContext(DbContextOptions<BusinessCreateUpdateTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessCreateUpdateTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessCreateUpdateTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_create_update_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessCreateUpdateTestDbContext(options);
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
        }
    }
}
