using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Commands;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db, new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        device.PushToken.Should().BeNull();
        device.PushTokenUpdatedAtUtc.Should().BeNull();
        device.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ClearUserDevicePushToken_Should_RejectEmptyOrNullRowVersion()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-3",
            Platform = MobilePlatform.Android,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db, new TestStringLocalizer<ValidationResource>());

        var emptyRowVersionResult = await handler.HandleAsync(device.Id, Array.Empty<byte>(), TestContext.Current.CancellationToken);
        emptyRowVersionResult.Succeeded.Should().BeFalse();
        emptyRowVersionResult.Error.Should().Be("RowVersionRequired");

        var nullRowVersionResult = await handler.HandleAsync(device.Id, null, TestContext.Current.CancellationToken);
        nullRowVersionResult.Succeeded.Should().BeFalse();
        nullRowVersionResult.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task ClearUserDevicePushToken_Should_RejectStaleRowVersion()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-10",
            Platform = MobilePlatform.Android,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(device.Id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DeviceConcurrencyConflict");
    }

    [Fact]
    public async Task ClearUserDevicePushToken_Should_RejectPostSaveConcurrencyException()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create(throwConcurrencyOnSecondSave: true);
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-4",
            Platform = MobilePlatform.Android,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db, new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DeviceConcurrencyConflict");
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
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db, new TestClock(), new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        device.IsActive.Should().BeFalse();
        device.NotificationsEnabled.Should().BeFalse();
        device.PushToken.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateUserDevice_Should_RejectEmptyOrNullRowVersion()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-5",
            Platform = MobilePlatform.iOS,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db, new TestClock(), new TestStringLocalizer<ValidationResource>());

        var emptyRowVersionResult = await handler.HandleAsync(device.Id, Array.Empty<byte>(), TestContext.Current.CancellationToken);
        emptyRowVersionResult.Succeeded.Should().BeFalse();
        emptyRowVersionResult.Error.Should().Be("RowVersionRequired");

        var nullRowVersionResult = await handler.HandleAsync(device.Id, null, TestContext.Current.CancellationToken);
        nullRowVersionResult.Succeeded.Should().BeFalse();
        nullRowVersionResult.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task DeactivateUserDevice_Should_RejectStaleRowVersion()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-11",
            Platform = MobilePlatform.iOS,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db, new TestClock(), new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(device.Id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DeviceConcurrencyConflict");
    }

    [Fact]
    public async Task DeactivateUserDevice_Should_RejectPostSaveConcurrencyException()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create(throwConcurrencyOnSecondSave: true);
        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceId = "device-6",
            Platform = MobilePlatform.iOS,
            PushToken = "push-token",
            NotificationsEnabled = true,
            IsActive = true,
            RowVersion = [1]
        };
        db.Set<UserDevice>().Add(device);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db, new TestClock(), new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(device.Id, device.RowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DeviceConcurrencyConflict");
    }

    [Fact]
    public async Task HandleNotificationsDisabledBatchAsync_Should_ApplyTakeCap_And_FilterByPlatformAndBusiness()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        db.Set<UserDevice>().AddRange(
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userA,
                DeviceId = "batch-android-1",
                Platform = MobilePlatform.Android,
                PushToken = "token-a",
                NotificationsEnabled = false,
                IsActive = true,
                CreatedAtUtc = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userA,
                DeviceId = "batch-android-2",
                Platform = MobilePlatform.Android,
                PushToken = "token-b",
                NotificationsEnabled = false,
                IsActive = true,
                CreatedAtUtc = new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userB,
                DeviceId = "batch-ios-1",
                Platform = MobilePlatform.iOS,
                PushToken = "token-c",
                NotificationsEnabled = false,
                IsActive = true,
                CreatedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userB,
                DeviceId = "batch-ignore",
                Platform = MobilePlatform.Android,
                PushToken = null,
                NotificationsEnabled = false,
                IsActive = true,
                CreatedAtUtc = new DateTime(2030, 1, 1, 7, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });

        db.Set<BusinessMember>().Add(new BusinessMember
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userA,
            IsActive = true,
            RowVersion = [1]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearUserDevicePushTokenHandler(db, new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleNotificationsDisabledBatchAsync(businessId, MobilePlatform.Android, 1, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AffectedCount.Should().Be(1);

        var affected = await db.Set<UserDevice>().Where(x => x.UserId == userA && x.Platform == MobilePlatform.Android).ToListAsync(TestContext.Current.CancellationToken);
        var staleAffected = affected.Single(x => x.DeviceId == "batch-android-2");
        var notAffected = affected.Single(x => x.DeviceId == "batch-android-1");

        staleAffected.PushToken.Should().BeNull();
        notAffected.PushToken.Should().Be("token-a");
    }

    [Fact]
    public async Task HandleStaleBatchAsync_Should_UseStaleCutoff_And_FilterByPlatform()
    {
        await using var db = MobileDeviceAdminTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var staleClock = new TestClock(new DateTime(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        var matchingUser = Guid.NewGuid();
        var excludedUser = Guid.NewGuid();

        db.Set<UserDevice>().AddRange(
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = matchingUser,
                DeviceId = "stale-android",
                Platform = MobilePlatform.Android,
                PushToken = "token-a",
                IsActive = true,
                LastSeenAtUtc = new DateTime(2029, 12, 31, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = matchingUser,
                DeviceId = "fresh-android",
                Platform = MobilePlatform.Android,
                PushToken = "token-b",
                IsActive = true,
                LastSeenAtUtc = new DateTime(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc).AddDays(-29),
                RowVersion = [1]
            },
            new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = excludedUser,
                DeviceId = "stale-ios",
                Platform = MobilePlatform.iOS,
                PushToken = "token-c",
                IsActive = true,
                LastSeenAtUtc = new DateTime(2029, 12, 25, 10, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });

        db.Set<BusinessMember>().AddRange(
            new BusinessMember
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                UserId = matchingUser,
                IsActive = true,
                RowVersion = [1]
            },
            new BusinessMember
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                UserId = excludedUser,
                IsActive = true,
                RowVersion = [1]
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeactivateUserDeviceHandler(db, staleClock, new TestStringLocalizer<ValidationResource>());
        var result = await handler.HandleStaleBatchAsync(businessId, MobilePlatform.Android, 10, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AffectedCount.Should().Be(1);

        var stale = await db.Set<UserDevice>().SingleAsync(x => x.DeviceId == "stale-android", TestContext.Current.CancellationToken);
        var fresh = await db.Set<UserDevice>().SingleAsync(x => x.DeviceId == "fresh-android", TestContext.Current.CancellationToken);
        var iOs = await db.Set<UserDevice>().SingleAsync(x => x.DeviceId == "stale-ios", TestContext.Current.CancellationToken);

        stale.IsActive.Should().BeFalse();
        stale.NotificationsEnabled.Should().BeFalse();
        stale.PushToken.Should().BeNull();

        fresh.IsActive.Should().BeTrue();
        fresh.PushToken.Should().Be("token-b");

        iOs.IsActive.Should().BeTrue();
        iOs.PushToken.Should().Be("token-c");
    }

    private sealed class MobileDeviceAdminTestDbContext : DbContext, IAppDbContext
    {
        private readonly bool _throwConcurrencyOnSecondSave;
        private int _saveChangesCallCount;

        private MobileDeviceAdminTestDbContext(DbContextOptions<MobileDeviceAdminTestDbContext> options, bool throwConcurrencyOnSecondSave = false)
            : base(options)
        {
            _throwConcurrencyOnSecondSave = throwConcurrencyOnSecondSave;
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MobileDeviceAdminTestDbContext Create(bool throwConcurrencyOnSecondSave = false)
        {
            var options = new DbContextOptionsBuilder<MobileDeviceAdminTestDbContext>()
                .UseInMemoryDatabase($"darwin_mobile_device_admin_{Guid.NewGuid()}")
                .Options;
            return new MobileDeviceAdminTestDbContext(options, throwConcurrencyOnSecondSave);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _saveChangesCallCount++;
            if (_throwConcurrencyOnSecondSave && _saveChangesCallCount > 1)
            {
                throw new DbUpdateConcurrencyException("Simulated concurrency conflict");
            }

            return base.SaveChangesAsync(cancellationToken);
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

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class TestClock : IClock
    {
        public TestClock()
        {
            UtcNow = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public TestClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
