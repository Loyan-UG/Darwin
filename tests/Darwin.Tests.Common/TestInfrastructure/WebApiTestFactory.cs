using Microsoft.AspNetCore.Mvc.Testing;

namespace Darwin.Tests.Common.TestInfrastructure;

/// <summary>
///     Composed integration factory that enforces <c>Testing</c> environment and
///     provides deterministic database reset/seed lifecycle helpers.
/// </summary>
public sealed class WebApiTestFactory : WebApplicationFactory<Program>
{
    /// <summary>
    ///     Creates a deterministic HTTPS client configured with test-host defaults.
    /// </summary>
    /// <returns>A client suitable for integration endpoint assertions.</returns>
    public HttpClient CreateDeterministicClient()
        => IntegrationTestClientFactory.CreateHttpsClient(IntegrationTestHostFactory.CreateTestingFactory(this));

    /// <summary>
    ///     Recreates and seeds the backing database for deterministic class-level state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public Task ResetDatabaseAsync(CancellationToken ct = default)
        => IntegrationTestDatabaseReset.ResetAndSeedAsync(IntegrationTestHostFactory.CreateTestingFactory(this), ct);
}
