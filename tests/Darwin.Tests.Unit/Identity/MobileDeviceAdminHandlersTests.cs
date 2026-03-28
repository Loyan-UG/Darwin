using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Identity;

public sealed class MobileDeviceAdminHandlersTests
{
    [Fact]
    public async Task ClearUserDevicePushToken_Should_RemoveTokenAndKeepDeviceActive()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-1",
            Platform = MobilePlatform.Android,
            PushToken = "push-token",
            PushTokenUpdatedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            NotificationsEnabled = true,
            IsActive = true
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db);
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        device.PushToken.Should().BeNull();
        device.PushTokenUpdatedAtUtc.Should().BeNull();
        device.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateUserDevice_Should_DisableNotificationsAndClearToken()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-2",
            Platform = MobilePlatform.iOS,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db);
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        device.IsActive.Should().BeFalse();
        device.NotificationsEnabled.Should().BeFalse();
        device.PushToken.Should().BeNull();
    }

    private sealed class MobileDeviceAdminTestDbContext : DbContext, IAppDbContext
    {
        private MobileDeviceAdminTestDbContext(DbContextOptions<MobileDeviceAdminTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MobileDeviceAdminTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MobileDeviceAdminTestDbContext>()
                .UseInMemoryDatabase($"darwin_mobile_device_admin_{Guid.NewGuid()}")
                .Options;
            return new MobileDeviceAdminTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

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
