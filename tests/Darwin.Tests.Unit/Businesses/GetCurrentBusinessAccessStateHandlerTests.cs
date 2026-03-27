using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers current-business access-state projections used by soft-gated business clients.
/// </summary>
public sealed class GetCurrentBusinessAccessStateHandlerTests
{
    [Fact]
    public async Task ApprovedActiveBusiness_Should_AllowOperations_AndMarkSetupComplete()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = Guid.NewGuid(),
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Berlin Mitte",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeTrue();
        result.IsSetupComplete.Should().BeTrue();
        result.BlockingReason.Should().BeNull();
    }

    [Fact]
    public async Task PendingApprovalBusiness_Should_BlockOperations_AndExposeChecklistGaps()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        business.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        business.IsActive = false;
        business.ContactEmail = null;
        business.LegalName = null;

        db.Set<Business>().Add(business);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeFalse();
        result.IsSetupComplete.Should().BeFalse();
        result.HasActiveOwner.Should().BeFalse();
        result.HasPrimaryLocation.Should().BeFalse();
        result.HasContactEmail.Should().BeFalse();
        result.HasLegalName.Should().BeFalse();
        result.BlockingReason.Should().Be("Business approval is still pending.");
    }

    [Fact]
    public async Task SuspendedBusiness_Should_PreserveSuspensionMetadata_AndBlockOperations()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        business.OperationalStatus = BusinessOperationalStatus.Suspended;
        business.IsActive = false;
        business.SuspendedAtUtc = new DateTime(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
        business.SuspensionReason = "Manual review in progress.";

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = Guid.NewGuid(),
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Hamburg",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeFalse();
        result.SuspendedAtUtc.Should().Be(new DateTime(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc));
        result.SuspensionReason.Should().Be("Manual review in progress.");
        result.BlockingReason.Should().Be("Manual review in progress.");
    }

    private static Business CreateBusiness()
    {
        return new Business
        {
            Name = "Backhaus Elbe",
            LegalName = "Backhaus Elbe GmbH",
            ContactEmail = "betrieb@backhaus-elbe.de",
            Category = BusinessCategoryKind.Bakery,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            IsActive = true,
            OperationalStatus = BusinessOperationalStatus.Approved,
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class BusinessAccessStateTestDbContext : DbContext, IAppDbContext
    {
        private BusinessAccessStateTestDbContext(DbContextOptions<BusinessAccessStateTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessAccessStateTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessAccessStateTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_access_state_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessAccessStateTestDbContext(options);
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

            modelBuilder.Entity<BusinessMember>(builder => builder.HasKey(x => x.Id));
            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
            });
        }
    }
}
