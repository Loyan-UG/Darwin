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

    [Fact]
    public async Task RequestEmailConfirmationAsync_Should_ReturnFalse_OnRequestFailure()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("provider unavailable")
        };

        var service = CreateService(api);
        var sent = await service.RequestEmailConfirmationAsync("member@darwin.de", TestContext.Current.CancellationToken);

        sent.Should().BeFalse();
    }

    [Fact]
    public async Task RequestEmailConfirmationAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);

        Func<Task> act = async () => await service.RequestEmailConfirmationAsync("member@darwin.de", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_Should_UseCanonicalMemberAuthRoute()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.ConfirmEmail);
                request.Should().BeOfType<ConfirmEmailRequest>();

                var payload = (ConfirmEmailRequest)request;
                payload.Email.Should().Be("member@darwin.de");
                payload.Token.Should().Be("confirm-token");

                return Result.Ok();
            }
        };

        var service = CreateService(api);

        var confirmed = await service.ConfirmEmailAsync("member@darwin.de", "confirm-token", TestContext.Current.CancellationToken);

        confirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailAsync_Should_ReturnFalse_OnRequestFailure()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("Invalid token")
        };

        var service = CreateService(api);
        var confirmed = await service.ConfirmEmailAsync("member@darwin.de", "bad-token", TestContext.Current.CancellationToken);

        confirmed.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmailAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);

        Func<Task> act = async () => await service.ConfirmEmailAsync("member@darwin.de", "confirm-token", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task LoginAsync_Should_UseCanonicalMemberRoute_And_ResolveDeviceId()
    {
        var accessToken = CreateBusinessJwt(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.Login);
                request.Should().BeOfType<PasswordLoginRequest>();

                var payload = (PasswordLoginRequest)request;
                payload.Email.Should().Be("member@darwin.de");
                payload.Password.Should().Be("S3cret!");
                payload.DeviceId.Should().Be("device-42");

                return Result<TokenResponse>.Ok(new TokenResponse
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    RefreshToken = "refresh-token",
                    RefreshTokenExpiresAtUtc = new DateTime(2030, 1, 8, 12, 0, 0, DateTimeKind.Utc),
                    UserId = Guid.NewGuid(),
                    Email = "member@darwin.de"
                });
            },
            OnGetAsync = _ =>
            {
                return new AppBootstrapResponse
                {
                    JwtAudience = "Darwin.PublicApi",
                    MaxOutboxItems = 9,
                    QrTokenRefreshSeconds = 77
                };
            }
        };
        var tokenStore = new FakeTokenStore();
        var service = CreateService(api, tokenStore);

        var bootstrap = await service.LoginAsync(
            "member@darwin.de",
            "S3cret!",
            deviceId: null,
            TestContext.Current.CancellationToken);

        bootstrap.JwtAudience.Should().Be("Darwin.PublicApi");
        tokenStore.AccessToken.Should().Be(accessToken);
        api.LastBearerToken.Should().Be(accessToken);
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowInvalidOperation_OnFailedLogin()
    {
        var api = new FakeApiClient
        {
            OnPostResultAsync = (_, _) => Result<TokenResponse>.Fail("Invalid credentials.")
        };

        var service = CreateService(api);

        Func<Task> act = async () => await service.LoginAsync(
            "member@darwin.de",
            "bad-password",
            "device-42",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_Should_PropagateException_OnNetworkFailure()
    {
        var api = new FakeApiClient
        {
            OnPostResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);

        Func<Task> act = async () => await service.LoginAsync(
            "member@darwin.de",
            "S3cret!",
            "device-42",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowOnNullRequest()
    {
        var service = CreateService(new FakeApiClient());

        Func<Task> act = async () => await service.RegisterAsync(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterAsync_Should_UseCanonicalRoute()
    {
        var api = new FakeApiClient
        {
            OnPostAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.Register);
                request.Should().BeOfType<RegisterRequest>();

                var payload = (RegisterRequest)request;
                payload.FirstName.Should().Be("Ada");
                payload.LastName.Should().Be("Lovelace");
                payload.Email.Should().Be("ada@example.de");
                payload.Password.Should().Be("Password123!");

                return new RegisterResponse
                {
                    DisplayName = "Ada Lovelace",
                    ConfirmationEmailSent = true
                };
            }
        };

        var service = CreateService(api);

        var response = await service.RegisterAsync(
            new RegisterRequest
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.de",
                Password = "Password123!"
            },
            TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response!.DisplayName.Should().Be("Ada Lovelace");
        response.ConfirmationEmailSent.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_Should_ReturnNull_WhenServiceReturnsNull()
    {
        var api = new FakeApiClient
        {
            OnPostAsync = (_, _) => null
        };

        var service = CreateService(api);
        var response = await service.RegisterAsync(
            new RegisterRequest
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.de",
                Password = "Password123!"
            },
            TestContext.Current.CancellationToken);

        response.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);

        Func<Task> act = async () => await service.RegisterAsync(
            new RegisterRequest
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.de",
                Password = "Password123!"
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_ReturnTrue_OnFirstAttemptSuccess()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.ChangePassword);
                request.Should().BeOfType<ChangePasswordRequest>();

                var payload = (ChangePasswordRequest)request;
                payload.CurrentPassword.Should().Be("old-password");
                payload.NewPassword.Should().Be("new-password");

                return Result.Ok();
            }
        };

        var service = CreateService(api);
        var changed = await service.ChangePasswordAsync("old-password", "new-password", TestContext.Current.CancellationToken);

        changed.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_ReturnFalse_WhenFirstAttemptFailsNonUnauthorized()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("validation error")
        };

        var service = CreateService(api);
        var changed = await service.ChangePasswordAsync("old-password", "new-password", TestContext.Current.CancellationToken);

        changed.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => throw new InvalidOperationException("network failure")
        };

        var service = CreateService(api);
        Func<Task> act = async () => await service.ChangePasswordAsync("old-password", "new-password", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_RetryAfterUnauthorizedFailure_WhenRefreshSucceeds()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshToken = "existing-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };

        var postNoContentCall = 0;
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.ChangePassword);
                postNoContentCall++;

                if (postNoContentCall == 1)
                {
                    return Result.Fail("Unauthorized");
                }

                return Result.Ok();
            },
            OnPostResultAsync = (_, _) =>
                Result<TokenResponse>.Ok(new TokenResponse
                {
                    AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
                    AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                    RefreshToken = "refreshed-token",
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(5),
                    UserId = Guid.NewGuid(),
                    Email = "member@darwin.de"
                }),
            OnGetAsync = _ =>
                new AppBootstrapResponse()
        };

        var service = CreateService(api, tokenStore);
        var changed = await service.ChangePasswordAsync("old-password", "new-password", TestContext.Current.CancellationToken);

        changed.Should().BeTrue();
        postNoContentCall.Should().Be(2);
        tokenStore.RefreshToken.Should().Be("refreshed-token");
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_ReturnFalse_WhenUnauthorizedRetryFails()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshToken = "existing-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("401 Unauthorized"),
            OnPostResultAsync = (_, _) => Result<TokenResponse>.Fail("refresh token expired"),
            OnGetAsync = _ => new AppBootstrapResponse()
        };

        var service = CreateService(api, tokenStore, cache);
        var changed = await service.ChangePasswordAsync("old-password", "new-password", TestContext.Current.CancellationToken);

        changed.Should().BeFalse();
        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
        cache.ClearCount.Should().Be(1);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_Should_UseCanonicalMemberAuthRoute()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.RequestPasswordReset);
                request.Should().BeOfType<RequestPasswordResetRequest>();

                var payload = (RequestPasswordResetRequest)request;
                payload.Email.Should().Be("member@darwin.de");

                return Result.Ok();
            }
        };

        var service = CreateService(api);
        var requested = await service.RequestPasswordResetAsync("member@darwin.de", TestContext.Current.CancellationToken);

        requested.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_Should_ReturnFalse_OnRequestFailure()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("Cannot find account.")
        };

        var service = CreateService(api);
        var requested = await service.RequestPasswordResetAsync("member@darwin.de", TestContext.Current.CancellationToken);

        requested.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);
        Func<Task> act = async () => await service.RequestPasswordResetAsync("member@darwin.de", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_UseCanonicalMemberAuthRoute()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.ResetPassword);
                request.Should().BeOfType<ResetPasswordRequest>();

                var payload = (ResetPasswordRequest)request;
                payload.Email.Should().Be("member@darwin.de");
                payload.Token.Should().Be("reset-token");
                payload.NewPassword.Should().Be("new-password");

                return Result.Ok();
            }
        };

        var service = CreateService(api);
        var reset = await service.ResetPasswordAsync("member@darwin.de", "reset-token", "new-password", TestContext.Current.CancellationToken);

        reset.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_ReturnFalse_OnRequestFailure()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => Result.Fail("invalid token")
        };

        var service = CreateService(api);
        var reset = await service.ResetPasswordAsync("member@darwin.de", "bad-token", "new-password", TestContext.Current.CancellationToken);

        reset.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_PropagateException()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(api);
        Func<Task> act = async () => await service.ResetPasswordAsync(
            "member@darwin.de",
            "reset-token",
            "new-password",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task LogoutAllAsync_Should_ClearSession_OnServerSuccess()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = "access-token",
            AccessExpiresUtc = DateTime.UtcNow.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            LastBearerToken = "access-token",
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.LogoutAll);
                request.Should().NotBeNull();
                return Result.Ok();
            }
        };

        var service = CreateService(api, tokenStore, cache);
        var loggedOut = await service.LogoutAllAsync(TestContext.Current.CancellationToken);

        loggedOut.Should().BeTrue();
        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
        cache.ClearCount.Should().Be(1);
        api.LastBearerToken.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAllAsync_Should_ReturnFalse_WhenServerRejects()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = "access-token",
            AccessExpiresUtc = DateTime.UtcNow.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var api = new FakeApiClient
        {
            LastBearerToken = "access-token",
            OnPostNoContentAsync = (_, _) => Result.Fail("server busy")
        };

        var service = CreateService(api, tokenStore);
        var loggedOut = await service.LogoutAllAsync(TestContext.Current.CancellationToken);

        loggedOut.Should().BeFalse();
        tokenStore.AccessToken.Should().Be("access-token");
        tokenStore.RefreshToken.Should().Be("refresh-token");
        api.LastBearerToken.Should().Be("access-token");
    }

    [Fact]
    public async Task LogoutAsync_Should_ClearTokenStoreCacheAndBearer_WhenRemoteRevokeFails()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = "access-token",
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshToken = "refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            LastBearerToken = "access-token",
            OnPostAsync = (_, _) => throw new InvalidOperationException("Remote revoke failed.")
        };
        var service = CreateService(api, tokenStore, cache);

        await service.LogoutAsync(TestContext.Current.CancellationToken);

        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
        cache.ClearCount.Should().Be(1);
        api.LastBearerToken.Should().BeNull();
    }

    [Fact]
    public async Task TryRefreshAsync_Should_ClearLocalSession_WhenRefreshTokenIsDefinitivelyRejected()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(-5),
            RefreshToken = "expired-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            LastBearerToken = "stale-access-token",
            OnPostResultAsync = (_, _) => Result<TokenResponse>.Fail("refresh token expired")
        };
        var service = CreateService(api, tokenStore, cache);

        var refreshed = await service.TryRefreshAsync(TestContext.Current.CancellationToken);

        refreshed.Should().BeFalse();
        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
        cache.ClearCount.Should().Be(1);
        api.LastBearerToken.Should().BeNull();
    }

    [Fact]
    public async Task TryRefreshAsync_Should_ClearLocalSession_WhenRefreshTokenMissing()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshToken = null,
            RefreshExpiresUtc = null
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            LastBearerToken = "stale-access-token"
        };

        var service = CreateService(api, tokenStore, cache);
        var refreshed = await service.TryRefreshAsync(TestContext.Current.CancellationToken);

        refreshed.Should().BeFalse();
        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
        cache.ClearCount.Should().Be(1);
        api.LastBearerToken.Should().BeNull();
    }

    [Fact]
    public async Task EnsureAuthenticatedSessionAsync_Should_ReturnTrue_WhenValidAccessToken_NotNearExpiry()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            OnGetAsync = _ => new AppBootstrapResponse
            {
                JwtAudience = "Darwin.PublicApi",
                MaxOutboxItems = 33,
                QrTokenRefreshSeconds = 60
            }
        };

        var service = CreateService(api, tokenStore, cache);
        var isSessionAlive = await service.EnsureAuthenticatedSessionAsync(TestContext.Current.CancellationToken);

        isSessionAlive.Should().BeTrue();
        api.LastBearerToken.Should().Be(tokenStore.AccessToken);
        cache.ClearCount.Should().Be(0);
    }

    [Fact]
    public async Task EnsureAuthenticatedSessionAsync_Should_UseCachedBootstrap_WhenAvailable()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService
        {
            FreshValue = new AppBootstrapResponse
            {
                JwtAudience = "Cached.Audience",
                MaxOutboxItems = 88,
                QrTokenRefreshSeconds = 77
            }
        };
        var api = new FakeApiClient
        {
            OnGetAsync = _ => new AppBootstrapResponse
            {
                JwtAudience = "Darwin.PublicApi",
                MaxOutboxItems = 11,
                QrTokenRefreshSeconds = 22
            }
        };

        var service = CreateService(api, tokenStore, cache);
        var isSessionAlive = await service.EnsureAuthenticatedSessionAsync(TestContext.Current.CancellationToken);

        isSessionAlive.Should().BeTrue();
        api.GetAsyncCallCount.Should().Be(0);
        api.LastBearerToken.Should().Be(tokenStore.AccessToken);
    }

    [Fact]
    public async Task EnsureAuthenticatedSessionAsync_Should_Refresh_WhenAccessTokenNearExpiry()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddSeconds(30),
            RefreshToken = "existing-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var newAccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.Auth.Refresh);
                request.Should().BeOfType<RefreshTokenRequest>();

                var payload = (RefreshTokenRequest)request;
                payload.RefreshToken.Should().Be("existing-refresh-token");
                payload.DeviceId.Should().Be("device-42");
                payload.BusinessId.Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

                return Result<TokenResponse>.Ok(new TokenResponse
                {
                    AccessToken = newAccessToken,
                    AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
                    RefreshToken = "refreshed-refresh-token",
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(5),
                    UserId = Guid.NewGuid(),
                    Email = "operator@darwin.de"
                });
            },
            OnGetAsync = _ => new AppBootstrapResponse
            {
                JwtAudience = "Darwin.PublicApi",
                MaxOutboxItems = 5,
                QrTokenRefreshSeconds = 42
            }
        };

        var service = CreateService(api, tokenStore);
        var isSessionAlive = await service.EnsureAuthenticatedSessionAsync(TestContext.Current.CancellationToken);

        isSessionAlive.Should().BeTrue();
        tokenStore.RefreshToken.Should().Be("refreshed-refresh-token");
        tokenStore.AccessToken.Should().Be(newAccessToken);
        api.LastBearerToken.Should().Be(newAccessToken);
    }

    [Fact]
    public async Task EnsureAuthenticatedSessionAsync_Should_ClearBearer_WhenRefreshFailsFromExpiredSession()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = "expired-access",
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(-10),
            RefreshToken = "expired-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(-1)
        };
        var api = new FakeApiClient
        {
            OnPostResultAsync = (_, _) => Result<TokenResponse>.Fail("refresh token expired"),
            OnPostNoContentAsync = (_, _) => Result.Fail("refresh token expired")
        };

        var service = CreateService(api, tokenStore);
        var isSessionAlive = await service.EnsureAuthenticatedSessionAsync(TestContext.Current.CancellationToken);

        isSessionAlive.Should().BeFalse();
        api.LastBearerToken.Should().BeNull();
        tokenStore.AccessToken.Should().BeNull();
        tokenStore.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task EnsureAuthenticatedSessionAsync_Should_ReturnFalse_WhenExpiredAndRefreshFailsWithoutRefreshToken()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = "expired-access",
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(-10),
            RefreshToken = null,
            RefreshExpiresUtc = null
        };
        var api = new FakeApiClient();
        var service = CreateService(api, tokenStore);

        var isSessionAlive = await service.EnsureAuthenticatedSessionAsync(TestContext.Current.CancellationToken);

        isSessionAlive.Should().BeFalse();
        api.LastBearerToken.Should().BeNull();
    }

    [Fact]
    public async Task TryRefreshAsync_Should_ReturnFalse_OnNonDefinitiveFailure_WithoutClearingSession()
    {
        var tokenStore = new FakeTokenStore
        {
            AccessToken = CreateBusinessJwt(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            AccessExpiresUtc = DateTime.UtcNow.AddMinutes(5),
            RefreshToken = "existing-refresh-token",
            RefreshExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        var cache = new FakeMobileCacheService();
        var api = new FakeApiClient
        {
            LastBearerToken = "access-token",
            OnPostResultAsync = (_, _) => Result<TokenResponse>.Fail("service unavailable")
        };

        var service = CreateService(api, tokenStore, cache);
        var refreshed = await service.TryRefreshAsync(TestContext.Current.CancellationToken);

        refreshed.Should().BeFalse();
        tokenStore.AccessToken.Should().NotBeNull();
        tokenStore.RefreshToken.Should().NotBeNull();
        cache.ClearCount.Should().Be(0);
    }

    private static AuthService CreateService(
        FakeApiClient api,
        FakeTokenStore? tokenStore = null,
        FakeMobileCacheService? cache = null)
    {
        return new AuthService(
            api,
            cache ?? new FakeMobileCacheService(),
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
        public object? FreshValue { get; set; }

        public object? UsableValue { get; set; }

        public int ClearCount { get; private set; }

        public Task ClearAsync(CancellationToken ct)
        {
            ClearCount++;
            return Task.CompletedTask;
        }

        public Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct)
            => Task.FromResult((T?)FreshValue);

        public Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct)
            => Task.FromResult((T?)UsableValue);

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

        public string? LastBearerToken { get; set; }

        public int GetAsyncCallCount { get; private set; }

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
        {
            GetAsyncCallCount++;
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
