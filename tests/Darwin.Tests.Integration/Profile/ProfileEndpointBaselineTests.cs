using Darwin.Contracts.Profile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

using Darwin.Tests.Integration.Support;

namespace Darwin.Tests.Integration.Profile;

/// <summary>
///     Provides baseline integration tests for profile endpoints that are expected
///     to be protected by authentication in all environments.
/// </summary>
public sealed class ProfileEndpointBaselineTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Initializes the test suite with a testing-environment host instance.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance for host creation.</param>
    public ProfileEndpointBaselineTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Verifies that anonymous calls to the profile read endpoint are rejected.
    ///     This prevents unauthenticated profile data access.
    /// </summary>
    [Fact]
    public async Task GetMe_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();

        // Act
        using var response = await client.GetAsync("/api/v1/profile/me", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that anonymous calls to the profile update endpoint are rejected
    ///     before application-level update logic executes.
    /// </summary>
    [Fact]
    public async Task UpdateMe_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            Email = "anonymous@example.test",
            FirstName = "Anonymous",
            LastName = "User",
            Locale = "en-US",
            Timezone = "UTC",
            Currency = "USD",
            RowVersion = [1]
        };

        // Act
        using var response = await client.PutAsJsonAsync("/api/v1/profile/me", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
