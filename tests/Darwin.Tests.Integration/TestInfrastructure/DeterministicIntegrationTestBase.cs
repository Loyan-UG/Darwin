using Darwin.Tests.Common.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Sdk;

namespace Darwin.Tests.Integration.Support;

/// <summary>
///     Provides a deterministic integration-test base with shared host configuration,
///     HTTPS client creation, and database reset/seed lifecycle orchestration.
/// </summary>
public abstract class DeterministicIntegrationTestBase : IAsyncLifetime
{
    /// <summary>
    ///     Initializes the base fixture by forcing the test host environment to <c>Testing</c>.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance from xUnit fixture.</param>
    protected DeterministicIntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = IntegrationTestHostFactory.CreateTestingFactory(factory);
    }

    /// <summary>
    ///     Gets the configured integration host factory that all derived suites reuse.
    /// </summary>
    protected WebApplicationFactory<Program> Factory { get; }

    /// <summary>
    ///     Creates an HTTPS client with deterministic base address and redirect behavior.
    /// </summary>
    /// <returns>A client configured for Darwin integration testing conventions.</returns>
    protected HttpClient CreateHttpsClient() => IntegrationTestClientFactory.CreateHttpsClient(Factory);

    /// <summary>
    ///     Recreates and seeds the database before each test class to guarantee
    ///     deterministic state regardless of execution order across suites.
    ///     Skips the test gracefully when no integration database connection string
    ///     is configured in the environment.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        try
        {
            await IntegrationTestDatabaseReset.ResetAndSeedAsync(Factory, default).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Integration DB connection string is missing"))
        {
            throw SkipException.ForSkip(
                "Integration database not configured. " +
                "Set 'ConnectionStrings__DefaultConnection' environment variable or provide appsettings.Testing.local.json to run integration tests.");
        }
    }

    /// <summary>
    ///     No asynchronous class-level cleanup is required by the shared fixture.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
