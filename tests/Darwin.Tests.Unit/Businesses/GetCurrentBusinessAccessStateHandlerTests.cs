using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
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
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<User>().Add(CreateUser(userId));
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Berlin Mitte",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeTrue();
        result.IsBusinessClientAccessAllowed.Should().BeTrue();
        result.IsSetupComplete.Should().BeTrue();
        result.HasActiveMembership.Should().BeTrue();
        result.IsUserActive.Should().BeTrue();
        result.IsUserEmailConfirmed.Should().BeTrue();
        result.IsUserLockedOut.Should().BeFalse();
        result.BlockingReason.Should().BeNull();
    }

    [Fact]
    public async Task PendingApprovalBusiness_Should_BlockOperations_AndExposeChecklistGaps()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        var userId = Guid.NewGuid();
        business.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        business.IsActive = false;
        business.ContactEmail = null;
        business.LegalName = null;

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<User>().Add(CreateUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeFalse();
        result.IsBusinessClientAccessAllowed.Should().BeTrue();
        result.IsSetupComplete.Should().BeFalse();
        result.HasActiveOwner.Should().BeTrue();
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
        var userId = Guid.NewGuid();
        business.OperationalStatus = BusinessOperationalStatus.Suspended;
        business.IsActive = false;
        business.SuspendedAtUtc = new DateTime(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
        business.SuspensionReason = "Manual review in progress.";

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<User>().Add(CreateUser(userId));
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Hamburg",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsOperationsAllowed.Should().BeFalse();
        result.IsBusinessClientAccessAllowed.Should().BeTrue();
        result.SuspendedAtUtc.Should().Be(new DateTime(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc));
        result.SuspensionReason.Should().Be("Manual review in progress.");
        result.BlockingReason.Should().Be("Manual review in progress.");
    }

    [Fact]
    public async Task InactiveMembership_Should_BlockBusinessClientAccess_BeforeBusinessChecks()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = false
        });
        db.Set<User>().Add(CreateUser(userId));
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Munich",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.HasActiveMembership.Should().BeFalse();
        result.IsBusinessClientAccessAllowed.Should().BeFalse();
        result.IsOperationsAllowed.Should().BeFalse();
        result.BlockingReason.Should().Be("Business membership is no longer active for this user.");
    }

    [Fact]
    public async Task UnconfirmedUser_Should_BlockBusinessClientAccess()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        user.EmailConfirmed = false;

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<User>().Add(user);
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Cologne",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsUserEmailConfirmed.Should().BeFalse();
        result.IsBusinessClientAccessAllowed.Should().BeFalse();
        result.IsOperationsAllowed.Should().BeFalse();
        result.BlockingReason.Should().Be("User email confirmation is still required.");
    }

    [Fact]
    public async Task LockedOutUser_Should_BlockBusinessClientAccess()
    {
        await using var db = BusinessAccessStateTestDbContext.Create();
        var business = CreateBusiness();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(30);

        db.Set<Business>().Add(business);
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        });
        db.Set<User>().Add(user);
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Frankfurt",
            IsPrimary = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentBusinessAccessStateHandler(db);
        var result = await handler.HandleAsync(business.Id, userId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.IsUserLockedOut.Should().BeTrue();
        result.IsBusinessClientAccessAllowed.Should().BeFalse();
        result.IsOperationsAllowed.Should().BeFalse();
        result.BlockingReason.Should().Be("User access is currently locked.");
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

    private static User CreateUser(Guid userId)
    {
        return new User("owner@backhaus-elbe.de", "hash", "stamp")
        {
            Id = userId,
            EmailConfirmed = true,
            IsActive = true
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
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
            });
        }
    }
}
