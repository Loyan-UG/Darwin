using Darwin.Contracts.Notifications;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Models.Notifications;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services.Notifications;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Consumer.Tests.Services;

/// <summary>
/// Tests Consumer push-registration orchestration without depending on MAUI static runtime singletons.
/// </summary>
public sealed class ConsumerPushRegistrationCoordinatorTests
{
    [Fact]
    public async Task TryRegisterCurrentDeviceAsync_Should_Fail_When_AccessTokenMissing()
    {
        var pushService = new FakePushRegistrationService();
        var coordinator = CreateCoordinator(pushService, accessToken: null);

        var result = await coordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("No access token");
        pushService.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task TryRegisterCurrentDeviceAsync_Should_Fail_When_PushTokenStateUnavailable()
    {
        var pushService = new FakePushRegistrationService();
        var coordinator = CreateCoordinator(
            pushService,
            tokenStateResult: Result<ConsumerPushTokenState>.Fail("Token unavailable."));

        var result = await coordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Token unavailable.");
        pushService.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task TryRegisterCurrentDeviceAsync_Should_Register_And_SaveSignature()
    {
        var stateStore = new FakeRegistrationStateStore();
        var pushService = new FakePushRegistrationService();
        var permissionService = new FakeNotificationPermissionService();
        var coordinator = CreateCoordinator(pushService, notificationPermissionService: permissionService, stateStore: stateStore);

        var result = await coordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        permissionService.CallCount.Should().Be(1);
        pushService.CallCount.Should().Be(1);
        pushService.LastDeviceId.Should().Be("device-1");
        pushService.LastPlatform.Should().Be(MobileDevicePlatform.Android);
        pushService.LastPushToken.Should().Be("push-token");
        pushService.LastNotificationsEnabled.Should().BeTrue();
        pushService.LastAppVersion.Should().Be("1.2.3");
        pushService.LastDeviceModel.Should().Be("Pixel 7");
        stateStore.Signature.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TryRegisterCurrentDeviceAsync_Should_Skip_DuplicateSignature()
    {
        var stateStore = new FakeRegistrationStateStore();
        var pushService = new FakePushRegistrationService();
        var permissionService = new FakeNotificationPermissionService();
        var coordinator = CreateCoordinator(pushService, notificationPermissionService: permissionService, stateStore: stateStore);

        (await coordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None)).Succeeded.Should().BeTrue();
        (await coordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None)).Succeeded.Should().BeTrue();

        permissionService.CallCount.Should().Be(1);
        pushService.CallCount.Should().Be(1);
    }

    [Fact]
    public void ResetCachedRegistrationState_Should_ClearSignature()
    {
        var stateStore = new FakeRegistrationStateStore { Signature = "old-signature" };
        var coordinator = CreateCoordinator(new FakePushRegistrationService(), stateStore: stateStore);

        coordinator.ResetCachedRegistrationState();

        stateStore.Signature.Should().BeEmpty();
    }

    private static ConsumerPushRegistrationCoordinator CreateCoordinator(
        FakePushRegistrationService pushRegistrationService,
        string? accessToken = "access-token",
        Result<ConsumerPushTokenState>? tokenStateResult = null,
        FakeNotificationPermissionService? notificationPermissionService = null,
        FakeRegistrationStateStore? stateStore = null)
    {
        return new ConsumerPushRegistrationCoordinator(
            pushRegistrationService,
            new FakeTokenStore(accessToken),
            notificationPermissionService ?? new FakeNotificationPermissionService(),
            new FakePushTokenProvider(tokenStateResult ?? Result<ConsumerPushTokenState>.Ok(new ConsumerPushTokenState
            {
                PushToken = "push-token",
                NotificationsEnabled = true
            })),
            new FakeDeviceIdProvider(),
            new FakeRuntimeInfo(),
            stateStore ?? new FakeRegistrationStateStore());
    }

    private sealed class FakeNotificationPermissionService : IConsumerNotificationPermissionService
    {
        public int CallCount { get; private set; }

        public Task<Result<bool>> EnsurePermissionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Result<bool>.Ok(true));
        }
    }

    private sealed class FakePushRegistrationService : IPushRegistrationService
    {
        public int CallCount { get; private set; }
        public string? LastDeviceId { get; private set; }
        public MobileDevicePlatform LastPlatform { get; private set; }
        public string? LastPushToken { get; private set; }
        public bool LastNotificationsEnabled { get; private set; }
        public string? LastAppVersion { get; private set; }
        public string? LastDeviceModel { get; private set; }

        public Task<Result<PushDeviceRegistrationClientModel>> RegisterDeviceAsync(
            string deviceId,
            MobileDevicePlatform platform,
            string? pushToken,
            bool notificationsEnabled,
            string? appVersion,
            string? deviceModel,
            CancellationToken ct)
        {
            CallCount++;
            LastDeviceId = deviceId;
            LastPlatform = platform;
            LastPushToken = pushToken;
            LastNotificationsEnabled = notificationsEnabled;
            LastAppVersion = appVersion;
            LastDeviceModel = deviceModel;

            return Task.FromResult(Result<PushDeviceRegistrationClientModel>.Ok(new PushDeviceRegistrationClientModel
            {
                DeviceId = deviceId,
                RegisteredAtUtc = DateTime.UtcNow
            }));
        }
    }

    private sealed class FakeTokenStore(string? accessToken) : ITokenStore
    {
        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
            => Task.CompletedTask;

        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult<(string?, DateTime?)>((accessToken, DateTime.UtcNow.AddMinutes(10)));

        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult<(string?, DateTime?)>(("refresh-token", DateTime.UtcNow.AddDays(1)));

        public Task ClearAsync()
            => Task.CompletedTask;
    }

    private sealed class FakePushTokenProvider(Result<ConsumerPushTokenState> result) : IConsumerPushTokenProvider
    {
        public Task<Result<ConsumerPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class FakeDeviceIdProvider : IDeviceIdProvider
    {
        public Task<string> GetDeviceIdAsync()
            => Task.FromResult("device-1");
    }

    private sealed class FakeRuntimeInfo : IConsumerPushRuntimeInfo
    {
        public MobileDevicePlatform Platform => MobileDevicePlatform.Android;

        public string? AppVersion => "1.2.3";

        public string? DeviceModel => "Pixel 7";
    }

    private sealed class FakeRegistrationStateStore : IConsumerPushRegistrationStateStore
    {
        public string Signature { get; set; } = string.Empty;

        public string GetLastRegistrationSignature()
            => Signature;

        public void SetLastRegistrationSignature(string signature)
            => Signature = signature;

        public void Reset()
            => Signature = string.Empty;
    }
}
