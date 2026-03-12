using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;

namespace Darwin.Tests.Integration.Meta;

/// <summary>
///     Validates that the public health endpoint is reachable through the real
///     ASP.NET Core pipeline. This smoke test is the first integration baseline
///     and proves that the WebApi host can boot inside the test process.
/// </summary>
public sealed class MetaHealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Creates a new test class instance with a shared WebApplicationFactory.
    /// </summary>
    /// <param name="factory">The host factory used to create test clients.</param>
    public MetaHealthEndpointTests(WebApplicationFactory<Program> factory)
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
    ///     Ensures the health endpoint returns HTTP 200 and includes the expected
    ///     status payload value. This protects against startup regressions.
    /// </summary>
    [Fact]
    public async Task GetHealth_Should_ReturnOk_WithHealthyStatus()
    {
        // Arrange: use HTTPS base address to avoid redirection noise from middleware.
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);

        // Act
        using var response = await client.GetAsync("/api/v1/meta/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("Healthy");
    }

    /// <summary>
    ///     Minimal response contract used by this smoke test.
    /// </summary>
    private sealed class HealthResponse
    {
        /// <summary>
        ///     The health state string returned by the API.
        /// </summary>
        public string Status { get; init; } = string.Empty;
    }
}
