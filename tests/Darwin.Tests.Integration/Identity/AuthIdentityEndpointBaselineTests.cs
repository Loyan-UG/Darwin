using Darwin.Contracts.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Darwin.Tests.Integration.Identity;

/// <summary>
///     Provides baseline integration coverage for authentication endpoints that
///     must remain stable regardless of surrounding feature growth.
/// </summary>
public sealed class AuthIdentityEndpointBaselineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test fixture with a host configured for testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public AuthIdentityEndpointBaselineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

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
    ///     Creates a test client using HTTPS base address so middleware behavior
    ///     remains deterministic without redirect side effects.
    /// </summary>
    /// <returns>Configured HttpClient instance.</returns>
    private HttpClient CreateHttpsClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
