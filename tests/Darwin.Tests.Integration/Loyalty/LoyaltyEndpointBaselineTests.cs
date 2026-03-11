using Darwin.Contracts.Loyalty;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Darwin.Tests.Integration.Loyalty;

/// <summary>
///     Provides baseline authorization integration coverage for loyalty endpoints.
///     These tests ensure anonymous callers are blocked before any business logic
///     handlers are executed.
/// </summary>
public sealed class LoyaltyEndpointBaselineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test suite with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared host factory used to create isolated clients.</param>
    public LoyaltyEndpointBaselineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    /// <summary>
    ///     Verifies that listing the current user's loyalty businesses requires an
    ///     authenticated caller and rejects anonymous requests.
    /// </summary>
    [Fact]
    public async Task GetMyBusinesses_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();

        // Act
        using var response = await client.GetAsync("/api/v1/loyalty/my/businesses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that preparing a scan session requires authentication and does
    ///     not allow anonymous requests to enter loyalty session flow.
    /// </summary>
    [Fact]
    public async Task PrepareScanSession_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new PrepareScanSessionRequest
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual,
            SelectedRewardIds = []
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Creates an HTTPS client so status assertions are not affected by HTTP->HTTPS redirect behavior.
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
