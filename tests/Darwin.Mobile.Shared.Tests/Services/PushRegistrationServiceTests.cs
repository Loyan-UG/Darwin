using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Services.Notifications;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers push-device registration behavior used by mobile notification setup.
/// </summary>
public sealed class PushRegistrationServiceTests
{
    /// <summary>
    /// Verifies push registration uses the canonical member notification route and trims optional fields.
    /// </summary>
    [Fact]
    public async Task RegisterDeviceAsync_Should_UseCanonicalMemberNotificationRoute()
    {
        var registeredAtUtc = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Notifications.RegisterDevice);
                request.Should().BeOfType<RegisterPushDeviceRequest>();

                var payload = (RegisterPushDeviceRequest)request;
                payload.DeviceId.Should().Be("device-42");
                payload.Platform.Should().Be(MobileDevicePlatform.Android);
                payload.PushToken.Should().Be("push-token");
                payload.NotificationsEnabled.Should().BeTrue();
                payload.AppVersion.Should().Be("1.2.3");
                payload.DeviceModel.Should().Be("Pixel 7");

                return Result<RegisterPushDeviceResponse>.Ok(new RegisterPushDeviceResponse
                {
                    DeviceId = payload.DeviceId,
                    RegisteredAtUtc = registeredAtUtc
                });
            }
        };
        var service = new PushRegistrationService(api);

        var result = await service.RegisterDeviceAsync(
            " device-42 ",
            MobileDevicePlatform.Android,
            " push-token ",
            notificationsEnabled: true,
            " 1.2.3 ",
            " Pixel 7 ",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DeviceId.Should().Be("device-42");
        result.Value.RegisteredAtUtc.Should().Be(registeredAtUtc);
    }

    /// <summary>
    /// Verifies invalid device identifiers fail before any API call is made.
    /// </summary>
    [Fact]
    public async Task RegisterDeviceAsync_Should_FailFast_WhenDeviceIdIsMissing()
    {
        var api = new FakeApiClient();
        var service = new PushRegistrationService(api);

        var result = await service.RegisterDeviceAsync(
            " ",
            MobileDevicePlatform.iOS,
            pushToken: null,
            notificationsEnabled: false,
            appVersion: null,
            deviceModel: null,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DeviceId is required.");
        api.PostCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies server/API failures are returned as safe failed results.
    /// </summary>
    [Fact]
    public async Task RegisterDeviceAsync_Should_ReturnFailure_WhenApiRejectsRegistration()
    {
        var api = new FakeApiClient
        {
            OnPostResultAsync = (_, _) => Result<RegisterPushDeviceResponse>.Fail("Registration rejected.")
        };
        var service = new PushRegistrationService(api);

        var result = await service.RegisterDeviceAsync(
            "device-42",
            MobileDevicePlatform.Android,
            pushToken: null,
            notificationsEnabled: false,
            appVersion: null,
            deviceModel: null,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Registration rejected.");
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object, object?>? OnPostResultAsync { get; init; }
        public int PostCount { get; private set; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            PostCount++;
            if (OnPostResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST handler configured."));
            }

            var response = OnPostResultAsync(route, request!);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type."));
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostFileResultAsync<TResponse>(
            string route,
            Stream fileStream,
            string formFieldName,
            string fileName,
            string contentType,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
