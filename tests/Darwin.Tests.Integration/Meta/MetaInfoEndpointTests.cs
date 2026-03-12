using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;

namespace Darwin.Tests.Integration.Meta;

/// <summary>
///     Verifies that the meta info endpoint is reachable and returns a stable
///     diagnostics payload shape. This test protects mobile diagnostics screens
///     and operations tooling that read basic app metadata.
/// </summary>
public sealed class MetaInfoEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Creates a test instance bound to a WebApi host running in testing environment.
    /// </summary>
    /// <param name="factory">The shared web host factory.</param>
    public MetaInfoEndpointTests(WebApplicationFactory<Program> factory)
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
    ///     Ensures the endpoint responds with HTTP 200 and includes non-empty application
    ///     and environment values used by client diagnostics.
    /// </summary>
    [Fact]
    public async Task GetInfo_Should_ReturnOk_WithBasicDiagnosticsFields()
    {
        // Arrange: use HTTPS base address to keep middleware behavior deterministic.
        using var client = IntegrationTestClientFactory.CreateHttpsClient(_factory);

        // Act
        using var response = await client.GetAsync("/api/v1/meta/info");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetaInfoResponse>();
        payload.Should().NotBeNull();
        payload!.Application.Should().NotBeNullOrWhiteSpace();
        payload.Environment.Should().NotBeNullOrWhiteSpace();
        payload.Version.Should().NotBeNullOrWhiteSpace();
        payload.UptimeSeconds.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    ///     Minimal response shape required by this integration test.
    /// </summary>
    private sealed class MetaInfoResponse
    {
        /// <summary>
        ///     Logical application name.
        /// </summary>
        public string Application { get; init; } = string.Empty;

        /// <summary>
        ///     Host environment value.
        /// </summary>
        public string Environment { get; init; } = string.Empty;

        /// <summary>
        ///     API version string extracted from assembly metadata.
        /// </summary>
        public string Version { get; init; } = string.Empty;

        /// <summary>
        ///     Server uptime in seconds.
        /// </summary>
        public double UptimeSeconds { get; init; }
    }
}
