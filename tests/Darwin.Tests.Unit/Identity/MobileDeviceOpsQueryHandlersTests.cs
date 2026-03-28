using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Identity;

public sealed class MobileDeviceOpsQueryHandlersTests
{
    [Fact]
    public async Task GetMobileDeviceOpsSummary_Should_ReturnFleetSignals()
    {
        await using var db = MobileDeviceOpsTestDbContext.Create();
        var businessUserId = Guid.NewGuid();
        var consumerUserId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<User>().AddRange(
            new User("business@example.com", "hash", "stamp") { Id = businessUserId, FirstName = "Mina", LastName = "Stark" },
            new User("consumer@example.com", "hash", "stamp") { Id = consumerUserId, FirstName = "Lio", LastName = "Bauer" });
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<BusinessMember>().Add(new BusinessMember { Id = Guid.NewGuid(), BusinessId = businessId, UserId = businessUserId, IsActive = true });
        db.Set<UserDevice>().AddRange(
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = businessUserId,
                DeviceId = "device-1",
                Platform = MobilePlatform.Android,
                PushToken = "push-1",
                NotificationsEnabled = true,
                LastSeenAtUtc = new DateTime(2030, 1, 29, 10, 0, 0, DateTimeKind.Utc),
                AppVersion = "1.2.0",
                IsActive = true
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = consumerUserId,
                DeviceId = "device-2",
                Platform = MobilePlatform.iOS,
                PushToken = null,
                NotificationsEnabled = false,
                LastSeenAtUtc = new DateTime(2029, 12, 1, 8, 0, 0, DateTimeKind.Utc),
                AppVersion = "1.1.0",
                IsActive = true
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMobileDeviceOpsSummaryHandler(db, new StubClock());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.TotalActiveDevices.Should().Be(2);
        result.BusinessMemberDevicesCount.Should().Be(1);
        result.StaleDevicesCount.Should().Be(1);
        result.DevicesMissingPushTokenCount.Should().Be(1);
        result.NotificationsDisabledCount.Should().Be(1);
        result.AndroidDevicesCount.Should().Be(1);
        result.IosDevicesCount.Should().Be(1);
        result.RecentVersions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMobileDevicesPage_Should_FilterBusinessMemberDevices()
    {
        await using var db = MobileDeviceOpsTestDbContext.Create();
        var businessUserId = Guid.NewGuid();
        var consumerUserId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<User>().AddRange(
            new User("business@example.com", "hash", "stamp") { Id = businessUserId, FirstName = "Mina", LastName = "Stark" },
            new User("consumer@example.com", "hash", "stamp") { Id = consumerUserId, FirstName = "Lio", LastName = "Bauer" });
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<BusinessMember>().Add(new BusinessMember { Id = Guid.NewGuid(), BusinessId = businessId, UserId = businessUserId, IsActive = true });
        db.Set<UserDevice>().AddRange(
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = businessUserId,
                DeviceId = "device-1",
                Platform = MobilePlatform.Android,
                PushToken = "push-1",
                NotificationsEnabled = true,
                LastSeenAtUtc = new DateTime(2030, 1, 29, 10, 0, 0, DateTimeKind.Utc),
                AppVersion = "1.2.0",
                IsActive = true
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = consumerUserId,
                DeviceId = "device-2",
                Platform = MobilePlatform.iOS,
                PushToken = "push-2",
                NotificationsEnabled = true,
                LastSeenAtUtc = new DateTime(2030, 1, 28, 8, 0, 0, DateTimeKind.Utc),
                AppVersion = "1.1.0",
                IsActive = true
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMobileDevicesPageHandler(db, new StubClock());

        var result = await handler.HandleAsync(state: "business-members", ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].UserEmail.Should().Be("business@example.com");
        result.Items[0].BusinessMembershipCount.Should().Be(1);
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2030, 1, 30, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class MobileDeviceOpsTestDbContext : DbContext, IAppDbContext
    {
        private MobileDeviceOpsTestDbContext(DbContextOptions<MobileDeviceOpsTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MobileDeviceOpsTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MobileDeviceOpsTestDbContext>()
                .UseInMemoryDatabase($"darwin_mobile_device_ops_{Guid.NewGuid()}")
                .Options;
            return new MobileDeviceOpsTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

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
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<UserDevice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.DeviceId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.User);
            });
        }
    }
}
