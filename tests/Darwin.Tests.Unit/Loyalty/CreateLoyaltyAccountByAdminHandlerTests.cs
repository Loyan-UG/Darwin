using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Loyalty;

public sealed class CreateLoyaltyAccountByAdminHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CreateAccount_WhenBusinessAndUserAreValid()
    {
        await using var db = CreateLoyaltyAccountTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<User>().Add(new User("member@example.com", "hash", "stamp")
        {
            Id = userId,
            FirstName = "Mia",
            LastName = "Keller"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountByAdminHandler(db, new StubClock());

        var result = await handler.HandleAsync(new CreateLoyaltyAccountByAdminDto
        {
            BusinessId = businessId,
            UserId = userId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(LoyaltyAccountStatus.Active);
        result.Value.UserEmail.Should().Be("member@example.com");
        result.Value.UserDisplayName.Should().Be("Mia Keller");

        var persisted = await db.Set<LoyaltyAccount>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.BusinessId.Should().Be(businessId);
        persisted.UserId.Should().Be(userId);
        persisted.PointsBalance.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectDuplicateAccount()
    {
        await using var db = CreateLoyaltyAccountTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<User>().Add(new User("member@example.com", "hash", "stamp") { Id = userId });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyAccountByAdminHandler(db, new StubClock());

        var result = await handler.HandleAsync(new CreateLoyaltyAccountByAdminDto
        {
            BusinessId = businessId,
            UserId = userId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class CreateLoyaltyAccountTestDbContext : DbContext, IAppDbContext
    {
        private CreateLoyaltyAccountTestDbContext(DbContextOptions<CreateLoyaltyAccountTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CreateLoyaltyAccountTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CreateLoyaltyAccountTestDbContext>()
                .UseInMemoryDatabase($"darwin_create_loyalty_account_admin_{Guid.NewGuid()}")
                .Options;
            return new CreateLoyaltyAccountTestDbContext(options);
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

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });
        }
    }
}
