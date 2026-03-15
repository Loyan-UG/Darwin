using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

using Darwin.Tests.Integration.TestInfrastructure;

namespace Darwin.Tests.Integration.Meta;

/// <summary>
///     Verifies that the meta info endpoint is reachable and returns a stable
///     diagnostics payload shape. This test protects mobile diagnostics screens
///     and operations tooling that read basic app metadata.
/// </summary>
public sealed class MetaInfoEndpointTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Creates a test instance bound to a WebApi host running in testing environment.
    /// </summary>
    /// <param name="factory">The shared web host factory.</param>
    public MetaInfoEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Ensures the endpoint responds with HTTP 200 and includes non-empty application
    ///     and environment values used by client diagnostics.
    /// </summary>
    [Fact]
    public async Task GetInfo_Should_ReturnOk_WithBasicDiagnosticsFields()
    {
        // Arrange: use HTTPS base address to keep middleware behavior deterministic.
        using var client = CreateHttpsClient();

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
