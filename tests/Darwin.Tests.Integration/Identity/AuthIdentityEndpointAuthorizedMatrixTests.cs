using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;

namespace Darwin.Tests.Integration.Identity;

/// <summary>
///     Provides authorized happy-path integration coverage for identity endpoints.
///     The suite validates end-user token lifecycle operations that mobile apps rely on:
///     register/login, refresh, change-password, logout, and global logout.
/// </summary>
public sealed class AuthIdentityEndpointAuthorizedMatrixTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test fixture with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public AuthIdentityEndpointAuthorizedMatrixTests(WebApplicationFactory<Program> factory)
    {
        _factory = IntegrationTestHostFactory.CreateTestingFactory(factory);
    }

    /// <summary>
    ///     Recreates and seeds the test database before each test class to guarantee
    ///     deterministic state regardless of execution order across integration suites.
    /// </summary>
    public Task InitializeAsync() => IntegrationTestDatabaseReset.ResetAndSeedAsync(_factory);

    /// <summary>
    ///     No asynchronous class-level cleanup is required because each test class
    ///     uses isolated clients and reset logic runs during initialization.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    ///     Verifies that a freshly registered member can login and receive a complete token response.
    /// </summary>
    [Fact]
    public async Task Login_Should_ReturnTokenResponse_WhenRegisteredUserCredentialsAreValid()
    {
        // Arrange
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "Tester", credentials.Email, credentials.Password);

        // Act
        using var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new PasswordLoginRequest
        {
            Email = credentials.Email,
            Password = credentials.Password,
            DeviceId = credentials.DeviceId
        });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        token.UserId.Should().NotBe(Guid.Empty);
        token.Email.Should().Be(credentials.Email);
        token.AccessTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        token.RefreshTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    /// <summary>
    ///     Verifies that refresh endpoint accepts a valid refresh token and returns a rotated token pair.
    /// </summary>
    [Fact]
    public async Task Refresh_Should_ReturnNewTokenPair_WhenRefreshTokenIsValid()
    {
        // Arrange
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "Tester", credentials.Email, credentials.Password);
        var loginToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);

        // Act
        using var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = loginToken.RefreshToken,
            DeviceId = credentials.DeviceId
        });

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshToken = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        refreshToken.Should().NotBeNull();
        refreshToken!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.AccessToken.Should().NotBe(loginToken.AccessToken);
        refreshToken.RefreshToken.Should().NotBe(loginToken.RefreshToken);
    }

    /// <summary>
    ///     Verifies authorized password-change flow and ensures the old password becomes invalid immediately.
    /// </summary>
    [Fact]
    public async Task ChangePassword_Should_Succeed_AndInvalidateOldPassword_WhenAuthorized()
    {
        // Arrange
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "Tester", credentials.Email, credentials.Password);
        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var newPassword = "N3wP@ssw0rd!Bb2";

        // Act
        using var changePasswordResponse = await client.PostAsJsonAsync("/api/v1/auth/password/change", new ChangePasswordRequest
        {
            CurrentPassword = credentials.Password,
            NewPassword = newPassword
        });

        // Assert
        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // The old password must no longer be accepted after a successful change.
        using var oldPasswordLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new PasswordLoginRequest
        {
            Email = credentials.Email,
            Password = credentials.Password,
            DeviceId = credentials.DeviceId
        });

        oldPasswordLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var oldPasswordProblem = await oldPasswordLoginResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        oldPasswordProblem.Should().NotBeNull();

        // The new password must authenticate successfully.
        using var newPasswordLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new PasswordLoginRequest
        {
            Email = credentials.Email,
            Password = newPassword,
            DeviceId = credentials.DeviceId
        });

        newPasswordLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies single-device logout success and checks that the revoked refresh token cannot be reused.
    /// </summary>
    [Fact]
    public async Task Logout_Should_Succeed_AndRevokeSubmittedRefreshToken_WhenAuthorized()
    {
        // Arrange
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "Tester", credentials.Email, credentials.Password);
        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        // Act
        using var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest
        {
            RefreshToken = token.RefreshToken
        });

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // A revoked refresh token must fail the refresh endpoint.
        using var refreshAfterLogoutResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = token.RefreshToken,
            DeviceId = credentials.DeviceId
        });

        refreshAfterLogoutResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    ///     Verifies global logout success and ensures all active refresh tokens for the user are revoked.
    /// </summary>
    [Fact]
    public async Task LogoutAll_Should_Succeed_AndRevokeAllRefreshTokens_WhenAuthorized()
    {
        // Arrange
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "Tester", credentials.Email, credentials.Password);

        var firstSession = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);
        var secondSession = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, $"{credentials.DeviceId}-second");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondSession.AccessToken);

        // Act
        using var logoutAllResponse = await client.PostAsync("/api/v1/auth/logout-all", content: null);

        // Assert
        logoutAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // All previously issued refresh tokens should be invalid after global logout.
        using var firstRefreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstSession.RefreshToken,
            DeviceId = credentials.DeviceId
        });

        using var secondRefreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = secondSession.RefreshToken,
            DeviceId = $"{credentials.DeviceId}-second"
        });

        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    ///     Creates unique credentials for isolation between integration test runs.
    /// </summary>
    /// <returns>Unique email/password/device-id tuple.</returns>
    private static TestCredentials CreateUniqueCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TestCredentials(
            Email: $"integration-{suffix}@example.test",
            Password: "P@ssw0rd!Aa1",
            DeviceId: $"device-{suffix}");
    }

    /// <summary>
    ///     Represents deterministic user credentials used by each integration test.
    /// </summary>
    /// <param name="Email">Unique user email.</param>
    /// <param name="Password">User password satisfying policy requirements.</param>
    /// <param name="DeviceId">Device identifier used for refresh-token binding scenarios.</param>
    private sealed record TestCredentials(string Email, string Password, string DeviceId);
}
