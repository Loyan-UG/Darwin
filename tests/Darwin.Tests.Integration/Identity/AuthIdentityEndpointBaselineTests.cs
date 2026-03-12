using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;

namespace Darwin.Tests.Integration.Identity;

/// <summary>
///     Provides baseline integration coverage for authentication endpoints that
///     must remain stable regardless of surrounding feature growth.
/// </summary>
public sealed class AuthIdentityEndpointBaselineTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test fixture with a host configured for testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public AuthIdentityEndpointBaselineTests(WebApplicationFactory<Program> factory)
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
    ///     Verifies anti-enumeration behavior for password reset request endpoint.
    ///     The endpoint must return 200/OK even when the email is not found or
    ///     internal processing fails, to avoid leaking user existence signals.
    /// </summary>
    [Fact]
    public async Task RequestPasswordReset_Should_ReturnOk_ForUnknownEmail()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new RequestPasswordResetRequest
        {
            Email = $"unknown-{Guid.NewGuid():N}@example.test"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/password/request-reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies that authenticated-only password change endpoint rejects
    ///     anonymous requests with 401/Unauthorized.
    /// </summary>
    [Fact]
    public async Task ChangePassword_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that login with clearly invalid credentials fails with a
    ///     problem-details payload instead of returning a successful token response.
    /// </summary>
    [Fact]
    public async Task Login_Should_ReturnBadRequest_WithProblemDetails_WhenCredentialsAreInvalid()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new PasswordLoginRequest
        {
            Email = $"invalid-{Guid.NewGuid():N}@example.test",
            Password = "WrongPassword123!"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    ///     Verifies that refresh with an invalid token is rejected and returned
    ///     as a problem-details error response.
    /// </summary>
    [Fact]
    public async Task Refresh_Should_ReturnBadRequest_WithProblemDetails_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new RefreshTokenRequest
        {
            RefreshToken = $"invalid-refresh-{Guid.NewGuid():N}",
            DeviceId = null
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    ///     Verifies that reset-password rejects fake reset tokens and returns a
    ///     consistent problem-details payload.
    /// </summary>
    [Fact]
    public async Task ResetPassword_Should_ReturnBadRequest_WithProblemDetails_WhenTokenIsInvalid()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new ResetPasswordRequest
        {
            Email = $"unknown-{Guid.NewGuid():N}@example.test",
            Token = $"invalid-reset-token-{Guid.NewGuid():N}",
            NewPassword = "NewPassword123!"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }





    /// <summary>
    ///     Verifies that single-device logout endpoint requires authentication and
    ///     rejects anonymous requests with 401/Unauthorized.
    /// </summary>
    [Fact]
    public async Task Logout_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new LogoutRequest
        {
            RefreshToken = $"anonymous-refresh-{Guid.NewGuid():N}"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that global logout endpoint requires authentication and rejects
    ///     anonymous requests with 401/Unauthorized.
    /// </summary>
    [Fact]
    public async Task LogoutAll_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();

        // Act
        using var response = await client.PostAsync("/api/v1/auth/logout-all", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Creates a test client using HTTPS base address so middleware behavior
    ///     remains deterministic without redirect side effects.
    /// </summary>
    /// <returns>Configured HttpClient instance.</returns>
    private HttpClient CreateHttpsClient()
    {
        return IntegrationTestClientFactory.CreateHttpsClient(_factory);
    }
}
