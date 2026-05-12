using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers invitation-onboarding behavior in the shared mobile authentication service.
/// </summary>
public sealed class AuthServiceInvitationTests
{
    [Fact]
    public async Task GetBusinessInvitationPreviewAsync_Should_UseCanonicalBusinessAuthRoute()
    {
        var api = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be("api/v1/business/auth/invitations/preview?token=invite-token");
                return Result<BusinessInvitationPreviewResponse>.Ok(new BusinessInvitationPreviewResponse
                {
                    InvitationId = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    BusinessName = "Cafe Morgenrot",
                    Email = "operator@morgenrot.de",
                    Role = "Owner",
                    Status = "Pending",
                    ExpiresAtUtc = new DateTime(2030, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    HasExistingUser = false
                });
            }
        };

        var service = CreateService(api);

        var preview = await service.GetBusinessInvitationPreviewAsync("invite-token", TestContext.Current.CancellationToken);

        preview.Should().NotBeNull();
        preview!.BusinessName.Should().Be("Cafe Morgenrot");
    }

    [Fact]
    public async Task AcceptBusinessInvitationAsync_Should_SaveTokens_AndLoadBootstrap()
    {
        var accessToken = CreateBusinessJwt(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var tokenStore = new FakeTokenStore();
        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be("api/v1/business/auth/invitations/accept");
                request.Should().BeOfType<AcceptBusinessInvitationRequest>();

                var payload = (AcceptBusinessInvitationRequest)request;
                payload.Token.Should().Be("invite-token");
                payload.DeviceId.Should().Be("device-42");

                return Result<TokenResponse>.Ok(new TokenResponse
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    RefreshToken = "refresh-token",
                    RefreshTokenExpiresAtUtc = new DateTime(2030, 1, 8, 12, 0, 0, DateTimeKind.Utc),
                    UserId = Guid.NewGuid(),
                    Email = "operator@morgenrot.de"
                });
            },
            OnGetAsync = route =>
            {
                route.Should().Be(ApiRoutes.Meta.Bootstrap);
                return new AppBootstrapResponse
                {
                    JwtAudience = "Darwin.PublicApi",
                    MaxOutboxItems = 77,
                    QrTokenRefreshSeconds = 33
                };
            }
        };

        var service = CreateService(api, tokenStore);

        var bootstrap = await service.AcceptBusinessInvitationAsync(
            new AcceptBusinessInvitationRequest
            {
                Token = "invite-token",
                FirstName = "Greta",
                LastName = "Sommer",
                Password = "Business123!"
            },
            deviceId: null,
            TestContext.Current.CancellationToken);

        bootstrap.JwtAudience.Should().Be("Darwin.PublicApi");
        bootstrap.MaxOutboxItems.Should().Be(77);
        tokenStore.AccessToken.Should().Be(accessToken);
        api.LastBearerToken.Should().Be(accessToken);
    }

    [Fact]
    public async Task TryRefreshAsync_Should_PreservePreferredBusinessId_FromStoredAccessToken()
    {
        var currentBusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var currentAccessToken = CreateBusinessJwt(currentBusinessId);
        var refreshedAccessToken = CreateBusinessJwt(currentBusinessId);

        var tokenStore = new FakeTokenStore
        {
            AccessToken = currentAccessToken,
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(5),
            RefreshToken = "existing-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(5)
        };

        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.Refresh);
                request.Should().BeOfType<RefreshTokenRequest>();

                var payload = (RefreshTokenRequest)request;
                payload.RefreshToken.Should().Be("existing-refresh-token");
                payload.DeviceId.Should().Be("device-42");
                payload.BusinessId.Should().Be(currentBusinessId);

                return Result<TokenResponse>.Ok(new TokenResponse
                {
                    AccessToken = refreshedAccessToken,
                    AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                    RefreshToken = "refreshed-refresh-token",
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                    UserId = Guid.NewGuid(),
                    Email = "operator@example.de"
                });
            }
        };

        var service = CreateService(api, tokenStore);

        var refreshed = await service.TryRefreshAsync(TestContext.Current.CancellationToken);

        refreshed.Should().BeTrue();
        tokenStore.RefreshToken.Should().Be("refreshed-refresh-token");
        api.LastBearerToken.Should().Be(refreshedAccessToken);
    }

    [Fact]
    public async Task RequestEmailConfirmationAsync_Should_UseCanonicalMemberAuthRoute()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.RequestEmailConfirmation);
                request.Should().BeOfType<RequestEmailConfirmationRequest>();

                var payload = (RequestEmailConfirmationRequest)request;
                payload.Email.Should().Be("member@darwin.de");

                return Result.Ok();
            }
        };

        var service = CreateService(api);

        var sent = await service.RequestEmailConfirmationAsync("member@darwin.de", TestContext.Current.CancellationToken);

        sent.Should().BeTrue();
    }

    private static AuthService CreateService(FakeApiClient api, FakeTokenStore? tokenStore = null)
    {
        return new AuthService(
            api,
            new FakeMobileCacheService(),
            tokenStore ?? new FakeTokenStore(),
            new ApiOptions
            {
                BaseUrl = "https://localhost",
                AppRole = MobileAppRole.Business,
                JwtAudience = string.Empty
            },
            new FakeDeviceIdProvider(),
            TimeProvider.System);
    }

    private static string CreateBusinessJwt(Guid businessId)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("business_id", businessId.ToString()),
                new Claim("email", "operator@example.de")
            ]);

        return handler.WriteToken(token);
    }

    private sealed class FakeDeviceIdProvider : IDeviceIdProvider
    {
        public Task<string> GetDeviceIdAsync() => Task.FromResult("device-42");
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        public string? AccessToken { get; set; }
        public DateTime? AccessExpiresUtc { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresUtc { get; set; }

        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            AccessToken = accessToken;
            AccessExpiresUtc = accessExpiresUtc;
            RefreshToken = refreshToken;
            RefreshExpiresUtc = refreshExpiresUtc;
            return Task.CompletedTask;
        }

        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult((AccessToken, AccessExpiresUtc));

        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult((RefreshToken, RefreshExpiresUtc));

        public Task ClearAsync()
        {
            AccessToken = null;
            AccessExpiresUtc = null;
            RefreshToken = null;
            RefreshExpiresUtc = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMobileCacheService : IMobileCacheService
    {
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct) => Task.FromResult<T?>(default);

        public Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct) => Task.FromResult<T?>(default);

        public Task RemoveAsync(string cacheKey, CancellationToken ct) => Task.CompletedTask;

        public Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object?>? OnGetResultAsync { get; init; }
        public Func<string, object?>? OnGetAsync { get; init; }
        public Func<string, object, object?>? OnPostResultAsync { get; init; }
        public Func<string, object, object?>? OnPostAsync { get; init; }
        public Func<string, object, Result>? OnPostNoContentAsync { get; init; }

        public string? LastBearerToken { get; private set; }

        public void SetBearerToken(string? accessToken)
        {
            LastBearerToken = accessToken;
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            if (OnGetResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No GET handler configured."));
            }

            var response = OnGetResultAsync(route);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type."));
        }

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (OnPostResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST handler configured."));
            }

            var response = OnPostResultAsync(route, request!);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type."));
        }

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
        {
            if (OnGetAsync is null)
            {
                return Task.FromResult<TResponse?>(default);
            }

            return Task.FromResult((TResponse?)OnGetAsync(route));
        }

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (OnPostAsync is null)
            {
                return Task.FromResult<TResponse?>(default);
            }

            return Task.FromResult((TResponse?)OnPostAsync(route, request!));
        }

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
        {
            if (OnPostNoContentAsync is null)
            {
                return Task.FromResult(Result.Fail("No POST no-content handler configured."));
            }

            return Task.FromResult(OnPostNoContentAsync(route, request!));
        }
    }
}
